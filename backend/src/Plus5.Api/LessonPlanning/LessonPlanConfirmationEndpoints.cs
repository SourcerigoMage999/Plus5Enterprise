using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.LessonPlanning;

namespace Plus5.Api.LessonPlanning;

public static class LessonPlanConfirmationEndpoints
{
    public static IEndpointRouteBuilder MapLessonPlanConfirmation(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapVersionOneApi()
            .MapGroup("/lesson-plans/{lessonPlanId:guid}/confirmation")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/", GetAsync);
        group.MapPost("/confirm", ConfirmAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPost("/edit", EditAsync).AddEndpointFilter(ValidateCsrfAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid lessonPlanId, HttpContext context,
        ILessonPlanConfirmationService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.GetAsync(teacherId, lessonPlanId, cancellationToken));
    }

    private static async Task<IResult> ConfirmAsync(Guid lessonPlanId, [FromBody] ConfirmRequest request,
        HttpContext context, ILessonPlanConfirmationService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.ConfirmAsync(teacherId, lessonPlanId,
            request.PlanRowVersion, request.VersionRowVersion, cancellationToken));
    }

    private static async Task<IResult> EditAsync(Guid lessonPlanId, [FromBody] EditRequest request,
        HttpContext context, ILessonPlanConfirmationService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.CreateEditableDraftAsync(teacherId, lessonPlanId,
            request.PlanRowVersion, cancellationToken));
    }

    private static IResult ToResult(LessonPlanConfirmationResult result) => result.Failure switch
    {
        LessonPlanConfirmationFailure.None => TypedResults.Ok(result.View),
        LessonPlanConfirmationFailure.Invalid => Problem(400, "lesson_confirmation_invalid_request"),
        LessonPlanConfirmationFailure.NotFound => Problem(404, "lesson_confirmation_not_found"),
        LessonPlanConfirmationFailure.NotDraft => Problem(409, "lesson_confirmation_not_draft"),
        LessonPlanConfirmationFailure.ValidationFailed => result.View is null
            ? Problem(422, "lesson_confirmation_validation_failed")
            : Results.Json(result.View, statusCode: 422),
        _ => Problem(409, "concurrency_conflict"),
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
        title: "Lesson plan confirmation request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record ConfirmRequest
    {
        [Required, StringLength(24)] public string PlanRowVersion { get; init; } = string.Empty;
        [Required, StringLength(24)] public string VersionRowVersion { get; init; } = string.Empty;
    }

    public sealed record EditRequest
    {
        [Required, StringLength(24)] public string PlanRowVersion { get; init; } = string.Empty;
    }
}
