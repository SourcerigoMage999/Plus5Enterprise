namespace Plus5.Application.Readiness;

public enum ReadinessRefreshRunStatus
{
    Completed = 1,
    LeaseSkipped = 2,
    LeaseLost = 3,
}

public sealed record ReadinessRefreshRunResult(
    ReadinessRefreshRunStatus Status,
    int StudentsRecalculated);

public interface IMasteryReadinessProjectionService
{
    Task RecalculateAffectedAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> knowledgeComponentIds,
        CancellationToken cancellationToken);
}

public interface IReadinessRefreshService
{
    Task<ReadinessRefreshRunResult> RunAsync(
        string leaseOwnerId,
        CancellationToken cancellationToken);
}
