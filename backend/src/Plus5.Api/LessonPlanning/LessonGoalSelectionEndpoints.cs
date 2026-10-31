using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Contracts;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.LessonPlanning;

namespace Plus5.Api.LessonPlanning;

public static class LessonGoalSelectionEndpoints
{
    public static IEndpointRouteBuilder MapLessonGoalSelection(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapVersionOneApi().MapGroup("/lesson-plans")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/participants", ParticipantsAsync);
        group.MapGet("/sessions", SessionsAsync);
        group.MapGet("/knowledge-components", KnowledgeComponentsAsync);
        group.MapPost("/", CreateAsync).AddEndpointFilter(ValidateCsrfAsync);
        return endpoints;
    }

    private static async ValueTask<object?> ValidateCsrfAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
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
    }

    private static async Task<IResult> ParticipantsAsync(
        [AsParameters] ParticipantRequest request,
        HttpContext context,
        ILessonGoalSelectionQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        if (!TryKind(request.Type, out var kind)) return Problem(400, "invalid_request");
        var page = await query.GetParticipantsAsync(teacherId, kind, request.Page ?? 1,
            request.PageSize ?? 50, request.Search, cancellationToken);
        return TypedResults.Ok(new PagedResponse<ParticipantResponse>(page.Items.Select(item =>
            new ParticipantResponse(item.Id, item.Name, item.SchoolGrade, item.ProgramName,
                item.MemberCount)).ToList(), page.Page, page.PageSize, page.TotalCount));
    }

    private static async Task<IResult> SessionsAsync(
        string type,
        Guid participantId,
        HttpContext context,
        ILessonGoalSelectionQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        if (!TryKind(type, out var kind) || participantId == Guid.Empty) return Problem(400, "invalid_request");
        var sessions = await query.GetSessionsAsync(teacherId, kind, participantId, cancellationToken);
        return TypedResults.Ok(sessions.Select(item => new SessionResponse(item.Id, item.StartsAtUtc,
            item.EndsAtUtc, item.TimeZoneId, item.Title)).ToList());
    }

    private static async Task<IResult> KnowledgeComponentsAsync(
        [AsParameters] KnowledgeRequest request,
        HttpContext context,
        ILessonGoalSelectionQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out _, out _)) return TypedResults.Unauthorized();
        var page = await query.GetKnowledgeComponentsAsync(request.Page ?? 1, request.PageSize ?? 50,
            request.Search, cancellationToken);
        return TypedResults.Ok(new PagedResponse<KnowledgeResponse>(page.Items.Select(item =>
            new KnowledgeResponse(item.Id, item.Name, item.AreaName, item.ModelCode,
                item.ModelVersion)).ToList(), page.Page, page.PageSize, page.TotalCount));
    }

    private static async Task<IResult> CreateAsync(
        CreateRequest request,
        HttpContext context,
        ILessonGoalSelectionService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        if (!TryKind(request.ParticipantType, out var kind) || request.KnowledgeComponentIds is null)
            return Problem(400, "lesson_goal_invalid_request");
        var result = await service.CreateAsync(teacherId, new(kind, request.ParticipantId,
            request.SessionId, request.Goal, request.KnowledgeComponentIds), cancellationToken);
        return result.Failure switch
        {
            CreateLessonGoalFailure.None => TypedResults.Created($"/api/v1/lesson-plans/{result.LessonPlanId:D}",
                new CreatedResponse(result.LessonPlanId!.Value, result.LessonPlanVersionId!.Value)),
            CreateLessonGoalFailure.Invalid => Problem(400, "lesson_goal_invalid_request"),
            CreateLessonGoalFailure.ParticipantUnavailable => Problem(404, "lesson_goal_participant_unavailable"),
            CreateLessonGoalFailure.SessionMismatch => Problem(409, "lesson_goal_session_mismatch"),
            CreateLessonGoalFailure.KnowledgeUnavailable => Problem(409, "lesson_goal_knowledge_unavailable"),
            CreateLessonGoalFailure.ReferenceNotFound => Problem(404, "lesson_goal_reference_not_found"),
            _ => Problem(409, "concurrency_conflict"),
        };
    }

    private static bool TryKind(string value, out LessonParticipantKind kind)
    {
        kind = value.ToLowerInvariant() switch
        {
            "student" => LessonParticipantKind.Student,
            "group" => LessonParticipantKind.Group,
            _ => default,
        };
        return kind != default;
    }

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Lesson goal selection request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record ParticipantRequest
    {
        [Required] public string Type { get; init; } = string.Empty;
        [Range(1, int.MaxValue)] public int? Page { get; init; }
        [Range(1, 100)] public int? PageSize { get; init; }
        [StringLength(100)] public string? Search { get; init; }
    }

    public sealed record KnowledgeRequest
    {
        [Range(1, int.MaxValue)] public int? Page { get; init; }
        [Range(1, 100)] public int? PageSize { get; init; }
        [StringLength(100)] public string? Search { get; init; }
    }

    public sealed record CreateRequest
    {
        [Required] public string ParticipantType { get; init; } = string.Empty;
        public Guid ParticipantId { get; init; }
        public Guid? SessionId { get; init; }
        [Required, StringLength(300)] public string Goal { get; init; } = string.Empty;
        [MinLength(1), MaxLength(50)] public Guid[] KnowledgeComponentIds { get; init; } = [];
    }

    public sealed record ParticipantResponse(Guid Id, string Name, string SchoolGrade,
        string? ProgramName, int? MemberCount);
    public sealed record SessionResponse(Guid Id, DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc, string TimeZoneId, string? Title);
    public sealed record KnowledgeResponse(Guid Id, string Name, string AreaName,
        string ModelCode, string ModelVersion);
    public sealed record CreatedResponse(Guid LessonPlanId, Guid LessonPlanVersionId);
}
