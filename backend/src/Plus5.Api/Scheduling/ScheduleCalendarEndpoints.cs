using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Scheduling;

namespace Plus5.Api.Scheduling;

public static class ScheduleCalendarEndpoints
{
    private const int MaximumRangeDays = 31;

    public static IEndpointRouteBuilder MapScheduleCalendar(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi()
            .MapGroup("/schedule")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/", GetAsync);
        group.MapGet("/{sessionId:guid}", DetailAsync);

        return endpoints;
    }

    private static async Task<IResult> DetailAsync(
        Guid sessionId,
        HttpContext context,
        IScheduleSessionDetailQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _))
        {
            return TypedResults.Unauthorized();
        }

        var item = await query.GetAsync(owner, sessionId, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<IResult> GetAsync(
        [AsParameters] CalendarRequest request,
        HttpContext context,
        IScheduleCalendarQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (!request.From.HasValue || !request.To.HasValue
            || request.To.Value <= request.From.Value
            || request.To.Value.DayNumber - request.From.Value.DayNumber > MaximumRangeDays
            || request.GroupId == Guid.Empty
            || request.ProgramId == Guid.Empty
            || request.LocationId == Guid.Empty)
        {
            return Problem(400, "invalid_calendar_range");
        }

        var calendar = await query.GetAsync(owner, new ScheduleCalendarCriteria(
            request.From.Value,
            request.To.Value,
            request.GroupId,
            request.ProgramId,
            request.LocationId), cancellationToken);

        return TypedResults.Ok(new CalendarResponse(
            calendar.TimeZoneId,
            calendar.From,
            calendar.To,
            calendar.Items.Select(MapItem).ToList(),
            calendar.Reminders.Select(MapItem).ToList(),
            new SummaryResponse(
                calendar.Summary.GroupSessions,
                calendar.Summary.IndividualSessions,
                calendar.Summary.TotalSessions,
                calendar.Summary.UniqueStudents,
                calendar.Summary.PlannedAttendances,
                calendar.Summary.AvailableSeats),
            calendar.Groups.Select(MapOption).ToList(),
            calendar.Programs.Select(MapOption).ToList(),
            calendar.Locations.Select(MapOption).ToList()));
    }

    private static CalendarItemResponse MapItem(ScheduleCalendarItem item) => new(
        item.Id,
        item.DeliveryMode,
        item.GroupId,
        item.StudentId,
        item.ContextName,
        item.ProgramId,
        item.ProgramName,
        item.StartsAtUtc,
        item.EndsAtUtc,
        item.TimeZoneId,
        item.LocationId,
        item.LocationName,
        item.Online,
        item.Status,
        item.MemberCount,
        item.Capacity);

    private static OptionResponse MapOption(ScheduleCalendarOption item) => new(item.Id, item.Name);

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Calendar request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record CalendarRequest(
        DateOnly? From,
        DateOnly? To,
        Guid? GroupId,
        Guid? ProgramId,
        Guid? LocationId);

    public sealed record CalendarItemResponse(
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

    public sealed record SummaryResponse(
        int GroupSessions,
        int IndividualSessions,
        int TotalSessions,
        int UniqueStudents,
        int PlannedAttendances,
        int AvailableSeats);

    public sealed record OptionResponse(Guid Id, string Name);

    public sealed record CalendarResponse(
        string TimeZoneId,
        DateOnly From,
        DateOnly To,
        IReadOnlyList<CalendarItemResponse> Items,
        IReadOnlyList<CalendarItemResponse> Reminders,
        SummaryResponse Summary,
        IReadOnlyList<OptionResponse> Groups,
        IReadOnlyList<OptionResponse> Programs,
        IReadOnlyList<OptionResponse> Locations);
}
