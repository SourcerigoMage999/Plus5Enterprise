using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonActivityAiSuggestionService(
    Plus5DbContext db,
    ILessonBuilderAiAssistant assistant,
    ILessonBuilderAiContextBuilder contextBuilder,
    ILessonActivityEditingService editingService,
    IOptions<LessonBuilderAiOptions> options,
    TimeProvider clock) : ILessonActivityAiSuggestionService
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions ContentJsonOptions = CreateContentJsonOptions();
    private readonly LessonBuilderAiOptions settings = options.Value;

    public async Task<LessonActivityAiSuggestionResult> CreateAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId,
        CreateLessonActivityAiSuggestionCommand command, CancellationToken cancellationToken)
    {
        if (!settings.Enabled) return new(LessonActivityAiSuggestionOutcome.Disabled);
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty || activityId == Guid.Empty
            || !TryRowVersion(command.RowVersion, out var expected))
            return new(LessonActivityAiSuggestionOutcome.InvalidInput);
        var initial = await LoadStateAsync(teacherId, lessonPlanId, activityId, true, cancellationToken);
        if (initial is null) return new(LessonActivityAiSuggestionOutcome.NotFound);
        if (!initial.Version.RowVersion.SequenceEqual(expected)) return new(LessonActivityAiSuggestionOutcome.Conflict);
        var now = clock.GetUtcNow();
        if (await db.LessonActivityAiSuggestions.AsNoTracking().CountAsync(item =>
            item.TeacherAccountId == teacherId && item.GeneratedAtUtc >= now.AddDays(-1), cancellationToken) >= 50)
            return new(LessonActivityAiSuggestionOutcome.DailyLimitReached);

        LessonActivityAiSuggestion? replaced = null;
        if (command.ReplacesSuggestionId is Guid replacedId)
        {
            replaced = await FindAsync(teacherId, lessonPlanId, activityId, replacedId, true, cancellationToken);
            if (replaced is null || replaced.Status != LessonActivityAiSuggestionStatus.Pending || replaced.IsExpired(now))
                return new(LessonActivityAiSuggestionOutcome.Conflict);
        }
        var context = await contextBuilder.BuildAsync(teacherId, lessonPlanId, activityId, cancellationToken);
        if (context is null) return new(LessonActivityAiSuggestionOutcome.NotFound);
        var provider = await assistant.GenerateActivitySuggestionAsync(
            new(context.ContextJson, OutputSchema), cancellationToken);
        if (provider.Outcome == LessonActivityAiProviderOutcome.Disabled)
            return new(LessonActivityAiSuggestionOutcome.Disabled);
        if (provider.Outcome == LessonActivityAiProviderOutcome.Unavailable)
            return new(LessonActivityAiSuggestionOutcome.Unavailable);
        if (provider.Outcome != LessonActivityAiProviderOutcome.Success
            || !TryNormalize(provider.ContentJson, out var normalized))
            return new(LessonActivityAiSuggestionOutcome.InvalidOutput);

        await using var transaction = await BeginAsync(cancellationToken);
        var current = await LoadStateAsync(teacherId, lessonPlanId, activityId, false, cancellationToken);
        if (current is null) return new(LessonActivityAiSuggestionOutcome.NotFound);
        if (!current.Version.RowVersion.SequenceEqual(expected)) return new(LessonActivityAiSuggestionOutcome.Conflict);
        if (command.ReplacesSuggestionId is Guid currentReplacedId)
        {
            replaced = await FindAsync(teacherId, lessonPlanId, activityId, currentReplacedId, false, cancellationToken);
            if (replaced is null || replaced.Status != LessonActivityAiSuggestionStatus.Pending || replaced.IsExpired(now))
                return new(LessonActivityAiSuggestionOutcome.Conflict);
            replaced.Resolve(LessonActivityAiSuggestionStatus.Replaced, now);
        }
        var suggestion = new LessonActivityAiSuggestion(Guid.NewGuid(), teacherId, lessonPlanId,
            current.Version.Id, activityId, expected,
            provider.Provider ?? "OPENAI", provider.Model ?? settings.Model,
            provider.PromptTemplateVersion ?? settings.PromptTemplateVersion,
            normalized, now, now.Add(TimeToLive), command.ReplacesSuggestionId);
        db.LessonActivityAiSuggestions.Add(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(LessonActivityAiSuggestionOutcome.Success, ToView(suggestion));
    }

    public async Task<LessonActivityAiSuggestionView?> GetAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, Guid suggestionId,
        CancellationToken cancellationToken)
    {
        var suggestion = await FindAsync(teacherId, lessonPlanId, activityId, suggestionId, false, cancellationToken);
        if (suggestion is null) return null;
        suggestion.Expire(clock.GetUtcNow());
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(cancellationToken);
        return ToView(suggestion);
    }

    public async Task<LessonActivityAiSuggestionResult> ResolveAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, Guid suggestionId,
        ResolveLessonActivityAiSuggestionCommand command, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.RowVersion, out var expected)
            || !Enum.TryParse<Decision>(command.Decision, true, out var decision))
            return new(LessonActivityAiSuggestionOutcome.InvalidInput);
        await using var transaction = await BeginAsync(cancellationToken);
        var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, false, cancellationToken);
        var suggestion = await FindAsync(teacherId, lessonPlanId, activityId, suggestionId, false, cancellationToken);
        if (state is null || suggestion is null) return new(LessonActivityAiSuggestionOutcome.NotFound);
        var now = clock.GetUtcNow();
        if (suggestion.IsExpired(now))
        {
            suggestion.Expire(now); await db.SaveChangesAsync(cancellationToken);
            return new(LessonActivityAiSuggestionOutcome.Expired, ToView(suggestion));
        }
        if (suggestion.Status != LessonActivityAiSuggestionStatus.Pending)
            return new(LessonActivityAiSuggestionOutcome.Conflict, ToView(suggestion));
        if (!state.Version.RowVersion.SequenceEqual(expected)
            || !suggestion.BasedOnRowVersion.SequenceEqual(expected))
            return new(LessonActivityAiSuggestionOutcome.Conflict);

        if (decision == Decision.Reject)
        {
            suggestion.Resolve(LessonActivityAiSuggestionStatus.Rejected, now);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(LessonActivityAiSuggestionOutcome.Success, ToView(suggestion));
        }

        var source = decision == Decision.Edit ? command.EditedContentJson : suggestion.ContentJson;
        if (!TryNormalize(source, out var normalized) || !TryRead(normalized, out var content))
            return new(LessonActivityAiSuggestionOutcome.InvalidInput);
        try
        {
            state.Activity.ApplyAiContent(content.Name, content.Purpose, content.Description,
                content.TeacherInstructions, content.StudentInstructions, content.WorkMode);
        }
        catch (ArgumentException)
        {
            return new(LessonActivityAiSuggestionOutcome.InvalidInput);
        }
        db.Entry(state.Version).Property(item => item.RowVersion).OriginalValue = expected;
        state.Version.RecordAuthoringChange(now);
        suggestion.Resolve(decision == Decision.Edit
            ? LessonActivityAiSuggestionStatus.Edited : LessonActivityAiSuggestionStatus.Accepted, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(LessonActivityAiSuggestionOutcome.Conflict);
        }
        var detail = await editingService.GetAsync(teacherId, lessonPlanId, activityId, cancellationToken);
        return new(LessonActivityAiSuggestionOutcome.Success, ToView(suggestion), detail.Detail);
    }

    public static bool TryNormalize(string? json, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(json) || System.Text.Encoding.UTF8.GetByteCount(json) > LessonActivityAiSuggestion.MaximumContentBytes)
            return false;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var allowed = new[] { "name", "purpose", "description", "teacherInstructions", "studentInstructions", "workMode" };
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Select(item => item.Name).Order().SequenceEqual(allowed.Order()) is false)
                return false;
            if (!TryString(root, "name", LessonActivity.NameMaxLength, required: true, out var name)
                || !TryString(root, "purpose", LessonActivity.PurposeDescriptionMaxLength, false, out var purpose)
                || !TryString(root, "description", LessonActivity.DescriptionMaxLength, false, out var description)
                || !TryString(root, "teacherInstructions", LessonActivity.InstructionsMaxLength, false, out var teacher)
                || !TryString(root, "studentInstructions", LessonActivity.InstructionsMaxLength, false, out var student)
                || !TryString(root, "workMode", 16, true, out var mode)
                || !Enum.TryParse<LessonActivityWorkMode>(mode, true, out var parsedMode)) return false;
            normalized = JsonSerializer.Serialize(new { name, purpose, description, teacherInstructions = teacher, studentInstructions = student, workMode = parsedMode.ToString() });
            return true;
        }
        catch (JsonException) { return false; }
    }

    public static string OutputSchema => """
        {"type":"object","additionalProperties":false,"properties":{"name":{"type":"string","minLength":1,"maxLength":160},"purpose":{"type":["string","null"],"maxLength":300},"description":{"type":["string","null"],"maxLength":300},"teacherInstructions":{"type":["string","null"],"maxLength":1000},"studentInstructions":{"type":["string","null"],"maxLength":1000},"workMode":{"type":"string","enum":["Individual","Pair","Group"]}},"required":["name","purpose","description","teacherInstructions","studentInstructions","workMode"]}
        """;

    private async Task<State?> LoadStateAsync(Guid teacherId, Guid planId, Guid activityId, bool noTracking, CancellationToken ct)
    {
        var query = from plan in db.LessonPlans
                    join version in db.LessonPlanVersions on plan.Id equals version.LessonPlanId
                    join activity in db.LessonActivities on version.Id equals activity.LessonPlanVersionId
                    where plan.Id == planId && plan.TeacherAccountId == teacherId && plan.ArchivedAtUtc == null
                        && version.Status == LessonPlanVersionStatus.Draft && activity.Id == activityId
                    select new State(version, activity);
        if (noTracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(ct);
    }

    private async Task<LessonActivityAiSuggestion?> FindAsync(Guid teacherId, Guid planId, Guid activityId, Guid id, bool noTracking, CancellationToken ct)
    {
        var query = db.LessonActivityAiSuggestions.Where(item => item.Id == id && item.TeacherAccountId == teacherId
            && item.LessonPlanId == planId && item.LessonActivityId == activityId);
        if (noTracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(ct);
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginAsync(CancellationToken ct) =>
        db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;

    private static LessonActivityAiSuggestionView ToView(LessonActivityAiSuggestion value) => new(
        value.Id, value.Status.ToString(), value.ContentJson, value.Provider, value.Model,
        value.PromptTemplateVersion, value.GeneratedAtUtc, value.ExpiresAtUtc, value.ReplacesSuggestionId);

    private static bool TryRead(string json, out Content content)
    {
        try
        {
            content = JsonSerializer.Deserialize<Content>(json, ContentJsonOptions)!;
            return content is not null;
        }
        catch (JsonException) { content = null!; return false; }
    }
    private static JsonSerializerOptions CreateContentJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }
    private static bool TryString(JsonElement root, string name, int max, bool required, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return !required;
        if (item.ValueKind != JsonValueKind.String) return false;
        value = item.GetString()?.Trim();
        return required ? !string.IsNullOrWhiteSpace(value) && value.Length <= max : value is null || value.Length <= max;
    }
    private static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length == 8; }
        catch (FormatException) { bytes = []; return false; }
    }
    private sealed record State(LessonPlanVersion Version, LessonActivity Activity);
    private sealed record Content(string Name, string? Purpose, string? Description, string? TeacherInstructions, string? StudentInstructions, LessonActivityWorkMode WorkMode);
    private enum Decision { Accept, Edit, Reject }
}
