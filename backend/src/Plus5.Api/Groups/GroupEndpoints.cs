using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Contracts;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Groups;

namespace Plus5.Api.Groups;

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroups(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapVersionOneApi().MapGroup("/groups")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/", Page);
        group.MapGet("/overview", Overview);
        group.MapGet("/{groupId:guid}", Detail);
        group.MapGet("/{groupId:guid}/students", Students);
        group.MapGet("/{groupId:guid}/candidates", Candidates);
        group.MapGet("/{groupId:guid}/sessions", Sessions);
        group.MapPost("/{groupId:guid}/members/{studentId:guid}", Membership).RequireGroupCsrf();
        return endpoints;
    }

    private static async Task<IResult> Page([AsParameters] Request request, HttpContext context, IGroupQuery query, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        return TypedResults.Ok(Map(await query.GetPageAsync(owner, Criteria(request), ct)));
    }
    private static async Task<IResult> Overview(HttpContext context, IGroupQuery query, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var item = await query.GetOverviewAsync(owner, ct);
        return TypedResults.Ok(new OverviewResponse(item.TotalGroups, item.ActiveGroups, item.Students, item.AvailableSeats,
            item.SessionsThisWeek, item.WeekStartsOn));
    }
    private static async Task<IResult> Detail(Guid groupId, HttpContext context, IGroupQuery query, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var item = await query.GetAsync(owner, groupId, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(Map(item));
    }
    private static Task<IResult> Students(Guid groupId, [AsParameters] Request request, HttpContext context, IGroupQuery query, CancellationToken ct) =>
        People(groupId, request, context, query, false, ct);
    private static Task<IResult> Candidates(Guid groupId, [AsParameters] Request request, HttpContext context, IGroupQuery query, CancellationToken ct) =>
        People(groupId, request, context, query, true, ct);
    private static async Task<IResult> People(Guid id, Request request, HttpContext context, IGroupQuery query, bool candidates, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var page = await query.GetStudentsAsync(owner, id, Criteria(request), candidates, ct);
        return page is null ? TypedResults.NotFound() : TypedResults.Ok(new PagedResponse<StudentResponse>(
            page.Items.Select(student => new StudentResponse(student.Id, student.FirstName, student.LastName, student.SchoolGrade,
                student.Recommended, Convert.ToBase64String(student.RowVersion))).ToList(), page.Page, page.PageSize, page.TotalCount));
    }
    private static async Task<IResult> Sessions(Guid groupId, [AsParameters] Request request, HttpContext context, IGroupQuery query, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var page = await query.GetSessionsAsync(owner, groupId, Criteria(request), ct);
        return page is null ? TypedResults.NotFound() : TypedResults.Ok(new PagedResponse<SessionResponse>(
            page.Items.Select(session => new SessionResponse(session.Id, session.StartsAtUtc, session.EndsAtUtc, session.TimeZoneId,
                session.Location, session.Online, session.Status)).ToList(), page.Page, page.PageSize, page.TotalCount));
    }
    private static async Task<IResult> Membership(Guid groupId, Guid studentId, MembershipRequest request, HttpContext context,
        IGroupMembershipService service, CancellationToken ct)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var result = await service.ChangeAsync(owner, groupId, studentId,
            new(request.Join, Convert.FromBase64String(request.GroupRowVersion), Convert.FromBase64String(request.StudentRowVersion)), ct);
        return result switch
        {
            GroupMembershipResult.Saved => TypedResults.NoContent(),
            GroupMembershipResult.NotFound => Problem(404, "group_or_student_not_found", "The group or student was not found."),
            GroupMembershipResult.Conflict => Problem(409, "concurrency_conflict", "Data changed while saving."),
            GroupMembershipResult.Full => Problem(409, "group_capacity_reached", "The group is full."),
            GroupMembershipResult.Unavailable => Problem(409, "group_unavailable", "The group is unavailable."),
            GroupMembershipResult.MembershipChanged => Problem(409, "membership_changed", "The membership changed."),
            _ => throw new InvalidOperationException("Unsupported membership result."),
        };
    }
    private static GroupCriteria Criteria(Request request) => new(request.Page ?? 1, request.PageSize ?? 25, request.Search, request.ProgramId, request.Status);
    private static PagedResponse<GroupResponse> Map(GroupPage<GroupItem> page) => new(page.Items.Select(Map).ToList(), page.Page, page.PageSize, page.TotalCount);
    private static GroupResponse Map(GroupItem item) => new(item.Id, item.Name, item.ProgramId, item.ProgramName, item.SchoolGradeId,
        item.SchoolGrade, Status(item.Status), item.Capacity, item.MemberCount, Convert.ToBase64String(item.RowVersion), item.Slots.Select(slot =>
            new SlotResponse(slot.DayOfWeek, slot.Start, slot.End, slot.TimeZoneId, slot.Location, slot.Online)).ToList());
    private static string Status(int status) => status switch { 1 => "active", 2 => "on_hold", 3 => "inactive", _ => throw new InvalidOperationException() };
    private static IResult Problem(int status, string code, string title) => Results.Problem(statusCode: status, title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static RouteHandlerBuilder RequireGroupCsrf(this RouteHandlerBuilder builder) => builder.AddEndpointFilter(async (context, next) =>
    {
        try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException) { return Problem(400, "invalid_csrf_token", "The anti-forgery token is missing or invalid."); }
        return await next(context);
    });

    public sealed record Request : IValidatableObject
    {
        [Range(1, int.MaxValue)] public int? Page { get; init; }
        [Range(1, 100)] public int? PageSize { get; init; }
        [StringLength(100)] public string? Search { get; init; }
        public Guid? ProgramId { get; init; }
        [Range(1, 3)] public int? Status { get; init; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        { if (ProgramId == Guid.Empty) yield return new("Program identifier must not be empty.", [nameof(ProgramId)]); }
    }
    public sealed record MembershipRequest(bool Join, string GroupRowVersion, string StudentRowVersion) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Valid(GroupRowVersion)) yield return new("Group row version is invalid.", [nameof(GroupRowVersion)]);
            if (!Valid(StudentRowVersion)) yield return new("Student row version is invalid.", [nameof(StudentRowVersion)]);
        }
        private static bool Valid(string? value) { try { return value is not null && Convert.FromBase64String(value).Length > 0; } catch (FormatException) { return false; } }
    }
    public sealed record GroupResponse(Guid Id, string Name, Guid ProgramId, string ProgramName, Guid SchoolGradeId, string SchoolGrade,
        string Status, int Capacity, int MemberCount, string RowVersion, IReadOnlyList<SlotResponse> Slots);
    public sealed record SlotResponse(int DayOfWeek, TimeOnly Start, TimeOnly End, string TimeZoneId, string? Location, bool Online);
    public sealed record OverviewResponse(long TotalGroups, long ActiveGroups, long Students, long AvailableSeats, long SessionsThisWeek, DateOnly WeekStartsOn);
    public sealed record StudentResponse(Guid Id, string FirstName, string LastName, string SchoolGrade, bool Recommended, string RowVersion);
    public sealed record SessionResponse(Guid Id, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string TimeZoneId, string? Location, bool Online, int Status);
}
