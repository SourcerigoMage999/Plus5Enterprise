using System.Data;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialSharingService(
    Plus5DbContext db,
    TimeProvider timeProvider) : IMaterialSharingService
{
    public async Task<MaterialSharingResult> SaveAsync(
        Guid teacherAccountId,
        Guid materialId,
        MaterialSharingCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(command, out var expectedRowVersion, out var requestedGrants))
        {
            return Result(MaterialSharingOutcome.InvalidInput);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var material = await db.Materials.SingleOrDefaultAsync(candidate =>
                candidate.Id == materialId
                && candidate.OwnerTeacherId == teacherAccountId
                && candidate.Status == MaterialStatus.Active
                && candidate.ArchivedAtUtc == null,
                cancellationToken);
            if (material is null || !await HasUsableCurrentVersionAsync(material, cancellationToken))
            {
                return Result(MaterialSharingOutcome.NotFound);
            }

            if (!material.RowVersion.SequenceEqual(expectedRowVersion))
            {
                return Result(MaterialSharingOutcome.Conflict);
            }

            var ownerEmail = await db.UserAccounts.AsNoTracking()
                .Where(account => account.Id == teacherAccountId)
                .Select(account => account.NormalizedEmail)
                .SingleAsync(cancellationToken);
            var normalizedEmails = requestedGrants.Keys.ToList();
            var recipients = normalizedEmails.Count == 0
                ? []
                : await db.UserAccounts
                    .Where(account => normalizedEmails.Contains(account.NormalizedEmail)
                        && account.Status == AccountStatus.Active)
                    .ToListAsync(cancellationToken);
            if (requestedGrants.ContainsKey(ownerEmail)
                || recipients.Count != normalizedEmails.Count)
            {
                return Result(MaterialSharingOutcome.InvalidRecipient);
            }

            var now = timeProvider.GetUtcNow();
            if (now <= material.UpdatedAtUtc)
            {
                now = material.UpdatedAtUtc.AddTicks(1);
            }
            var visibility = command.Visibility == MaterialSharingVisibility.Shared
                ? MaterialVisibility.Shared
                : MaterialVisibility.Private;

            if (visibility == MaterialVisibility.Shared || material.Visibility == MaterialVisibility.Private)
            {
                material.SetVisibility(visibility, now);
            }
            else
            {
                material.RecordVersionChange(now);
            }

            await db.SaveChangesAsync(cancellationToken);

            var existing = await db.MaterialShares
                .Where(share => share.MaterialId == materialId)
                .ToListAsync(cancellationToken);

            if (visibility == MaterialVisibility.Private)
            {
                db.MaterialShares.RemoveRange(existing);
                await db.SaveChangesAsync(cancellationToken);
                material.SetVisibility(MaterialVisibility.Private, now);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var recipientByEmail = recipients.ToDictionary(
                    account => account.NormalizedEmail,
                    StringComparer.Ordinal);
                var requestedById = requestedGrants.ToDictionary(
                    pair => recipientByEmail[pair.Key].Id,
                    pair => pair.Value);

                db.MaterialShares.RemoveRange(existing.Where(share =>
                    !requestedById.ContainsKey(share.SharedWithTeacherId)));

                foreach (var pair in requestedById)
                {
                    var current = existing.SingleOrDefault(share =>
                        share.SharedWithTeacherId == pair.Key);
                    var permission = pair.Value == MaterialSharingAccess.Use
                        ? MaterialShareAccess.Use
                        : MaterialShareAccess.View;
                    if (current is null)
                    {
                        db.MaterialShares.Add(new MaterialShare(material, pair.Key, permission, now));
                    }
                    else if (current.Permission != permission)
                    {
                        current.ChangePermission(permission, now);
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result(MaterialSharingOutcome.Success);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result(MaterialSharingOutcome.Conflict);
        }
    }

    private Task<bool> HasUsableCurrentVersionAsync(
        Material material,
        CancellationToken cancellationToken) =>
        material.CurrentVersionId.HasValue
            ? db.MaterialVersions.AnyAsync(version =>
                version.Id == material.CurrentVersionId
                && version.MaterialId == material.Id
                && version.Status == MaterialVersionStatus.Active
                && db.MaterialFiles.Any(file =>
                    file.MaterialVersionId == version.Id
                    && file.Status == MaterialFileStatus.Clean),
                cancellationToken)
            : Task.FromResult(false);

    private static bool TryValidate(
        MaterialSharingCommand command,
        out byte[] expectedRowVersion,
        out Dictionary<string, MaterialSharingAccess> requestedGrants)
    {
        expectedRowVersion = [];
        requestedGrants = new(StringComparer.Ordinal);
        if (!Enum.IsDefined(command.Visibility)
            || command.Grants is null
            || !TryRowVersion(command.ExpectedRowVersion, out expectedRowVersion)
            || command.Visibility == MaterialSharingVisibility.Private && command.Grants.Count != 0)
        {
            return false;
        }

        foreach (var grant in command.Grants)
        {
            if (!Enum.IsDefined(grant.Access)
                || !TryNormalizeEmail(grant.RecipientEmail, out var normalizedEmail)
                || !requestedGrants.TryAdd(normalizedEmail, grant.Access))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryNormalizeEmail(string value, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;
        var candidate = value?.Trim();
        if (string.IsNullOrWhiteSpace(candidate)
            || candidate.Length > 320
            || !MailAddress.TryCreate(candidate, out var parsed)
            || !string.Equals(candidate, parsed.Address, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        normalizedEmail = parsed.Address.ToUpperInvariant();
        return true;
    }

    private static bool TryRowVersion(string value, out byte[] rowVersion)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 24)
        {
            rowVersion = [];
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static MaterialSharingResult Result(MaterialSharingOutcome outcome) => new(outcome);
}
