namespace Plus5.Application.LessonSessions;

public sealed record LessonSessionActivityView(
    Guid Id,
    Guid? SourceLessonActivityId,
    int SortOrder,
    string Origin,
    string Status,
    string Name,
    string Purpose,
    int DurationMinutes,
    string WorkMode,
    string? Description,
    string? StudentInstructions,
    bool ShowStudentInstructionsOnBoard,
    bool ShowProgressOnBoard,
    IReadOnlyList<Guid> MaterialVersionIds,
    IReadOnlyList<Guid> AssessableTaskVersionIds);

public sealed record LessonSessionView(
    Guid Id,
    Guid LessonPlanId,
    Guid LessonPlanVersionId,
    int LessonPlanVersionNumber,
    Guid? CalendarSessionId,
    string ParticipantType,
    string ParticipantName,
    string Goal,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ScheduledStartsAtUtc,
    DateTimeOffset? ScheduledEndsAtUtc,
    string? ScheduledTimeZoneId,
    DateTimeOffset LastCommittedAtUtc,
    long Revision,
    string RowVersion,
    IReadOnlyList<LessonSessionActivityView> Activities);

public sealed record ActiveLessonSessionView(
    Guid Id,
    Guid LessonPlanId,
    Guid LessonPlanVersionId,
    Guid? CalendarSessionId,
    string ParticipantType,
    string ParticipantName,
    string Goal,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastCommittedAtUtc,
    long Revision,
    int ActivityCount);

public enum LessonSessionFailure
{
    None = 0,
    Invalid = 1,
    NotFound = 2,
    NotReady = 3,
    CalendarUnavailable = 4,
    ActiveConflict = 5,
    OperationMismatch = 6,
    ConcurrencyConflict = 7,
    InvalidMutation = 8,
}

public sealed record LessonSessionResult(
    LessonSessionView? View,
    LessonSessionFailure Failure,
    bool Created = false);

public interface ILessonSessionService
{
    Task<LessonSessionResult> StartAsync(Guid teacherId, Guid lessonPlanId,
        Guid lessonPlanVersionId, Guid? calendarSessionId, Guid operationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveLessonSessionView>> GetActiveAsync(Guid teacherId,
        CancellationToken cancellationToken);

    Task<LessonSessionResult> GetAsync(Guid teacherId, Guid lessonSessionId,
        CancellationToken cancellationToken);

    Task<LessonSessionResult> ReorderTimelineAsync(Guid teacherId, Guid lessonSessionId,
        Guid operationId, long expectedRevision, IReadOnlyList<Guid> activityIds,
        CancellationToken cancellationToken);
}
