using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialEditingQuery(Plus5DbContext db, IMaterialImportQuery optionsQuery) : IMaterialEditingQuery
{
    public async Task<MaterialEditWorkspace?> GetAsync(Guid teacherAccountId, Guid materialId, CancellationToken cancellationToken)
    {
        var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(candidate =>
            candidate.Id == materialId && candidate.OwnerTeacherId == teacherAccountId
            && candidate.Status == MaterialStatus.Active && candidate.ArchivedAtUtc == null, cancellationToken);
        if (material?.CurrentVersionId is null) return null;

        var versions = await db.MaterialVersions.AsNoTracking()
            .Where(version => version.MaterialId == materialId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        var editable = versions.FirstOrDefault(version => version.Status == MaterialVersionStatus.Draft)
            ?? versions.Single(version => version.Id == material.CurrentVersionId);
        var snapshot = await LoadSnapshotAsync(materialId, editable.Id, cancellationToken);
        if (snapshot is null) return null;

        return new MaterialEditWorkspace(
            material.Id,
            Convert.ToBase64String(material.RowVersion),
            material.Visibility.ToString().ToLowerInvariant(),
            material.CurrentVersionId.Value,
            snapshot,
            versions.Select(version => new MaterialVersionHistoryItem(
                version.Id, version.VersionNumber, version.Status.ToString().ToLowerInvariant(),
                version.Title, version.CreatedAtUtc, version.ActivatedAtUtc, version.SupersededAtUtc,
                version.Id == material.CurrentVersionId)).ToList(),
            await optionsQuery.GetOptionsAsync(teacherAccountId, cancellationToken));
    }

    public async Task<MaterialVersionSnapshot?> GetVersionAsync(Guid teacherAccountId, Guid materialId, Guid versionId, CancellationToken cancellationToken)
    {
        var owns = await db.Materials.AsNoTracking().AnyAsync(material => material.Id == materialId
            && material.OwnerTeacherId == teacherAccountId && material.Status == MaterialStatus.Active
            && material.ArchivedAtUtc == null, cancellationToken);
        return owns ? await LoadSnapshotAsync(materialId, versionId, cancellationToken) : null;
    }

    private async Task<MaterialVersionSnapshot?> LoadSnapshotAsync(Guid materialId, Guid versionId, CancellationToken cancellationToken)
    {
        var row = await (
            from version in db.MaterialVersions.AsNoTracking()
            join file in db.MaterialFiles.AsNoTracking() on version.Id equals file.MaterialVersionId
            where version.MaterialId == materialId && version.Id == versionId && file.Status == MaterialFileStatus.Clean
            select new { version, file }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var tags = await db.MaterialVersionTags.AsNoTracking().Where(x => x.MaterialVersionId == versionId).OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(cancellationToken);
        var componentIds = await db.MaterialVersionKnowledgeComponents.AsNoTracking().Where(x => x.MaterialVersionId == versionId).Select(x => x.KnowledgeComponentId).ToListAsync(cancellationToken);
        var outcomeIds = await db.MaterialVersionCurriculumOutcomes.AsNoTracking().Where(x => x.MaterialVersionId == versionId).Select(x => x.CurriculumOutcomeId).ToListAsync(cancellationToken);
        var taskCount = await db.AssessableTaskVersions.AsNoTracking().CountAsync(x => x.MaterialVersionId == versionId, cancellationToken);
        return new MaterialVersionSnapshot(
            row.version.Id, row.version.VersionNumber, row.version.Status.ToString().ToLowerInvariant(),
            row.version.Title, row.version.Description, row.version.MaterialTypeCode,
            row.version.Subject, row.version.LanguageCode, row.version.ProgramId,
            row.version.SchoolGradeId, row.version.ProficiencyLevelId, row.version.LearningGoal,
            new MaterialEditFile(row.file.Format.ToString().ToLowerInvariant(), row.file.OriginalFileName,
                row.file.DeclaredMediaType, row.file.ActualSizeBytes ?? row.file.DeclaredSizeBytes),
            tags, componentIds, outcomeIds, taskCount);
    }
}
