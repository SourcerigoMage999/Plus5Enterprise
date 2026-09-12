using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
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
        group.MapPost("/", CreateAsync).AddEndpointFilter(async (context, next) =>
        {
            try
            {
                await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>()
                    .ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Problem(400, "invalid_csrf_token");
            }

            return await next(context);
        });

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

    private static async Task<IResult> CreateAsync(
        CreateRequest request,
        HttpContext context,
        IScheduleCreationService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.CreateAsync(owner, new ScheduleCreateCommand(
            request.DeliveryMode,
            request.ContextId,
            request.Title,
            request.Notes,
            request.Date,
            request.StartsAt,
            request.EndsAt,
            request.RepeatWeekly,
            request.EndsOn,
            request.LocationId,
            request.OnlineMeetingUrl), cancellationToken);

        return result.Failure switch
        {
            ScheduleCreateFailure.None => TypedResults.Created(
                $"/api/v1/schedule/{result.SessionId}",
                new CreatedResponse(result.SessionId!.Value, result.SessionCount)),
            ScheduleCreateFailure.Invalid => Problem(400, "invalid_request"),
            ScheduleCreateFailure.NotFound => Problem(404, "schedule_context_not_found"),
            ScheduleCreateFailure.Unavailable => Problem(409, "schedule_context_unavailable"),
            ScheduleCreateFailure.ScheduleConflict => Problem(409, "schedule_conflict"),
            ScheduleCreateFailure.InvalidLocalTime => Problem(400, "invalid_local_time"),
            _ => Problem(409, "concurrency_conflict"),
        };
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

    public sealed record CreateRequest : IValidatableObject
    {
        [Range(1, 2)]
        public int DeliveryMode { get; init; }

        public Guid ContextId { get; init; }

        [StringLength(200)]
        public string? Title { get; init; }

        [StringLength(2000)]
        public string? Notes { get; init; }

        public DateOnly Date { get; init; }

        public TimeOnly StartsAt { get; init; }

        public TimeOnly EndsAt { get; init; }

        public bool RepeatWeekly { get; init; }

        public DateOnly? EndsOn { get; init; }

        public Guid? LocationId { get; init; }

        [StringLength(2048)]
        public string? OnlineMeetingUrl { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ContextId == Guid.Empty)
            {
                yield return new("A group or student is required.", [nameof(ContextId)]);
            }

            if (Date == default)
            {
                yield return new("A date is required.", [nameof(Date)]);
            }

            if (EndsAt <= StartsAt)
            {
                yield return new("The end time must be after the start time.", [nameof(EndsAt)]);
            }

            if (LocationId == Guid.Empty)
            {
                yield return new("The location identifier is invalid.", [nameof(LocationId)]);
            }

            if (LocationId.HasValue && !string.IsNullOrWhiteSpace(OnlineMeetingUrl))
            {
                yield return new("A physical and online location cannot be selected together.",
                    [nameof(LocationId), nameof(OnlineMeetingUrl)]);
            }

            if (!string.IsNullOrWhiteSpace(OnlineMeetingUrl)
                && (!Uri.TryCreate(OnlineMeetingUrl.Trim(), UriKind.Absolute, out var uri)
                    || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new("The online meeting URL must use HTTPS.", [nameof(OnlineMeetingUrl)]);
            }

            if (RepeatWeekly && DeliveryMode != 1)
            {
                yield return new("A regular group schedule is managed from the group workflow.",
                    [nameof(RepeatWeekly)]);
            }

            if (RepeatWeekly && EndsOn.HasValue && EndsOn < Date)
            {
                yield return new("The recurrence end date cannot precede the first date.", [nameof(EndsOn)]);
            }

            if (!RepeatWeekly && EndsOn.HasValue)
            {
                yield return new("An end date is only valid for a recurring session.", [nameof(EndsOn)]);
            }
        }
    }

    public sealed record CreatedResponse(Guid Id, int SessionCount);

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
