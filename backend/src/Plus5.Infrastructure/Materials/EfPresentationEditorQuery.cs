using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationEditorQuery(Plus5DbContext db) : IPresentationEditorQuery
{
    private static readonly string[] AssetElementTypes = ["IMAGE", "VIDEO", "AUDIO"];

    public async Task<PresentationEditorWorkspace?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var candidate = await (
            from material in db.Materials.AsNoTracking()
            join version in db.MaterialVersions.AsNoTracking()
                on material.Id equals version.MaterialId
            where material.Id == materialId
                && material.OwnerTeacherId == teacherAccountId
                && material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Draft
                && version.MaterialTypeCode == "PRESENTATION"
            orderby version.VersionNumber descending
            select new
            {
                material.Id,
                MaterialRowVersion = material.RowVersion,
                MaterialVersionId = version.Id,
                version.VersionNumber,
                version.Title,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var document = await db.PresentationDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.MaterialVersionId == candidate.MaterialVersionId,
                cancellationToken);

        PresentationDocumentView? documentView = null;
        if (document is not null)
        {
            var slides = await db.PresentationSlides
                .AsNoTracking()
                .Where(slide => slide.MaterialVersionId == candidate.MaterialVersionId)
                .OrderBy(slide => slide.SortOrder)
                .ToListAsync(cancellationToken);
            var slideIds = slides.Select(slide => slide.Id).ToList();
            var elements = await db.PresentationElements
                .AsNoTracking()
                .Where(element => slideIds.Contains(element.PresentationSlideId))
                .OrderBy(element => element.ZIndex)
                .ToListAsync(cancellationToken);
            var assetIds = elements
                .Where(element => element.PresentationAssetId.HasValue)
                .Select(element => element.PresentationAssetId!.Value)
                .Distinct()
                .ToList();
            var assets = await db.PresentationAssets
                .AsNoTracking()
                .Where(asset => assetIds.Contains(asset.Id)
                    && asset.Status == PresentationAssetStatus.Clean)
                .ToDictionaryAsync(asset => asset.Id, cancellationToken);
            var taskVersionIds = elements
                .Where(element => element.AssessableTaskVersionId.HasValue)
                .Select(element => element.AssessableTaskVersionId!.Value)
                .Distinct()
                .ToList();
            var taskVersions = await db.AssessableTaskVersions
                .AsNoTracking()
                .Where(version => taskVersionIds.Contains(version.Id))
                .ToDictionaryAsync(version => version.Id, cancellationToken);
            var taskMappings = await (
                from mapping in db.AssessableTaskVersionKnowledgeComponents.AsNoTracking()
                join component in db.KnowledgeComponents.AsNoTracking()
                    on mapping.KnowledgeComponentId equals component.Id
                join area in db.KnowledgeAreas.AsNoTracking()
                    on component.KnowledgeAreaId equals area.Id
                join model in db.KnowledgeModels.AsNoTracking()
                    on component.KnowledgeModelId equals model.Id
                where taskVersionIds.Contains(mapping.AssessableTaskVersionId)
                orderby model.Code, model.Version, area.SortOrder,
                    component.SortOrder, component.Name
                select new
                {
                    mapping.AssessableTaskVersionId,
                    component.Id,
                    component.Name,
                    KnowledgeAreaName = area.Name,
                    KnowledgeModelCode = model.Code,
                    KnowledgeModelVersion = model.Version,
                    KnowledgeModelStatus = model.Status,
                    KnowledgeComponentStatus = component.Status,
                })
                .ToListAsync(cancellationToken);
            var mappingsByTaskVersion = taskMappings
                .GroupBy(mapping => mapping.AssessableTaskVersionId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<PresentationKnowledgeComponentView>)group
                        .Select(mapping => new PresentationKnowledgeComponentView(
                            mapping.Id,
                            mapping.Name,
                            mapping.KnowledgeAreaName,
                            mapping.KnowledgeModelCode,
                            mapping.KnowledgeModelVersion,
                            mapping.KnowledgeModelStatus.ToString(),
                            mapping.KnowledgeComponentStatus.ToString()))
                        .ToList());
            var slideViews = slides.Select(slide => new PresentationSlideView(
                slide.Id,
                slide.SortOrder,
                elements
                    .Where(element => element.PresentationSlideId == slide.Id)
                    .Select(element => new PresentationElementView(
                        element.Id,
                        element.ElementTypeCode,
                        element.ElementSchemaVersion,
                        element.ZIndex,
                        element.X,
                        element.Y,
                        element.Width,
                        element.Height,
                        element.PayloadJson,
                        element.PresentationAssetId.HasValue
                            && assets.TryGetValue(element.PresentationAssetId.Value, out var asset)
                                ? new PresentationAssetView(
                                    asset.Id,
                                    asset.AssetKindCode,
                                    asset.OriginalFileName,
                                    asset.MediaType,
                                    asset.SizeBytes,
                                    $"/materials/{candidate.Id}/presentation/assets/{asset.Id}")
                                : null,
                        element.AssessableTaskVersionId.HasValue
                            && taskVersions.TryGetValue(
                                element.AssessableTaskVersionId.Value,
                                out var taskVersion)
                            ? new PresentationQuickCheckView(
                                taskVersion.Id,
                                taskVersion.Prompt,
                                QuickCheckAuthoringPayloadRegistry.ReadOptions(
                                    taskVersion.AuthoringPayloadJson ?? string.Empty),
                                taskVersion.CorrectAnswer ?? string.Empty,
                                taskVersion.Difficulty,
                                taskVersion.EvidenceType,
                                taskVersion.MaxPoints,
                                !mappingsByTaskVersion.ContainsKey(taskVersion.Id),
                                mappingsByTaskVersion.GetValueOrDefault(taskVersion.Id, []))
                            : null))
                    .ToList()))
                .ToList();
            documentView = new PresentationDocumentView(
                document.DocumentSchemaVersion,
                document.CanvasWidth,
                document.CanvasHeight,
                Convert.ToBase64String(document.RowVersion),
                document.UpdatedAtUtc,
                slideViews);
        }

        return new PresentationEditorWorkspace(
            candidate.Id,
            candidate.MaterialVersionId,
            candidate.VersionNumber,
            candidate.Title,
            Convert.ToBase64String(candidate.MaterialRowVersion),
            documentView);
    }

    public async Task<PresentationKnowledgeComponentPage?> SearchKnowledgeComponentsAsync(
        Guid teacherAccountId,
        Guid materialId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var ownsDraftPresentation = await (
            from material in db.Materials.AsNoTracking()
            join version in db.MaterialVersions.AsNoTracking()
                on material.Id equals version.MaterialId
            where material.Id == materialId
                && material.OwnerTeacherId == teacherAccountId
                && material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Draft
                && version.MaterialTypeCode == "PRESENTATION"
            select version.Id)
            .AnyAsync(cancellationToken);
        if (!ownsDraftPresentation)
        {
            return null;
        }

        var normalizedSearch = search?.Trim();
        var candidates =
            from component in db.KnowledgeComponents.AsNoTracking()
            join area in db.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            where model.Status == KnowledgeModelStatus.Published
                && component.Status == KnowledgeComponentStatus.Active
                && !db.KnowledgeComponents.Any(child =>
                    child.ParentComponentId == component.Id)
                && (string.IsNullOrEmpty(normalizedSearch)
                    || component.Name.Contains(normalizedSearch)
                    || area.Name.Contains(normalizedSearch)
                    || model.Code.Contains(normalizedSearch)
                    || model.Version.Contains(normalizedSearch))
            orderby model.Code, model.Version, area.SortOrder,
                component.SortOrder, component.Name, component.Id
            select new
            {
                component.Id,
                component.Name,
                KnowledgeAreaName = area.Name,
                KnowledgeModelCode = model.Code,
                KnowledgeModelVersion = model.Version,
            };

        var totalCount = await candidates.CountAsync(cancellationToken);
        var rows = await candidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PresentationKnowledgeComponentPage(
            rows.Select(row => new PresentationKnowledgeComponentView(
                row.Id,
                row.Name,
                row.KnowledgeAreaName,
                row.KnowledgeModelCode,
                row.KnowledgeModelVersion,
                KnowledgeModelStatus.Published.ToString(),
                KnowledgeComponentStatus.Active.ToString())).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<PresentationReviewWorkspace?> GetReviewAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var editor = await GetAsync(teacherAccountId, materialId, cancellationToken);
        if (editor?.Document is null)
        {
            return null;
        }

        var metadata = await (
            from material in db.Materials.AsNoTracking()
            join version in db.MaterialVersions.AsNoTracking()
                on material.Id equals version.MaterialId
            where material.Id == materialId
                && material.OwnerTeacherId == teacherAccountId
                && version.Id == editor.MaterialVersionId
                && version.Status == MaterialVersionStatus.Draft
            select new
            {
                material.Visibility,
                version.Description,
                version.Subject,
                version.LanguageCode,
                version.ProgramId,
                version.SchoolGradeId,
                version.ProficiencyLevelId,
                version.LearningGoal,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var programName = metadata.ProgramId.HasValue
            ? await db.Programs.AsNoTracking()
                .Where(program => program.Id == metadata.ProgramId)
                .Select(program => program.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var gradeName = metadata.SchoolGradeId.HasValue
            ? await db.SchoolGrades.AsNoTracking()
                .Where(grade => grade.Id == metadata.SchoolGradeId)
                .Select(grade => grade.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var levelName = metadata.ProficiencyLevelId.HasValue
            ? await db.ProficiencyLevels.AsNoTracking()
                .Where(level => level.Id == metadata.ProficiencyLevelId)
                .Select(level => level.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var tags = await db.MaterialVersionTags.AsNoTracking()
            .Where(tag => tag.MaterialVersionId == editor.MaterialVersionId)
            .OrderBy(tag => tag.Name)
            .Select(tag => tag.Name)
            .ToListAsync(cancellationToken);
        var components = await (
            from mapping in db.MaterialVersionKnowledgeComponents.AsNoTracking()
            join component in db.KnowledgeComponents.AsNoTracking()
                on mapping.KnowledgeComponentId equals component.Id
            join area in db.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            where mapping.MaterialVersionId == editor.MaterialVersionId
            orderby model.Code, model.Version, area.SortOrder, component.SortOrder
            select new PresentationKnowledgeComponentView(
                component.Id,
                component.Name,
                area.Name,
                model.Code,
                model.Version,
                model.Status.ToString(),
                component.Status.ToString()))
            .ToListAsync(cancellationToken);
        var outcomes = await (
            from mapping in db.MaterialVersionCurriculumOutcomes.AsNoTracking()
            join outcome in db.CurriculumOutcomes.AsNoTracking()
                on mapping.CurriculumOutcomeId equals outcome.Id
            where mapping.MaterialVersionId == editor.MaterialVersionId
            orderby outcome.SortOrder, outcome.Title
            select new PresentationCurriculumOutcomeView(
                outcome.Id,
                outcome.OfficialCode,
                outcome.Title))
            .ToListAsync(cancellationToken);

        var quickChecks = editor.Document.Slides
            .SelectMany(slide => slide.Elements)
            .Where(element => element.ElementTypeCode == "QUICK_CHECK")
            .ToList();
        var blockers = new List<string>();
        if (editor.Document.Slides.Count == 0)
        {
            blockers.Add("Prezentacija mora sadržavati najmanje jedan slajd.");
        }
        if (editor.Document.Slides.SelectMany(slide => slide.Elements).Any(element =>
                AssetElementTypes.Contains(element.ElementTypeCode, StringComparer.Ordinal)
                && element.Asset is null))
        {
            blockers.Add("Svi medijski elementi moraju imati vlastiti provjereni zapis.");
        }
        if (quickChecks.Any(element => element.QuickCheck is null
                || element.QuickCheck.KnowledgeMappingPending))
        {
            blockers.Add("Svaki Quick Check mora biti povezan s barem jednom komponentom znanja.");
        }

        return new PresentationReviewWorkspace(
            editor.MaterialId,
            editor.MaterialVersionId,
            editor.MaterialVersionNumber,
            editor.MaterialRowVersion,
            editor.Title,
            metadata.Description,
            metadata.Subject,
            metadata.LanguageCode,
            metadata.Visibility.ToString(),
            programName,
            gradeName,
            levelName,
            metadata.LearningGoal,
            tags,
            components,
            outcomes,
            quickChecks.Count,
            editor.Document,
            blockers);
    }
}
