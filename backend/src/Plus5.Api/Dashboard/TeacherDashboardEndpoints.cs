using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Dashboard;

namespace Plus5.Api.Dashboard;

public static class TeacherDashboardEndpoints
{
    public static IEndpointRouteBuilder MapTeacherDashboard(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapVersionOneApi()
            .MapGet("/dashboard", GetAsync)
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext context,
        ITeacherDashboardQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var dashboard = await query.GetAsync(teacherAccountId, cancellationToken);
        return TypedResults.Ok(Map(dashboard));
    }

    internal static DashboardResponse Map(TeacherDashboard dashboard) => new(
        dashboard.TimeZoneId,
        dashboard.Today,
        dashboard.GeneratedAtUtc,
        dashboard.TodaySessions.Select(MapSession).ToList(),
        dashboard.NextSession is null ? null : MapSession(dashboard.NextSession),
        new StudentOverviewResponse(
            dashboard.Students.TotalCount,
            dashboard.Students.ActiveCount,
            dashboard.Students.OnHoldCount,
            dashboard.Students.InactiveCount));

    private static SessionResponse MapSession(TeacherDashboardSession session) => new(
        session.Id,
        session.DeliveryMode,
        session.GroupId,
        session.StudentId,
        session.ContextName,
        session.SchoolGradeName,
        session.ProgramId,
        session.ProgramName,
        session.Title,
        session.StartsAtUtc,
        session.EndsAtUtc,
        session.TimeZoneId,
        session.LocationId,
        session.LocationName,
        session.Online,
        session.Status,
        session.ParticipantCount);

    public sealed record DashboardResponse(
        string TimeZoneId,
        DateOnly Today,
        DateTimeOffset GeneratedAtUtc,
        IReadOnlyList<SessionResponse> TodaySessions,
        SessionResponse? NextSession,
        StudentOverviewResponse Students);

    public sealed record SessionResponse(
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

    public sealed record StudentOverviewResponse(
        int TotalCount,
        int ActiveCount,
        int OnHoldCount,
        int InactiveCount);
}
