using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plus5.Application.Readiness;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Readiness;

public sealed partial class EfReadinessRefreshService(
    DbContextOptions<Plus5DbContext> dbOptions,
    TimeProvider clock,
    ILogger<EfReadinessRefreshService> logger) : IReadinessRefreshService
{
    private const int BatchSize = 100;

    public async Task<ReadinessRefreshRunResult> RunAsync(
        string leaseOwnerId,
        CancellationToken cancellationToken)
    {
        var lease = new SqlReadinessRefreshLeaseManager(dbOptions, clock);
        if (!await lease.TryAcquireAsync(leaseOwnerId, cancellationToken))
        {
            return new(ReadinessRefreshRunStatus.LeaseSkipped, 0);
        }

        var recalculated = 0;
        try
        {
            Guid? afterStudentId = null;
            while (true)
            {
                var studentIds = await LoadStudentBatchAsync(afterStudentId, cancellationToken);
                if (studentIds.Length == 0)
                {
                    break;
                }

                foreach (var studentId in studentIds)
                {
                    if (!await lease.TryRenewAsync(leaseOwnerId, cancellationToken))
                    {
                        LogLeaseLost(logger, leaseOwnerId);
                        return new(ReadinessRefreshRunStatus.LeaseLost, recalculated);
                    }

                    await RecalculateStudentAsync(studentId, cancellationToken);
                    recalculated++;
                }

                afterStudentId = studentIds[^1];
            }

            return new(ReadinessRefreshRunStatus.Completed, recalculated);
        }
        finally
        {
            await lease.ReleaseAsync(leaseOwnerId, CancellationToken.None);
        }
    }

    private async Task<Guid[]> LoadStudentBatchAsync(
        Guid? afterStudentId,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        return await db.EvidenceEvents.AsNoTracking()
            .Where(evidence => !afterStudentId.HasValue || evidence.StudentId.CompareTo(afterStudentId.Value) > 0)
            .Select(evidence => evidence.StudentId)
            .Distinct()
            .OrderBy(studentId => studentId)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);
    }

    private async Task RecalculateStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var componentIds = await (
            from evidence in db.EvidenceEvents.AsNoTracking()
            join mapping in db.EvidenceEventKnowledgeComponents.AsNoTracking()
                on evidence.Id equals mapping.EvidenceEventId
            where evidence.StudentId == studentId
            select mapping.KnowledgeComponentId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        await MasteryProjectionWriter.RecalculateAsync(
            db,
            studentId,
            componentIds,
            clock.GetUtcNow(),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    [LoggerMessage(4604, LogLevel.Warning,
        "Readiness refresh lease was lost by {LeaseOwnerId}; the run is stopping.")]
    private static partial void LogLeaseLost(ILogger logger, string leaseOwnerId);
}
