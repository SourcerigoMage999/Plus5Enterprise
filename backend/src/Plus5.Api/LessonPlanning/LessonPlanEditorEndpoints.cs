using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.LessonPlanning;

namespace Plus5.Api.LessonPlanning;

public static class LessonPlanEditorEndpoints
{
    public static IEndpointRouteBuilder MapLessonPlanEditor(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapVersionOneApi()
            .MapGroup("/lesson-plans/{lessonPlanId:guid}/editor")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/", GetAsync);
        group.MapGet("/activity-candidates", GetActivityCandidatesAsync);
        group.MapPost("/activities", AddActivityAsync)
            .AddEndpointFilter(ValidateCsrfAsync);
        group.MapGet("/activities/{activityId:guid}/material-candidates", GetCandidatesAsync);
        group.MapPost("/activities/{activityId:guid}/materials/{materialVersionId:guid}", AttachAsync)
            .AddEndpointFilter(ValidateCsrfAsync);
        group.MapDelete("/activities/{activityId:guid}/materials/{materialVersionId:guid}", DetachAsync)
            .AddEndpointFilter(ValidateCsrfAsync);
        group.MapGet("/activities/{activityId:guid}", GetActivityAsync);
        group.MapPut("/activities/{activityId:guid}", UpdateActivityAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPost("/activities/{activityId:guid}/duplicate", DuplicateActivityAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapDelete("/activities/{activityId:guid}", DeleteActivityAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPut("/activities/reorder", ReorderActivitiesAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPut("/activities/{activityId:guid}/knowledge-components", UpdateActivityKnowledgeAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPost("/activities/{activityId:guid}/ai/suggestions", CreateAiSuggestionAsync)
            .AddEndpointFilter(ValidateCsrfAsync)
            .RequireRateLimiting(IdentityServiceExtensions.LessonBuilderAiRateLimitPolicy);
        group.MapGet("/activities/{activityId:guid}/ai/suggestions/{suggestionId:guid}", GetAiSuggestionAsync);
        group.MapPost("/activities/{activityId:guid}/ai/suggestions/{suggestionId:guid}/resolve", ResolveAiSuggestionAsync)
            .AddEndpointFilter(ValidateCsrfAsync);
        return endpoints;
    }

    private static async Task<IResult> GetActivityAsync(Guid lessonPlanId, Guid activityId, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityResult(await service.GetAsync(teacherId, lessonPlanId, activityId, cancellationToken));
    }

    private static async Task<IResult> UpdateActivityAsync(Guid lessonPlanId, Guid activityId,
        [FromBody] UpdateLessonActivityCommand request, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityResult(await service.UpdateAsync(teacherId, lessonPlanId, activityId, request, cancellationToken));
    }

    private static async Task<IResult> DuplicateActivityAsync(Guid lessonPlanId, Guid activityId,
        [FromBody] LessonActivityRowVersionCommand request, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityMutationResult(await service.DuplicateAsync(teacherId, lessonPlanId, activityId, request.RowVersion, cancellationToken));
    }

    private static async Task<IResult> DeleteActivityAsync(Guid lessonPlanId, Guid activityId,
        [FromBody] LessonActivityRowVersionCommand request, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityMutationResult(await service.DeleteAsync(teacherId, lessonPlanId, activityId, request.RowVersion, cancellationToken));
    }

    private static async Task<IResult> ReorderActivitiesAsync(Guid lessonPlanId,
        [FromBody] ReorderLessonActivitiesCommand request, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityMutationResult(await service.ReorderAsync(teacherId, lessonPlanId, request, cancellationToken));
    }

    private static async Task<IResult> UpdateActivityKnowledgeAsync(Guid lessonPlanId, Guid activityId,
        [FromBody] UpdateLessonActivityKnowledgeCommand request, HttpContext context,
        ILessonActivityEditingService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToActivityResult(await service.UpdateKnowledgeAsync(teacherId, lessonPlanId, activityId, request, cancellationToken));
    }

    private static async Task<IResult> CreateAiSuggestionAsync(Guid lessonPlanId, Guid activityId,
        [FromBody] CreateLessonActivityAiSuggestionCommand request, HttpContext context,
        ILessonActivityAiSuggestionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToAiResult(await service.CreateAsync(teacherId, lessonPlanId, activityId, request, cancellationToken));
    }

    private static async Task<IResult> GetAiSuggestionAsync(Guid lessonPlanId, Guid activityId, Guid suggestionId,
        HttpContext context, ILessonActivityAiSuggestionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(teacherId, lessonPlanId, activityId, suggestionId, cancellationToken);
        return result is null ? Problem(404, "lesson_activity_ai_suggestion_not_found") : TypedResults.Ok(result);
    }

    private static async Task<IResult> ResolveAiSuggestionAsync(Guid lessonPlanId, Guid activityId, Guid suggestionId,
        [FromBody] ResolveLessonActivityAiSuggestionCommand request, HttpContext context,
        ILessonActivityAiSuggestionService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToAiResult(await service.ResolveAsync(teacherId, lessonPlanId, activityId, suggestionId, request, cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid lessonPlanId,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        return ToResult(await service.GetAsync(teacherId, lessonPlanId, cancellationToken));
    }

    private static async Task<IResult> GetCandidatesAsync(
        Guid lessonPlanId,
        Guid activityId,
        string? search,
        int? page,
        int? pageSize,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        var result = await service.GetMaterialCandidatesAsync(teacherId, lessonPlanId,
            activityId, search, page ?? 1, pageSize ?? 20, cancellationToken);
        return result.Failure switch
        {
            LessonPlanEditorFailure.None => TypedResults.Ok(result.Page),
            LessonPlanEditorFailure.Invalid => Problem(400, "lesson_editor_invalid_request"),
            _ => Problem(404, "lesson_editor_not_found"),
        };
    }

    private static async Task<IResult> GetActivityCandidatesAsync(
        Guid lessonPlanId,
        string? search,
        int? maximumDurationMinutes,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        var result = await service.GetActivityCandidatesAsync(teacherId, lessonPlanId,
            search, maximumDurationMinutes, cancellationToken);
        return result.Failure switch
        {
            LessonPlanEditorFailure.None => TypedResults.Ok(result.Items),
            LessonPlanEditorFailure.Invalid => Problem(400, "lesson_activity_selection_invalid_request"),
            LessonPlanEditorFailure.NotDraft => Problem(409, "lesson_editor_not_draft"),
            _ => Problem(404, "lesson_editor_not_found"),
        };
    }

    private static async Task<IResult> AddActivityAsync(
        Guid lessonPlanId,
        [FromBody] AddActivityRequest request,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        return ToResult(await service.AddActivityAsync(teacherId, lessonPlanId,
            request.KnowledgeBlockVersionId, request.RowVersion, cancellationToken));
    }

    private static async Task<IResult> AttachAsync(
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        [FromBody] RowVersionRequest request,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        return ToResult(await service.AttachMaterialAsync(teacherId, lessonPlanId, activityId,
            materialVersionId, request.RowVersion, cancellationToken));
    }

    private static async Task<IResult> DetachAsync(
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        [FromBody] RowVersionRequest request,
        HttpContext context,
        ILessonPlanEditorService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
            return TypedResults.Unauthorized();
        return ToResult(await service.DetachMaterialAsync(teacherId, lessonPlanId, activityId,
            materialVersionId, request.RowVersion, cancellationToken));
    }

    private static IResult ToResult(LessonPlanEditorResult result) => result.Failure switch
    {
        LessonPlanEditorFailure.None => TypedResults.Ok(result.View),
        LessonPlanEditorFailure.Invalid => Problem(400, "lesson_editor_invalid_request"),
        LessonPlanEditorFailure.NotFound or LessonPlanEditorFailure.ActivityNotFound =>
            Problem(404, "lesson_editor_not_found"),
        LessonPlanEditorFailure.NotDraft => Problem(409, "lesson_editor_not_draft"),
        LessonPlanEditorFailure.MaterialUnavailable => Problem(409, "lesson_material_unavailable"),
        LessonPlanEditorFailure.AlreadyAttached => Problem(409, "lesson_material_already_attached"),
        LessonPlanEditorFailure.AttachmentNotFound => Problem(409, "lesson_material_attachment_not_found"),
        LessonPlanEditorFailure.KnowledgeBlockUnavailable =>
            Problem(409, "lesson_knowledge_block_unavailable"),
        _ => Problem(409, "concurrency_conflict"),
    };

    private static IResult ToActivityResult(LessonActivityDetailResult result) => result.Failure switch
    {
        LessonActivityEditingFailure.None => TypedResults.Ok(result.Detail),
        LessonActivityEditingFailure.Invalid => Problem(400, "lesson_activity_invalid_request"),
        LessonActivityEditingFailure.NotFound => Problem(404, "lesson_activity_not_found"),
        LessonActivityEditingFailure.NotDraft => Problem(409, "lesson_editor_not_draft"),
        _ => Problem(409, "concurrency_conflict"),
    };

    private static IResult ToActivityMutationResult(LessonActivityMutationResult result) => result.Failure switch
    {
        LessonActivityEditingFailure.None => TypedResults.Ok(result.View),
        LessonActivityEditingFailure.Invalid => Problem(400, "lesson_activity_invalid_request"),
        LessonActivityEditingFailure.NotFound => Problem(404, "lesson_activity_not_found"),
        LessonActivityEditingFailure.NotDraft => Problem(409, "lesson_editor_not_draft"),
        _ => Problem(409, "concurrency_conflict"),
    };

    private static IResult ToAiResult(LessonActivityAiSuggestionResult result) => result.Outcome switch
    {
        LessonActivityAiSuggestionOutcome.Success => TypedResults.Ok(result),
        LessonActivityAiSuggestionOutcome.Disabled => Problem(503, "lesson_builder_ai_disabled"),
        LessonActivityAiSuggestionOutcome.InvalidInput => Problem(400, "lesson_activity_ai_invalid_request"),
        LessonActivityAiSuggestionOutcome.InvalidOutput => Problem(422, "lesson_activity_ai_invalid_output"),
        LessonActivityAiSuggestionOutcome.NotFound => Problem(404, "lesson_activity_not_found"),
        LessonActivityAiSuggestionOutcome.Expired => Problem(409, "lesson_activity_ai_suggestion_expired"),
        LessonActivityAiSuggestionOutcome.DailyLimitReached => Problem(429, "lesson_activity_ai_daily_limit"),
        LessonActivityAiSuggestionOutcome.Unavailable => Problem(503, "lesson_activity_ai_unavailable"),
        _ => Problem(409, "concurrency_conflict"),
    };

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

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Lesson plan editor request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record RowVersionRequest
    {
        [Required, StringLength(24)]
        public string RowVersion { get; init; } = string.Empty;
    }

    public sealed record AddActivityRequest
    {
        public Guid KnowledgeBlockVersionId { get; init; }

        [Required, StringLength(24)]
        public string RowVersion { get; init; } = string.Empty;
    }
}
