namespace Plus5.Application.Scheduling;

public enum ScheduleEditScope
{
    OneOccurrence = 1,
    FutureSeries = 2,
}

public sealed record ScheduleEditItem(
    ScheduleSessionDetail Detail,
    string? OnlineMeetingUrl,
    byte[] RowVersion,
    bool CanEditFutureSeries);

public sealed record ScheduleEditCommand(
    string? Title,
    string? Notes,
    DateOnly Date,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? LocationId,
    string? OnlineMeetingUrl,
    int Scope,
    byte[] RowVersion);

public enum ScheduleEditFailure
{
    None,
    Invalid,
    NotFound,
    Unavailable,
    ScheduleConflict,
    InvalidLocalTime,
    ConcurrencyConflict,
}

public sealed record ScheduleEditPreviewResult(
    ScheduleEditFailure Failure,
    bool HasConflict = false);

public sealed record ScheduleEditResult(
    Guid? SessionId,
    ScheduleEditFailure Failure,
    int SessionCount = 0);

public interface IScheduleEditingQuery
{
    Task<ScheduleEditItem?> GetAsync(
        Guid owner,
        Guid sessionId,
        CancellationToken cancellationToken);
}

public interface IScheduleEditingService
{
    Task<ScheduleEditPreviewResult> PreviewAsync(
        Guid owner,
        Guid sessionId,
        ScheduleEditCommand command,
        CancellationToken cancellationToken);

    Task<ScheduleEditResult> UpdateAsync(
        Guid owner,
        Guid sessionId,
        ScheduleEditCommand command,
        CancellationToken cancellationToken);

    Task<ScheduleEditFailure> CancelAsync(
        Guid owner,
        Guid sessionId,
        byte[] rowVersion,
        CancellationToken cancellationToken);
}
