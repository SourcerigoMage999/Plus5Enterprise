using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationEditorQuery(Plus5DbContext db) : IPresentationEditorQuery
{
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
            var mappedTaskVersionIds = await db.AssessableTaskVersionKnowledgeComponents
                .AsNoTracking()
                .Where(mapping => taskVersionIds.Contains(mapping.AssessableTaskVersionId))
                .Select(mapping => mapping.AssessableTaskVersionId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var mappedTaskVersions = mappedTaskVersionIds.ToHashSet();
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
                                !mappedTaskVersions.Contains(taskVersion.Id))
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
}
