using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Identity;
using Plus5.Application.Materials;

namespace Plus5.Api.Materials;

public static class MaterialEditingEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEditing(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/materials")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/{materialId:guid}/edit", GetAsync);
        group.MapGet("/{materialId:guid}/versions/{versionId:guid}", GetVersionAsync);
        RequireCsrf(group.MapPut("/{materialId:guid}/draft", SaveDraftAsync));
        RequireCsrf(group.MapPost("/{materialId:guid}/draft/publish", PublishAsync));
        RequireCsrf(group.MapPost("/{materialId:guid}/versions/{versionId:guid}/restore", RestoreAsync));
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid materialId, HttpContext context, IMaterialEditingQuery query, CancellationToken token)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        var result = await query.GetAsync(teacherId, materialId, token);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> GetVersionAsync(Guid materialId, Guid versionId, HttpContext context, IMaterialEditingQuery query, CancellationToken token)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        var result = await query.GetVersionAsync(teacherId, materialId, versionId, token);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> SaveDraftAsync(Guid materialId, MaterialEditCommand request, HttpContext context, IMaterialEditingService service, CancellationToken token)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return Map(await service.SaveDraftAsync(teacherId, materialId, request, token));
    }

    private static async Task<IResult> PublishAsync(Guid materialId, MaterialPublishCommand request, HttpContext context, IMaterialEditingService service, CancellationToken token)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return Map(await service.PublishDraftAsync(teacherId, materialId, request, token));
    }

    private static async Task<IResult> RestoreAsync(Guid materialId, Guid versionId, MaterialRestoreCommand request, HttpContext context, IMaterialEditingService service, CancellationToken token)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _)) return TypedResults.Unauthorized();
        return Map(await service.RestoreAsync(teacherId, materialId, versionId, request, token));
    }

    private static IResult Map(MaterialEditResult result) => result.Outcome switch
    {
        MaterialEditOutcome.Success => TypedResults.Ok(new { versionId = result.VersionId }),
        MaterialEditOutcome.InvalidInput => Problem(400, "material_edit_invalid_request"),
        MaterialEditOutcome.NotFound => Problem(404, "material_not_found"),
        MaterialEditOutcome.ReferenceNotFound => Problem(404, "material_edit_reference_not_found"),
        MaterialEditOutcome.DraftAlreadyExists => Problem(409, "material_draft_exists"),
        MaterialEditOutcome.Conflict => Problem(409, "concurrency_conflict"),
        MaterialEditOutcome.StorageFailure => Problem(503, "material_storage_unavailable"),
        _ => Problem(500, "material_edit_failed"),
    };

    private static void RequireCsrf(RouteHandlerBuilder builder) => builder.AddEndpointFilter(async (context, next) =>
    {
        try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException) { return Problem(400, "invalid_csrf_token"); }
        return await next(context);
    });

    private static IResult Problem(int status, string code) => Results.Problem(statusCode: status,
        title: "Material editing request could not be completed.", extensions: new Dictionary<string, object?> { ["code"] = code });
}
