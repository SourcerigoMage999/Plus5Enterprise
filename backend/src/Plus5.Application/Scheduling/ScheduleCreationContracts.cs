namespace Plus5.Application.Scheduling;

public sealed record ScheduleCreateCommand(
    int DeliveryMode,
    Guid ContextId,
    string? Title,
    string? Notes,
    DateOnly Date,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    bool RepeatWeekly,
    DateOnly? EndsOn,
    Guid? LocationId,
    string? OnlineMeetingUrl);

public enum ScheduleCreateFailure
{
    None,
    Invalid,
    NotFound,
    Unavailable,
    ScheduleConflict,
    InvalidLocalTime,
    ConcurrencyConflict,
}

public sealed record ScheduleCreateResult(
    Guid? SessionId,
    ScheduleCreateFailure Failure,
    int SessionCount = 0);

public interface IScheduleCreationService
{
    Task<ScheduleCreateResult> CreateAsync(
        Guid owner,
        ScheduleCreateCommand command,
        CancellationToken cancellationToken);
}
