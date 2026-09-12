using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Identity;
using Plus5.Application.Groups;

namespace Plus5.Api.Groups;

public static class GroupEditingEndpoints
{
    public static void MapGroupEditing(this RouteGroupBuilder group)
    {
        group.MapGet("/{groupId:guid}/edit", Read);
        group.MapPut("/{groupId:guid}", Update).AddEndpointFilter(async (context, next) =>
        {
            try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
            catch (AntiforgeryValidationException) { return Problem(400, "invalid_csrf_token"); }
            return await next(context);
        });
    }

    private static async Task<IResult> Read(Guid groupId, HttpContext context, IGroupEditingQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        var item = await query.GetAsync(owner, groupId, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(new EditResponse(item.Id, item.Name,
            item.Description, item.ProgramId, item.ProgramName, item.SchoolGradeId, item.SchoolGrade,
            item.Status, item.Capacity, item.MemberCount, Convert.ToBase64String(item.RowVersion),
            item.Slots.Select(slot => new SlotResponse(slot.SeriesId, slot.DayOfWeek, slot.Start, slot.End,
                slot.StartsOn, slot.EndsOn, slot.LocationId, slot.LocationName)).ToList()));
    }

    private static async Task<IResult> Update(Guid groupId, UpdateRequest request, HttpContext context,
        IGroupEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var owner, out _)) return TypedResults.Unauthorized();
        if (request.Slots is null || !TryVersion(request.RowVersion, out var rowVersion))
            return Problem(400, "invalid_request");
        var result = await service.UpdateAsync(owner, groupId, new GroupEditCommand(request.Name,
            request.Description, request.ProgramId, request.SchoolGradeId, request.Status, request.Capacity,
            rowVersion, request.Slots.Select(slot => new GroupScheduleSlot(slot.DayOfWeek, slot.Start, slot.End)).ToList(),
            request.ScheduleStartsOn, request.ScheduleEndsOn, request.LocationId), cancellationToken);
        return result.Failure switch
        {
            GroupEditFailure.None => TypedResults.Ok(new UpdatedResponse(groupId, result.SessionCount)),
            GroupEditFailure.Invalid => Problem(400, "invalid_request"),
            GroupEditFailure.InvalidLocalTime => Problem(400, "invalid_local_time"),
            GroupEditFailure.NotFound => Problem(404, "group_edit_reference_not_found"),
            GroupEditFailure.DuplicateName => Problem(409, "group_name_exists"),
            GroupEditFailure.ProgramHasActiveMembers => Problem(409, "group_program_has_active_members"),
            GroupEditFailure.CapacityBelowMembers => Problem(409, "group_capacity_below_members"),
            GroupEditFailure.ScheduleConflict => Problem(409, "schedule_conflict"),
            _ => Problem(409, "concurrency_conflict"),
        };
    }

    private static bool TryVersion(string? value, out byte[] result)
    {
        try { result = Convert.FromBase64String(value ?? string.Empty); return result.Length == 8; }
        catch (FormatException) { result = []; return false; }
    }

    private static IResult Problem(int status, string code) => Results.Problem(statusCode: status,
        title: "Group editing request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record SlotRequest(int DayOfWeek, TimeOnly Start, TimeOnly End);
    public sealed record SlotResponse(Guid SeriesId, int DayOfWeek, TimeOnly Start, TimeOnly End,
        DateOnly StartsOn, DateOnly? EndsOn, Guid? LocationId, string? LocationName);
    public sealed record EditResponse(Guid Id, string Name, string? Description, Guid ProgramId,
        string ProgramName, Guid SchoolGradeId, string SchoolGrade, int Status, int Capacity,
        int MemberCount, string RowVersion, IReadOnlyList<SlotResponse> Slots);
    public sealed record UpdatedResponse(Guid Id, int SessionCount);

    public sealed record UpdateRequest
    {
        [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
        [StringLength(1000)] public string? Description { get; init; }
        public Guid ProgramId { get; init; }
        public Guid SchoolGradeId { get; init; }
        [Range(1, 3)] public int Status { get; init; }
        [Range(1, int.MaxValue)] public int Capacity { get; init; }
        [Required] public string RowVersion { get; init; } = string.Empty;
        [MaxLength(14)] public SlotRequest[] Slots { get; init; } = [];
        public DateOnly? ScheduleStartsOn { get; init; }
        public DateOnly? ScheduleEndsOn { get; init; }
        public Guid? LocationId { get; init; }
    }
}
