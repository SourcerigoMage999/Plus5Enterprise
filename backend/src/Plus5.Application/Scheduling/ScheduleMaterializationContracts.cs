namespace Plus5.Application.Scheduling;

public enum ScheduleMaterializationRunStatus
{
    Completed = 1,
    LeaseSkipped = 2,
    LeaseLost = 3,
}

public sealed record ScheduleMaterializationRunResult(
    ScheduleMaterializationRunStatus Status,
    int SeriesScanned,
    int SessionsCreated,
    int OccurrencesSkipped,
    int ConflictsFound,
    int IssuesRecorded,
    int IssuesResolved);

public interface IScheduleMaterializationService
{
    Task<ScheduleMaterializationRunResult> RunAsync(
        string leaseOwnerId,
        CancellationToken cancellationToken);
}
