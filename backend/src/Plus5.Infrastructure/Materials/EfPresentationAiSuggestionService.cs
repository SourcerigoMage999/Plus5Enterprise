using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;
using Plus5.Domain.Evidence;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationAiSuggestionService(
    Plus5DbContext db,
    IPresentationAiAssistant assistant,
    IPresentationEditorQuery editorQuery,
    IPresentationSlideService slideService,
    IPresentationElementService elementService,
    IOptions<PresentationAiOptions> options,
    TimeProvider timeProvider) : IPresentationAiSuggestionService
{
    private const int MaximumInstructionLength = 2000;
    private const int MaximumContextElements = 100;
    private const int MaximumKnowledgeCandidates = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] TextElementTypes =
        ["TEXT", "RULE", "EXAMPLE", "VOCABULARY", "QUESTION"];
    private readonly PresentationAiOptions settings = options.Value;

    public async Task<PresentationAiSuggestionResult> CreateAsync(
        Guid teacherAccountId,
        Guid materialId,
        CreatePresentationAiSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
        {
            return new(PresentationAiSuggestionOutcome.Disabled);
        }

        if (command is null
            || !PresentationAiSuggestionKinds.IsSupported(command.SuggestionKind)
            || command.Instruction?.Trim().Length > MaximumInstructionLength)
        {
            return new(PresentationAiSuggestionOutcome.InvalidInput);
        }

        var workspace = await editorQuery.GetAsync(
            teacherAccountId,
            materialId,
            cancellationToken);
        if (workspace?.Document is null)
        {
            return new(PresentationAiSuggestionOutcome.NotFound);
        }

        if (!TryDecodeRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || !expected.SequenceEqual(Convert.FromBase64String(workspace.Document.RowVersion))
            || !TargetsExist(workspace, command))
        {
            return new(PresentationAiSuggestionOutcome.Conflict);
        }

        var context = await BuildContextAsync(
            workspace,
            command,
            cancellationToken);
        var providerResult = await assistant.GenerateAsync(
            new PresentationAiProviderRequest(
                command.SuggestionKind,
                command.Instruction?.Trim() ?? string.Empty,
                context,
                SchemaFor(command.SuggestionKind)),
            cancellationToken);
        if (providerResult.Outcome == PresentationAiProviderOutcome.Disabled)
        {
            return new(PresentationAiSuggestionOutcome.Disabled);
        }

        if (providerResult.Outcome == PresentationAiProviderOutcome.Unavailable)
        {
            return new(PresentationAiSuggestionOutcome.Unavailable);
        }

        if (providerResult.Outcome != PresentationAiProviderOutcome.Success
            || !TryNormalize(command.SuggestionKind, providerResult.ContentJson, out var normalized))
        {
            return new(PresentationAiSuggestionOutcome.InvalidOutput);
        }

        if (command.SuggestionKind == PresentationAiSuggestionKinds.KnowledgeMapping
            && !await UsesOnlyBoundedKnowledgeCandidatesAsync(
                normalized,
                cancellationToken))
        {
            return new(PresentationAiSuggestionOutcome.InvalidOutput);
        }

        var now = timeProvider.GetUtcNow();
        var suggestion = new PresentationAiSuggestion(
            Guid.NewGuid(),
            teacherAccountId,
            materialId,
            workspace.MaterialVersionId,
            workspace.MaterialVersionId,
            command.SuggestionKind,
            providerResult.Provider ?? "OPENAI",
            providerResult.Model ?? settings.Model,
            providerResult.PromptTemplateVersion ?? settings.PromptTemplateVersion,
            normalized,
            now,
            now.AddHours(24),
            command.TargetSlideId,
            command.TargetElementId);
        db.PresentationAiSuggestions.Add(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return new(PresentationAiSuggestionOutcome.Success, ToView(suggestion));
    }

    public async Task<PresentationAiSuggestionView?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid suggestionId,
        CancellationToken cancellationToken)
    {
        var suggestion = await FindAsync(
            teacherAccountId,
            materialId,
            suggestionId,
            cancellationToken);
        if (suggestion is null)
        {
            return null;
        }

        if (suggestion.IsExpired(timeProvider.GetUtcNow()))
        {
            suggestion.Resolve(PresentationAiSuggestionStatus.Expired, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToView(suggestion);
    }

    public async Task<PresentationAiSuggestionResult> ResolveAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid suggestionId,
        ResolvePresentationAiSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || command.Decision is not ("Accept" or "Edit" or "Reject"))
        {
            return new(PresentationAiSuggestionOutcome.InvalidInput);
        }

        var suggestion = await FindAsync(
            teacherAccountId,
            materialId,
            suggestionId,
            cancellationToken);
        if (suggestion is null)
        {
            return new(PresentationAiSuggestionOutcome.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (suggestion.IsExpired(now))
        {
            suggestion.Resolve(PresentationAiSuggestionStatus.Expired, now);
            await db.SaveChangesAsync(cancellationToken);
            return new(PresentationAiSuggestionOutcome.Expired, ToView(suggestion));
        }

        if (suggestion.Status != PresentationAiSuggestionStatus.Pending)
        {
            return new(PresentationAiSuggestionOutcome.Conflict, ToView(suggestion));
        }

        if (command.Decision == "Reject")
        {
            suggestion.Resolve(PresentationAiSuggestionStatus.Rejected, now);
            await db.SaveChangesAsync(cancellationToken);
            return new(PresentationAiSuggestionOutcome.Success, ToView(suggestion));
        }

        var source = command.Decision == "Edit"
            ? command.EditedContentJson
            : suggestion.GeneratedContentJson;
        if (!TryNormalize(suggestion.SuggestionKind, source, out var normalized))
        {
            return new(PresentationAiSuggestionOutcome.InvalidInput);
        }

        if (suggestion.SuggestionKind == PresentationAiSuggestionKinds.KnowledgeMapping
            && !await UsesOnlyBoundedKnowledgeCandidatesAsync(
                normalized,
                cancellationToken))
        {
            return new(PresentationAiSuggestionOutcome.InvalidInput);
        }

        var workspace = await editorQuery.GetAsync(
            teacherAccountId,
            materialId,
            cancellationToken);
        if (workspace?.Document is null)
        {
            return new(PresentationAiSuggestionOutcome.NotFound);
        }

        if (!TryDecodeRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || !expected.SequenceEqual(Convert.FromBase64String(workspace.Document.RowVersion)))
        {
            return new(PresentationAiSuggestionOutcome.Conflict);
        }

        var applied = await ApplyAsync(
            teacherAccountId,
            materialId,
            suggestion,
            normalized,
            workspace,
            cancellationToken);
        if (applied != PresentationAiSuggestionOutcome.Success)
        {
            return new(applied);
        }

        suggestion.Resolve(
            command.Decision == "Edit"
                ? PresentationAiSuggestionStatus.Edited
                : PresentationAiSuggestionStatus.Accepted,
            now);
        await db.SaveChangesAsync(cancellationToken);
        return new(PresentationAiSuggestionOutcome.Success, ToView(suggestion));
    }

    private async Task<PresentationAiSuggestionOutcome> ApplyAsync(
        Guid teacherAccountId,
        Guid materialId,
        PresentationAiSuggestion suggestion,
        string contentJson,
        PresentationEditorWorkspace workspace,
        CancellationToken cancellationToken)
    {
        using var content = JsonDocument.Parse(contentJson);
        return suggestion.SuggestionKind switch
        {
            PresentationAiSuggestionKinds.PresentationDraft => await ApplyDraftAsync(
                teacherAccountId, materialId, content.RootElement, workspace, cancellationToken),
            PresentationAiSuggestionKinds.ElementContent => await ApplyElementAsync(
                teacherAccountId, materialId, suggestion, content.RootElement, workspace, cancellationToken),
            PresentationAiSuggestionKinds.QuickCheck => await ApplyQuickCheckAsync(
                teacherAccountId, materialId, suggestion, content.RootElement, workspace, cancellationToken),
            PresentationAiSuggestionKinds.KnowledgeMapping => await ApplyKnowledgeMappingAsync(
                teacherAccountId, materialId, suggestion, content.RootElement, workspace, cancellationToken),
            _ => PresentationAiSuggestionOutcome.InvalidInput,
        };
    }

    private async Task<PresentationAiSuggestionOutcome> ApplyDraftAsync(
        Guid teacherAccountId,
        Guid materialId,
        JsonElement content,
        PresentationEditorWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var slides = content.GetProperty("slides").EnumerateArray().ToList();
        var document = workspace.Document!;
        var targetSlideId = document.Slides[^1].Id;
        for (var slideIndex = 0; slideIndex < slides.Count; slideIndex++)
        {
            if (slideIndex > 0)
            {
                var add = await slideService.AddAsync(
                    teacherAccountId,
                    materialId,
                    new PresentationConcurrencyCommandSource(document.RowVersion, targetSlideId)
                        .ToAddCommand(),
                    cancellationToken);
                if (add.Outcome != PresentationMutationOutcome.Success || !add.SlideId.HasValue)
                {
                    return MapMutation(add.Outcome);
                }

                targetSlideId = add.SlideId.Value;
                workspace = (await editorQuery.GetAsync(teacherAccountId, materialId, cancellationToken))!;
                document = workspace.Document!;
            }

            var title = slides[slideIndex].GetProperty("title").GetString()!;
            var titleResult = await elementService.CreateAsync(
                teacherAccountId,
                materialId,
                targetSlideId,
                CreateText(document.RowVersion, title, 90, 70, 1420, 120, 58, true),
                cancellationToken);
            if (titleResult.Outcome != PresentationMutationOutcome.Success)
            {
                return MapMutation(titleResult.Outcome);
            }

            workspace = (await editorQuery.GetAsync(teacherAccountId, materialId, cancellationToken))!;
            document = workspace.Document!;
            var y = 230m;
            foreach (var element in slides[slideIndex].GetProperty("elements").EnumerateArray())
            {
                var type = element.GetProperty("elementTypeCode").GetString()!;
                var text = element.GetProperty("content").GetString()!;
                var result = await elementService.CreateAsync(
                    teacherAccountId,
                    materialId,
                    targetSlideId,
                    CreateTextualElement(document.RowVersion, type, text, y),
                    cancellationToken);
                if (result.Outcome != PresentationMutationOutcome.Success)
                {
                    return MapMutation(result.Outcome);
                }

                workspace = (await editorQuery.GetAsync(teacherAccountId, materialId, cancellationToken))!;
                document = workspace.Document!;
                y = Math.Min(y + 190, 700);
            }
        }

        return PresentationAiSuggestionOutcome.Success;
    }

    private async Task<PresentationAiSuggestionOutcome> ApplyElementAsync(
        Guid teacherAccountId,
        Guid materialId,
        PresentationAiSuggestion suggestion,
        JsonElement content,
        PresentationEditorWorkspace workspace,
        CancellationToken cancellationToken)
    {
        if (!suggestion.TargetSlideId.HasValue)
        {
            return PresentationAiSuggestionOutcome.InvalidInput;
        }

        var type = content.GetProperty("elementTypeCode").GetString()!;
        var text = content.GetProperty("content").GetString()!;
        if (suggestion.TargetElementId.HasValue)
        {
            var existing = workspace.Document!.Slides
                .SingleOrDefault(item => item.Id == suggestion.TargetSlideId.Value)?
                .Elements.SingleOrDefault(item => item.Id == suggestion.TargetElementId.Value);
            if (existing is null || existing.ElementTypeCode != type)
            {
                return PresentationAiSuggestionOutcome.Conflict;
            }

            var update = await elementService.UpdateAsync(
                teacherAccountId,
                materialId,
                suggestion.TargetSlideId.Value,
                existing.Id,
                new UpdatePresentationElementCommand(
                    workspace.Document.RowVersion,
                    existing.ZIndex,
                    existing.X,
                    existing.Y,
                    existing.Width,
                    existing.Height,
                    PayloadFor(type, text)),
                cancellationToken);
            return MapMutation(update.Outcome);
        }

        var create = await elementService.CreateAsync(
            teacherAccountId,
            materialId,
            suggestion.TargetSlideId.Value,
            CreateTextualElement(workspace.Document!.RowVersion, type, text, 260),
            cancellationToken);
        return MapMutation(create.Outcome);
    }

    private async Task<PresentationAiSuggestionOutcome> ApplyQuickCheckAsync(
        Guid teacherAccountId,
        Guid materialId,
        PresentationAiSuggestion suggestion,
        JsonElement content,
        PresentationEditorWorkspace workspace,
        CancellationToken cancellationToken)
    {
        if (!suggestion.TargetSlideId.HasValue)
        {
            return PresentationAiSuggestionOutcome.InvalidInput;
        }

        var answers = content.GetProperty("answers").EnumerateArray()
            .Select(item => item.GetString()!)
            .ToList();
        var result = await elementService.CreateQuickCheckAsync(
            teacherAccountId,
            materialId,
            suggestion.TargetSlideId.Value,
            new UpsertPresentationQuickCheckCommand(
                workspace.Document!.RowVersion,
                workspace.Document.Slides.Single(item => item.Id == suggestion.TargetSlideId.Value)
                    .Elements.Count,
                250,
                180,
                1100,
                520,
                content.GetProperty("question").GetString()!,
                answers,
                content.GetProperty("correctAnswerIndex").GetInt32(),
                content.GetProperty("difficulty").GetInt32(),
                (EvidenceType)content.GetProperty("evidenceType").GetInt32(),
                []),
            cancellationToken);
        return MapMutation(result.Outcome);
    }

    private async Task<PresentationAiSuggestionOutcome> ApplyKnowledgeMappingAsync(
        Guid teacherAccountId,
        Guid materialId,
        PresentationAiSuggestion suggestion,
        JsonElement content,
        PresentationEditorWorkspace workspace,
        CancellationToken cancellationToken)
    {
        if (!suggestion.TargetSlideId.HasValue || !suggestion.TargetElementId.HasValue)
        {
            return PresentationAiSuggestionOutcome.InvalidInput;
        }

        var element = workspace.Document!.Slides
            .SingleOrDefault(item => item.Id == suggestion.TargetSlideId.Value)?
            .Elements.SingleOrDefault(item => item.Id == suggestion.TargetElementId.Value);
        if (element?.QuickCheck is null)
        {
            return PresentationAiSuggestionOutcome.Conflict;
        }

        var ids = content.GetProperty("knowledgeComponentIds").EnumerateArray()
            .Select(item => item.GetGuid())
            .ToList();
        var result = await elementService.UpdateQuickCheckAsync(
            teacherAccountId,
            materialId,
            suggestion.TargetSlideId.Value,
            element.Id,
            new UpsertPresentationQuickCheckCommand(
                workspace.Document.RowVersion,
                element.ZIndex,
                element.X,
                element.Y,
                element.Width,
                element.Height,
                element.QuickCheck.Prompt,
                element.QuickCheck.Options.Select(item => item.Text).ToList(),
                element.QuickCheck.Options.ToList().FindIndex(
                    item => item.Key == element.QuickCheck.CorrectOptionKey),
                element.QuickCheck.Difficulty,
                element.QuickCheck.EvidenceType,
                ids),
            cancellationToken);
        return MapMutation(result.Outcome);
    }

    private async Task<string> BuildContextAsync(
        PresentationEditorWorkspace workspace,
        CreatePresentationAiSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        var metadata = await (
            from version in db.MaterialVersions.AsNoTracking()
            where version.Id == workspace.MaterialVersionId
            select new
            {
                version.Title,
                version.LearningGoal,
                version.Subject,
                version.LanguageCode,
                Program = version.ProgramId == null
                    ? null
                    : db.Programs.Where(item => item.Id == version.ProgramId).Select(item => item.Name).FirstOrDefault(),
                SchoolGrade = version.SchoolGradeId == null
                    ? null
                    : db.SchoolGrades.Where(item => item.Id == version.SchoolGradeId).Select(item => item.Name).FirstOrDefault(),
                ProficiencyLevel = version.ProficiencyLevelId == null
                    ? null
                    : db.ProficiencyLevels.Where(item => item.Id == version.ProficiencyLevelId).Select(item => item.Name).FirstOrDefault(),
            }).SingleAsync(cancellationToken);
        var tags = await db.MaterialVersionTags.AsNoTracking()
            .Where(item => item.MaterialVersionId == workspace.MaterialVersionId)
            .OrderBy(item => item.Name)
            .Select(item => item.Name)
            .Take(20)
            .ToListAsync(cancellationToken);
        var candidates = command.SuggestionKind == PresentationAiSuggestionKinds.KnowledgeMapping
            ? await (
                from component in db.KnowledgeComponents.AsNoTracking()
                join area in db.KnowledgeAreas.AsNoTracking() on component.KnowledgeAreaId equals area.Id
                join model in db.KnowledgeModels.AsNoTracking() on component.KnowledgeModelId equals model.Id
                where component.Status == KnowledgeComponentStatus.Active
                    && model.Status == KnowledgeModelStatus.Published
                    && !db.KnowledgeComponents.Any(child => child.ParentComponentId == component.Id)
                orderby model.Code, model.Version, area.SortOrder, component.SortOrder
                select new
                {
                    KnowledgeComponentId = component.Id,
                    component.Name,
                    KnowledgeArea = area.Name,
                    KnowledgeModelCode = model.Code,
                    KnowledgeModelVersion = model.Version,
                }).Take(MaximumKnowledgeCandidates).ToListAsync(cancellationToken)
            : [];
        var elements = workspace.Document!.Slides
            .OrderBy(item => item.SortOrder)
            .SelectMany(slide => slide.Elements.OrderBy(item => item.ZIndex).Select(element => new
            {
                SlideId = slide.Id,
                slide.SortOrder,
                ElementId = element.Id,
                element.ElementTypeCode,
                TextPayload = TextElementTypes.Contains(element.ElementTypeCode, StringComparer.Ordinal)
                    ? element.PayloadJson
                    : null,
            }))
            .Take(MaximumContextElements)
            .ToList();
        return JsonSerializer.Serialize(new
        {
            Material = new
            {
                metadata.Title,
                metadata.LearningGoal,
                metadata.Subject,
                metadata.LanguageCode,
                metadata.Program,
                metadata.SchoolGrade,
                metadata.ProficiencyLevel,
                Tags = tags,
            },
            Presentation = new
            {
                workspace.MaterialVersionId,
                Slides = workspace.Document.Slides.Select(item => new { item.Id, item.SortOrder }),
                Elements = elements,
                SupportedTextElementTypes = TextElementTypes,
            },
            KnowledgeCandidates = candidates,
        }, JsonOptions);
    }

    private async Task<bool> UsesOnlyBoundedKnowledgeCandidatesAsync(
        string contentJson,
        CancellationToken cancellationToken)
    {
        using var content = JsonDocument.Parse(contentJson);
        var suggestedIds = content.RootElement.GetProperty("knowledgeComponentIds")
            .EnumerateArray()
            .Select(item => item.GetGuid())
            .ToHashSet();
        var eligibleIds = await (
            from component in db.KnowledgeComponents.AsNoTracking()
            join area in db.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            where component.Status == KnowledgeComponentStatus.Active
                && model.Status == KnowledgeModelStatus.Published
                && !db.KnowledgeComponents.Any(child =>
                    child.ParentComponentId == component.Id)
            orderby model.Code, model.Version, area.SortOrder, component.SortOrder
            select component.Id)
            .Take(MaximumKnowledgeCandidates)
            .ToHashSetAsync(cancellationToken);
        return suggestedIds.IsSubsetOf(eligibleIds);
    }

    private static bool TargetsExist(
        PresentationEditorWorkspace workspace,
        CreatePresentationAiSuggestionCommand command)
    {
        var slides = workspace.Document!.Slides;
        if (command.SuggestionKind == PresentationAiSuggestionKinds.PresentationDraft)
        {
            return true;
        }

        if (!command.TargetSlideId.HasValue
            || slides.All(item => item.Id != command.TargetSlideId.Value))
        {
            return false;
        }

        return !command.TargetElementId.HasValue
            || slides.Single(item => item.Id == command.TargetSlideId.Value)
                .Elements.Any(item => item.Id == command.TargetElementId.Value);
    }

    private async Task<PresentationAiSuggestion?> FindAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid suggestionId,
        CancellationToken cancellationToken) => await (
        from suggestion in db.PresentationAiSuggestions
        join material in db.Materials on suggestion.MaterialId equals material.Id
        join version in db.MaterialVersions on suggestion.MaterialVersionId equals version.Id
        where suggestion.Id == suggestionId
            && suggestion.MaterialId == materialId
            && suggestion.TeacherAccountId == teacherAccountId
            && material.OwnerTeacherId == teacherAccountId
            && material.Status == MaterialStatus.Active
            && material.ArchivedAtUtc == null
            && version.Status == MaterialVersionStatus.Draft
        select suggestion).SingleOrDefaultAsync(cancellationToken);

    internal static bool TryNormalize(string kind, string? json, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(json ?? string.Empty);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var valid = kind switch
            {
                PresentationAiSuggestionKinds.PresentationDraft => ValidateDraft(root),
                PresentationAiSuggestionKinds.ElementContent => ValidateElement(root),
                PresentationAiSuggestionKinds.QuickCheck => ValidateQuickCheck(root),
                PresentationAiSuggestionKinds.KnowledgeMapping => ValidateMapping(root),
                _ => false,
            };
            if (!valid)
            {
                return false;
            }

            normalized = JsonSerializer.Serialize(root, JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ValidateDraft(JsonElement root)
    {
        if (!HasOnly(root, "slides")
            || !root.TryGetProperty("slides", out var slides)
            || slides.ValueKind != JsonValueKind.Array
            || slides.GetArrayLength() is < 1 or > 20)
        {
            return false;
        }

        foreach (var slide in slides.EnumerateArray())
        {
            if (!HasOnly(slide, "title", "elements")
                || !ValidString(slide, "title", 160, true)
                || !slide.TryGetProperty("elements", out var elements)
                || elements.ValueKind != JsonValueKind.Array
                || elements.GetArrayLength() > 10
                || elements.EnumerateArray().Any(element =>
                    !HasOnly(element, "elementTypeCode", "content")
                    || !ValidElementType(element)
                    || !ValidString(element, "content", 4000, true)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateElement(JsonElement root) =>
        HasOnly(root, "elementTypeCode", "content")
        && ValidElementType(root)
        && ValidString(root, "content", 4000, true);

    private static bool ValidateQuickCheck(JsonElement root)
    {
        if (!HasOnly(root, "question", "answers", "correctAnswerIndex", "difficulty", "evidenceType")
            || !ValidString(root, "question", 4000, true)
            || !root.TryGetProperty("answers", out var answers)
            || answers.ValueKind != JsonValueKind.Array
            || answers.GetArrayLength() is < 2 or > 8
            || answers.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String
                || item.GetString() is not { Length: > 0 and <= 1000 })
            || !TryBoundedInt(root, "correctAnswerIndex", 0, answers.GetArrayLength() - 1)
            || !TryBoundedInt(root, "difficulty", 1, 5)
            || !TryBoundedInt(root, "evidenceType", 1, 4))
        {
            return false;
        }

        return true;
    }

    private static bool ValidateMapping(JsonElement root)
    {
        if (!HasOnly(root, "knowledgeComponentIds")
            || !root.TryGetProperty("knowledgeComponentIds", out var ids)
            || ids.ValueKind != JsonValueKind.Array
            || ids.GetArrayLength() is < 1 or > 20)
        {
            return false;
        }

        var values = new HashSet<Guid>();
        return ids.EnumerateArray().All(item => item.TryGetGuid(out var id)
            && id != Guid.Empty
            && values.Add(id));
    }

    private static bool HasOnly(JsonElement root, params string[] names) =>
        root.ValueKind == JsonValueKind.Object
        && root.EnumerateObject().Select(item => item.Name).Order().SequenceEqual(names.Order());

    private static bool ValidElementType(JsonElement root) =>
        root.TryGetProperty("elementTypeCode", out var type)
        && type.GetString() is { } value
        && TextElementTypes.Contains(value, StringComparer.Ordinal);

    private static bool ValidString(JsonElement root, string name, int maximum, bool required) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
        && text.Length <= maximum
        && (!required || !string.IsNullOrWhiteSpace(text));

    private static bool TryBoundedInt(JsonElement root, string name, int minimum, int maximum) =>
        root.TryGetProperty(name, out var value)
        && value.TryGetInt32(out var parsed)
        && parsed >= minimum
        && parsed <= maximum;

    private static CreatePresentationElementCommand CreateText(
        string rowVersion,
        string text,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        int fontSize,
        bool bold) => new(
            rowVersion,
            "TEXT",
            x,
            y,
            width,
            height,
            JsonSerializer.Serialize(new
            {
                text,
                fontSize,
                fontWeight = bold ? "bold" : "regular",
                color = "#173E67",
                align = "left",
            }, JsonOptions));

    private static CreatePresentationElementCommand CreateTextualElement(
        string rowVersion,
        string type,
        string content,
        decimal y) => new(
            rowVersion,
            type,
            150,
            y,
            1300,
            160,
            PayloadFor(type, content));

    private static string PayloadFor(string type, string content) => type switch
    {
        "TEXT" => JsonSerializer.Serialize(new { text = content, fontSize = 36, fontWeight = "regular", color = "#173E67", align = "left" }, JsonOptions),
        "RULE" => JsonSerializer.Serialize(new { heading = "Pravilo", body = content, accentColor = "#F4B400" }, JsonOptions),
        "EXAMPLE" => JsonSerializer.Serialize(new { example = content, explanation = string.Empty, accentColor = "#1767FF" }, JsonOptions),
        "VOCABULARY" => JsonSerializer.Serialize(new { term = content, definition = content, example = string.Empty }, JsonOptions),
        "QUESTION" => JsonSerializer.Serialize(new { prompt = content, discussionNote = string.Empty }, JsonOptions),
        _ => throw new InvalidOperationException("Unsupported AI text element type."),
    };

    private static bool TryDecodeRowVersion(string? encoded, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(encoded);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static PresentationAiSuggestionOutcome MapMutation(PresentationMutationOutcome outcome) => outcome switch
    {
        PresentationMutationOutcome.Success => PresentationAiSuggestionOutcome.Success,
        PresentationMutationOutcome.NotFound => PresentationAiSuggestionOutcome.NotFound,
        PresentationMutationOutcome.Conflict => PresentationAiSuggestionOutcome.Conflict,
        _ => PresentationAiSuggestionOutcome.InvalidInput,
    };

    private static PresentationAiSuggestionView ToView(PresentationAiSuggestion suggestion) => new(
        suggestion.Id,
        suggestion.SuggestionKind,
        suggestion.Status.ToString(),
        suggestion.GeneratedContentJson,
        suggestion.Provider,
        suggestion.Model,
        suggestion.PromptTemplateVersion,
        suggestion.GeneratedAtUtc,
        suggestion.ExpiresAtUtc,
        suggestion.ResolvedAtUtc,
        suggestion.TargetSlideId,
        suggestion.TargetElementId);

    internal static string SchemaFor(string kind) => kind switch
    {
        PresentationAiSuggestionKinds.PresentationDraft => """
            {"type":"object","additionalProperties":false,"properties":{"slides":{"type":"array","minItems":1,"maxItems":20,"items":{"type":"object","additionalProperties":false,"properties":{"title":{"type":"string","minLength":1,"maxLength":160},"elements":{"type":"array","maxItems":10,"items":{"type":"object","additionalProperties":false,"properties":{"elementTypeCode":{"type":"string","enum":["TEXT","RULE","EXAMPLE","VOCABULARY","QUESTION"]},"content":{"type":"string","minLength":1,"maxLength":4000}},"required":["elementTypeCode","content"]}}},"required":["title","elements"]}}},"required":["slides"]}
            """,
        PresentationAiSuggestionKinds.ElementContent => """
            {"type":"object","additionalProperties":false,"properties":{"elementTypeCode":{"type":"string","enum":["TEXT","RULE","EXAMPLE","VOCABULARY","QUESTION"]},"content":{"type":"string","minLength":1,"maxLength":4000}},"required":["elementTypeCode","content"]}
            """,
        PresentationAiSuggestionKinds.QuickCheck => """
            {"type":"object","additionalProperties":false,"properties":{"question":{"type":"string","minLength":1,"maxLength":4000},"answers":{"type":"array","minItems":2,"maxItems":8,"items":{"type":"string","minLength":1,"maxLength":1000}},"correctAnswerIndex":{"type":"integer","minimum":0,"maximum":7},"difficulty":{"type":"integer","minimum":1,"maximum":5},"evidenceType":{"type":"integer","minimum":1,"maximum":4}},"required":["question","answers","correctAnswerIndex","difficulty","evidenceType"]}
            """,
        PresentationAiSuggestionKinds.KnowledgeMapping => """
            {"type":"object","additionalProperties":false,"properties":{"knowledgeComponentIds":{"type":"array","minItems":1,"maxItems":20,"uniqueItems":true,"items":{"type":"string","format":"uuid"}}},"required":["knowledgeComponentIds"]}
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private sealed record PresentationConcurrencyCommandSource(
        string ExpectedDocumentRowVersion,
        Guid AfterSlideId)
    {
        public AddPresentationSlideCommand ToAddCommand() =>
            new(ExpectedDocumentRowVersion, AfterSlideId);
    }
}
