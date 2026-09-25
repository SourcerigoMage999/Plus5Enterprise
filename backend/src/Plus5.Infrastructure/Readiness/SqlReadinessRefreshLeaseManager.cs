using System.Data;
using Microsoft.EntityFrameworkCore;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Readiness;

internal sealed class SqlReadinessRefreshLeaseManager(
    DbContextOptions<Plus5DbContext> dbOptions,
    TimeProvider clock)
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(15);
    private const string LeaseName = "mastery-readiness-refresh";

    public Task<bool> TryAcquireAsync(string ownerId, CancellationToken cancellationToken) =>
        ChangeLeaseAsync(ownerId, acquire: true, cancellationToken);

    public Task<bool> TryRenewAsync(string ownerId, CancellationToken cancellationToken) =>
        ChangeLeaseAsync(ownerId, acquire: false, cancellationToken);

    public async Task ReleaseAsync(string ownerId, CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var lease = await db.ScheduleMaterializationLeases.SingleOrDefaultAsync(
            item => item.Name == LeaseName,
            cancellationToken);
        if (lease?.TryRelease(ownerId, clock.GetUtcNow()) == true)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> ChangeLeaseAsync(
        string ownerId,
        bool acquire,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var now = clock.GetUtcNow();
        var lease = await db.ScheduleMaterializationLeases.SingleOrDefaultAsync(
            item => item.Name == LeaseName,
            cancellationToken);
        if (lease is null)
        {
            if (!acquire)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            db.ScheduleMaterializationLeases.Add(new ScheduleMaterializationLease(
                LeaseName,
                ownerId,
                now.Add(LeaseDuration),
                now));
        }
        else
        {
            var changed = acquire
                ? lease.TryAcquire(ownerId, now.Add(LeaseDuration), now)
                : lease.TryRenew(ownerId, now.Add(LeaseDuration), now);
            if (!changed)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
