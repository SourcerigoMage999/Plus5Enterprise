namespace Plus5.Application.Dashboard;

public sealed record TeacherDashboardSession(
    Guid Id,
    int DeliveryMode,
    Guid? GroupId,
    Guid? StudentId,
    string ContextName,
    string SchoolGradeName,
    Guid? ProgramId,
    string? ProgramName,
    string? Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    Guid? LocationId,
    string? LocationName,
    bool Online,
    int Status,
    int ParticipantCount);

public sealed record TeacherDashboardStudentOverview(
    int TotalCount,
    int ActiveCount,
    int OnHoldCount,
    int InactiveCount);

public sealed record TeacherDashboard(
    string TimeZoneId,
    DateOnly Today,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<TeacherDashboardSession> TodaySessions,
    TeacherDashboardSession? NextSession,
    TeacherDashboardStudentOverview Students);

public interface ITeacherDashboardQuery
{
    Task<TeacherDashboard> GetAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken);
}
