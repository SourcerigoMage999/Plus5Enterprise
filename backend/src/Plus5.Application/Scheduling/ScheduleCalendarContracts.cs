namespace Plus5.Application.Scheduling;

public sealed record ScheduleCalendarCriteria(
    DateOnly From,
    DateOnly To,
    Guid? GroupId = null,
    Guid? ProgramId = null,
    Guid? LocationId = null);

public sealed record ScheduleCalendarItem(
    Guid Id,
    int DeliveryMode,
    Guid? GroupId,
    Guid? StudentId,
    string ContextName,
    Guid? ProgramId,
    string? ProgramName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    Guid? LocationId,
    string? LocationName,
    bool Online,
    int Status,
    int MemberCount,
    int? Capacity);

public sealed record ScheduleCalendarSummary(
    int GroupSessions,
    int IndividualSessions,
    int TotalSessions,
    int UniqueStudents,
    int PlannedAttendances,
    int AvailableSeats);

public sealed record ScheduleCalendarOption(Guid Id, string Name);

public sealed record ScheduleCalendar(
    string TimeZoneId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ScheduleCalendarItem> Items,
    IReadOnlyList<ScheduleCalendarItem> Reminders,
    ScheduleCalendarSummary Summary,
    IReadOnlyList<ScheduleCalendarOption> Groups,
    IReadOnlyList<ScheduleCalendarOption> Programs,
    IReadOnlyList<ScheduleCalendarOption> Locations);

public interface IScheduleCalendarQuery
{
    Task<ScheduleCalendar> GetAsync(
        Guid owner,
        ScheduleCalendarCriteria criteria,
        CancellationToken cancellationToken);
}
