namespace Plus5.Application.Scheduling;

public sealed record ScheduleSessionParticipant(
    Guid Id,
    string FirstName,
    string LastName,
    Guid SchoolGradeId,
    string SchoolGrade,
    int Status);

public sealed record ScheduleSessionDetail(
    Guid Id,
    int DeliveryMode,
    Guid? GroupId,
    Guid? StudentId,
    string ContextName,
    Guid? ProgramId,
    string? ProgramName,
    Guid SchoolGradeId,
    string SchoolGrade,
    int? GroupStatus,
    int? Capacity,
    string? Title,
    string? Notes,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    Guid? LocationId,
    string? LocationName,
    bool Online,
    int Status,
    bool IsSeriesOccurrence,
    bool IsSeriesException,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    IReadOnlyList<ScheduleSessionParticipant> Participants);

public interface IScheduleSessionDetailQuery
{
    Task<ScheduleSessionDetail?> GetAsync(
        Guid owner,
        Guid sessionId,
        CancellationToken cancellationToken);
}
