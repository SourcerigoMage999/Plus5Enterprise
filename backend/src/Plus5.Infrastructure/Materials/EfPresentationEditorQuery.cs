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
                .Select(slide => new PresentationSlideView(slide.Id, slide.SortOrder))
                .ToListAsync(cancellationToken);
            documentView = new PresentationDocumentView(
                document.DocumentSchemaVersion,
                document.CanvasWidth,
                document.CanvasHeight,
                Convert.ToBase64String(document.RowVersion),
                document.UpdatedAtUtc,
                slides);
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
