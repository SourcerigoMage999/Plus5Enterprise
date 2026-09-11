using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Contracts;
using Plus5.Api.Identity;
using Plus5.Application.Groups;

namespace Plus5.Api.Groups;

public static class GroupCreationEndpoints
{
    public static void MapGroupCreationReads(this RouteGroupBuilder group)
    {
        group.MapGet("/create-candidates", Candidates);
        group.MapGet("/create-locations", Locations);
        group.MapPost("/", Create).AddEndpointFilter(async (context, next) =>
        {
            try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
            catch (AntiforgeryValidationException) { return Problem(400, "invalid_csrf_token"); }
            return await next(context);
        });
    }

    private static async Task<IResult> Locations(int? page, string? search, HttpContext context, IGroupCreationQuery query, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        if (page is < 1 || search?.Length > 100) return Problem(400, "invalid_request");
        var result = await query.GetLocationsAsync(owner, page ?? 1, search, cancellationToken);
        return TypedResults.Ok(new PagedResponse<LocationResponse>(result.Items.Select(l => new LocationResponse(l.Id, l.Name)).ToList(), result.Page, result.PageSize, result.TotalCount));
    }

    private static async Task<IResult> Create(CreateRequest request, HttpContext context, IGroupCreationService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        if (request.Members is null || request.Slots is null) return Problem(400, "invalid_request");
        var members = new List<InitialGroupMember>();
        foreach (var member in request.Members)
        {
            if (member is null || member.RowVersion is null) return Problem(400, "invalid_request");
            try { members.Add(new(member.StudentId, Convert.FromBase64String(member.RowVersion))); }
            catch (FormatException) { return Problem(400, "invalid_request"); }
        }
        if (request.Slots.Any(s => s is null)) return Problem(400, "invalid_request");
        var result = await service.CreateAsync(owner, new(request.Name, request.ProgramId, request.SchoolGradeId, request.Capacity,
            request.Description, members, request.Slots.Select(s => new GroupScheduleSlot(s.DayOfWeek, s.Start, s.End)).ToList(),
            request.StartsOn, request.EndsOn, request.LocationId), cancellationToken);
        return result.Failure switch
        {
            GroupCreateFailure.None => TypedResults.Created($"/api/v1/groups/{result.Id}", new CreatedResponse(result.Id!.Value, result.SessionCount)),
            GroupCreateFailure.Invalid => Problem(400, "invalid_request"),
            GroupCreateFailure.NotFound => Problem(404, "group_create_reference_not_found"),
            GroupCreateFailure.DuplicateName => Problem(409, "group_name_exists"),
            GroupCreateFailure.MembershipChanged => Problem(409, "membership_changed"),
            GroupCreateFailure.ScheduleConflict => Problem(409, "schedule_conflict"),
            GroupCreateFailure.InvalidLocalTime => Problem(400, "invalid_local_time"),
            _ => Problem(409, "concurrency_conflict"),
        };
    }

    private static IResult Problem(int status, string code) => Results.Problem(statusCode: status, title: "Group creation request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static async Task<IResult> Candidates([AsParameters] Request request, HttpContext context,
        IGroupCreationQuery query, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var result = await query.GetCandidatesAsync(owner,
            new(request.Page ?? 1, request.PageSize ?? 25, request.ProgramId, request.SchoolGradeId, request.Search), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(new PagedResponse<CandidateResponse>(
            result.Items.Select(item => new CandidateResponse(item.Id, item.FirstName, item.LastName,
                item.SchoolGrade, item.ProgramName, item.Recommended, Convert.ToBase64String(item.RowVersion))).ToList(), result.Page, result.PageSize, result.TotalCount));
    }

    public sealed record Request : IValidatableObject
    {
        [Range(1, int.MaxValue)] public int? Page { get; init; }
        [Range(1, 100)] public int? PageSize { get; init; }
        [StringLength(100)] public string? Search { get; init; }
        public Guid ProgramId { get; init; }
        public Guid SchoolGradeId { get; init; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ProgramId == Guid.Empty) yield return new("Program is required.", [nameof(ProgramId)]);
            if (SchoolGradeId == Guid.Empty) yield return new("School grade is required.", [nameof(SchoolGradeId)]);
        }
    }

    public sealed record CandidateResponse(Guid Id, string FirstName, string LastName,
        string SchoolGrade, string? ProgramName, bool Recommended, string RowVersion);
    public sealed record LocationResponse(Guid Id, string Name);
    public sealed record MemberRequest(Guid StudentId, string RowVersion);
    public sealed record SlotRequest(int DayOfWeek, TimeOnly Start, TimeOnly End);
    public sealed record CreatedResponse(Guid Id, int SessionCount);
    public sealed record CreateRequest
    {
        [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
        public Guid ProgramId { get; init; }
        public Guid SchoolGradeId { get; init; }
        [Range(1, int.MaxValue)] public int Capacity { get; init; }
        [StringLength(1000)] public string? Description { get; init; }
        [MaxLength(100)] public MemberRequest[] Members { get; init; } = [];
        [MaxLength(14)] public SlotRequest[] Slots { get; init; } = [];
        public DateOnly? StartsOn { get; init; }
        public DateOnly? EndsOn { get; init; }
        public Guid? LocationId { get; init; }
    }
}
