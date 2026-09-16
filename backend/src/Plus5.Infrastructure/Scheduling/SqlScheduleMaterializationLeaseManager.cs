using System.Data;
using Microsoft.EntityFrameworkCore;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

internal sealed class SqlScheduleMaterializationLeaseManager(
    DbContextOptions<Plus5DbContext> dbOptions,
    TimeProvider clock)
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(15);
    private const string LeaseName = "schedule-materialization";

    public async Task<bool> TryAcquireAsync(
        string ownerId,
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
            db.ScheduleMaterializationLeases.Add(new ScheduleMaterializationLease(
                LeaseName,
                ownerId,
                now.Add(LeaseDuration),
                now));
        }
        else if (!lease.TryAcquire(ownerId, now.Add(LeaseDuration), now))
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRenewAsync(
        string ownerId,
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
        if (lease is null || !lease.TryRenew(ownerId, now.Add(LeaseDuration), now))
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseAsync(
        string ownerId,
        CancellationToken cancellationToken)
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
}
