using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.LessonPlanning;

namespace Plus5.Api.LessonPlanning;

public static class LessonStructureEndpoints
{
    public static IEndpointRouteBuilder MapLessonStructure(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapVersionOneApi().MapGroup("/lesson-plans/{lessonPlanId:guid}/structure")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/", GetAsync);
        group.MapPut("/target-duration", ChangeDurationAsync).AddEndpointFilter(ValidateCsrfAsync);
        group.MapPost("/apply", ApplyAsync).AddEndpointFilter(ValidateCsrfAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid lessonPlanId, HttpContext context,
        ILessonStructureService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.GetAsync(teacherId, lessonPlanId, cancellationToken));
    }

    private static async Task<IResult> ChangeDurationAsync(Guid lessonPlanId, DurationRequest request,
        HttpContext context, ILessonStructureService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.ChangeTargetDurationAsync(teacherId, lessonPlanId,
            request.TargetDurationMinutes, request.RowVersion, cancellationToken));
    }

    private static async Task<IResult> ApplyAsync(Guid lessonPlanId, ApplyRequest request,
        HttpContext context, ILessonStructureService service, CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return ToResult(await service.ApplyBaselineAsync(teacherId, lessonPlanId,
            request.RowVersion, cancellationToken));
    }

    private static IResult ToResult(LessonStructureResult result) => result.Failure switch
    {
        LessonStructureFailure.None => TypedResults.Ok(result.View),
        LessonStructureFailure.Invalid => Problem(400, "lesson_structure_invalid_request"),
        LessonStructureFailure.NotFound => Problem(404, "lesson_structure_not_found"),
        LessonStructureFailure.NotDraft => Problem(409, "lesson_structure_not_draft"),
        LessonStructureFailure.AlreadyApplied => Problem(409, "lesson_structure_already_applied"),
        LessonStructureFailure.KnowledgeBlockUnavailable => Problem(409, "lesson_structure_catalog_unavailable"),
        _ => Problem(409, "concurrency_conflict"),
    };

    private static async ValueTask<object?> ValidateCsrfAsync(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>()
                .ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException) { return Problem(400, "invalid_csrf_token"); }
        return await next(context);
    }

    private static IResult Problem(int status, string code) => Results.Problem(statusCode: status,
        title: "Lesson structure request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record DurationRequest
    {
        [Range(5, 480)] public int TargetDurationMinutes { get; init; }
        [Required, StringLength(24)] public string RowVersion { get; init; } = string.Empty;
    }
    public sealed record ApplyRequest
    {
        [Required, StringLength(24)] public string RowVersion { get; init; } = string.Empty;
    }
}
