using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialSharingQuery(Plus5DbContext db) : IMaterialSharingQuery
{
    public async Task<MaterialSharingWorkspace?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var material = await (
            from candidate in db.Materials.AsNoTracking()
            join version in db.MaterialVersions.AsNoTracking()
                on candidate.CurrentVersionId equals version.Id
            join file in db.MaterialFiles.AsNoTracking()
                on version.Id equals file.MaterialVersionId
            where candidate.Id == materialId
                && candidate.OwnerTeacherId == teacherAccountId
                && candidate.Status == MaterialStatus.Active
                && candidate.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Active
                && file.Status == MaterialFileStatus.Clean
            select new
            {
                candidate.Id,
                version.Title,
                candidate.RowVersion,
                candidate.Visibility,
            }).SingleOrDefaultAsync(cancellationToken);

        if (material is null)
        {
            return null;
        }

        var grants = await (
            from share in db.MaterialShares.AsNoTracking()
            join account in db.UserAccounts.AsNoTracking()
                on share.SharedWithTeacherId equals account.Id
            where share.MaterialId == material.Id
            orderby account.NormalizedEmail
            select new MaterialSharingGrant(
                account.Id,
                account.Email,
                share.Permission == MaterialShareAccess.Use
                    ? MaterialSharingAccess.Use
                    : MaterialSharingAccess.View))
            .ToListAsync(cancellationToken);

        return new MaterialSharingWorkspace(
            material.Id,
            material.Title,
            Convert.ToBase64String(material.RowVersion),
            material.Visibility == MaterialVisibility.Shared
                ? MaterialSharingVisibility.Shared
                : MaterialSharingVisibility.Private,
            grants);
    }
}
