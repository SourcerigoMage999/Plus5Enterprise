using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.LessonSessions;

namespace Plus5.Api.LessonSessions;

public static class LessonSessionEndpoints
{
    public static IEndpointRouteBuilder MapLessonSessions(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapVersionOneApi()
            .MapPost("/lesson-plans/{lessonPlanId:guid}/lesson-sessions/start", StartAsync)
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy)
            .AddEndpointFilter(ValidateCsrfAsync);

        var group = endpoints.MapVersionOneApi().MapGroup("/lesson-sessions")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/active", ActiveAsync);
        group.MapGet("/{lessonSessionId:guid}", GetAsync);
        group.MapPost("/{lessonSessionId:guid}/mutations", MutateAsync)
            .AddEndpointFilter(ValidateCsrfAsync);
        return endpoints;
    }

    private static async Task<IResult> StartAsync(Guid lessonPlanId, [FromBody] StartRequest request,
        HttpContext context, ILessonSessionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        var result = await service.StartAsync(teacherId, lessonPlanId,
            request.LessonPlanVersionId, request.CalendarSessionId,
            request.StartOperationId, cancellationToken);
        if (result.Failure != LessonSessionFailure.None) return ToFailure(result.Failure);
        return Results.Json(result.View, statusCode: result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }

    private static async Task<IResult> ActiveAsync(HttpContext context,
        ILessonSessionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return TypedResults.Ok(await service.GetActiveAsync(teacherId, cancellationToken));
    }

    private static async Task<IResult> GetAsync(Guid lessonSessionId, HttpContext context,
        ILessonSessionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(teacherId, lessonSessionId, cancellationToken);
        return result.Failure == LessonSessionFailure.None
            ? TypedResults.Ok(result.View)
            : ToFailure(result.Failure);
    }

    private static async Task<IResult> MutateAsync(Guid lessonSessionId,
        [FromBody] MutationRequest request, HttpContext context,
        ILessonSessionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        if (!string.Equals(request.Type, "ReorderTimeline", StringComparison.Ordinal)
            || request.ActivityIds is null || request.ActivityIds.Count is < 1 or > 200)
            return ToFailure(LessonSessionFailure.InvalidMutation);
        var result = await service.ReorderTimelineAsync(teacherId, lessonSessionId,
            request.OperationId, request.ExpectedRevision, request.ActivityIds,
            cancellationToken);
        return result.Failure == LessonSessionFailure.None
            ? TypedResults.Ok(result.View)
            : ToFailure(result.Failure);
    }

    private static IResult ToFailure(LessonSessionFailure failure) => failure switch
    {
        LessonSessionFailure.Invalid => Problem(400, "lesson_session_invalid_request"),
        LessonSessionFailure.NotFound => Problem(404, "lesson_session_not_found"),
        LessonSessionFailure.NotReady => Problem(409, "lesson_session_plan_not_ready"),
        LessonSessionFailure.CalendarUnavailable => Problem(409, "lesson_session_calendar_unavailable"),
        LessonSessionFailure.ActiveConflict => Problem(409, "lesson_session_active_conflict"),
        LessonSessionFailure.OperationMismatch => Problem(409, "lesson_session_operation_mismatch"),
        LessonSessionFailure.ConcurrencyConflict => Problem(409, "lesson_session_concurrency_conflict"),
        LessonSessionFailure.InvalidMutation => Problem(422, "lesson_session_invalid_mutation"),
        _ => Problem(409, "lesson_session_conflict"),
    };

    private static async ValueTask<object?> ValidateCsrfAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
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

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Lesson Session request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record StartRequest
    {
        [Required] public Guid LessonPlanVersionId { get; init; }
        public Guid? CalendarSessionId { get; init; }
        [Required] public Guid StartOperationId { get; init; }
    }

    public sealed record MutationRequest
    {
        [Required, StringLength(40)] public string Type { get; init; } = string.Empty;
        [Required] public Guid OperationId { get; init; }
        [Range(1, long.MaxValue)] public long ExpectedRevision { get; init; }
        public IReadOnlyList<Guid>? ActivityIds { get; init; }
    }
}
