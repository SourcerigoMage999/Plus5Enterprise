namespace Plus5.Application.LessonPlanning;

public enum LessonParticipantKind
{
    Student = 1,
    Group = 2,
}

public sealed record LessonGoalParticipant(
    Guid Id,
    string Name,
    string SchoolGrade,
    string? ProgramName,
    int? MemberCount);

public sealed record LessonGoalSession(
    Guid Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    string? Title);

public sealed record LessonKnowledgeComponentOption(
    Guid Id,
    string Name,
    string AreaName,
    string ModelCode,
    string ModelVersion);

public sealed record LessonGoalPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface ILessonGoalSelectionQuery
{
    Task<LessonGoalPage<LessonGoalParticipant>> GetParticipantsAsync(
        Guid teacherId,
        LessonParticipantKind kind,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonGoalSession>> GetSessionsAsync(
        Guid teacherId,
        LessonParticipantKind kind,
        Guid participantId,
        CancellationToken cancellationToken);

    Task<LessonGoalPage<LessonKnowledgeComponentOption>> GetKnowledgeComponentsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);
}

public sealed record CreateLessonGoalCommand(
    LessonParticipantKind ParticipantKind,
    Guid ParticipantId,
    Guid? SessionId,
    string Goal,
    IReadOnlyList<Guid> KnowledgeComponentIds);

public enum CreateLessonGoalFailure
{
    None,
    Invalid,
    ReferenceNotFound,
    ParticipantUnavailable,
    SessionMismatch,
    KnowledgeUnavailable,
    Conflict,
}

public sealed record CreateLessonGoalResult(
    Guid? LessonPlanId,
    Guid? LessonPlanVersionId,
    CreateLessonGoalFailure Failure);

public interface ILessonGoalSelectionService
{
    Task<CreateLessonGoalResult> CreateAsync(
        Guid teacherId,
        CreateLessonGoalCommand command,
        CancellationToken cancellationToken);
}
