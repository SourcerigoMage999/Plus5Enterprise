using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Identity;
using Plus5.Application.Materials;

namespace Plus5.Api.Materials;

public static class MaterialSharingEndpoints
{
    public static IEndpointRouteBuilder MapMaterialSharing(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/materials")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapGet("/{materialId:guid}/sharing", GetAsync);
        group.MapPut("/{materialId:guid}/sharing", SaveAsync)
            .AddEndpointFilter(ValidateCsrfAsync)
            .RequireRateLimiting(IdentityServiceExtensions.MaterialSharingRateLimitPolicy);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid materialId,
        HttpContext context,
        IMaterialSharingQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var workspace = await query.GetAsync(
            teacherAccountId,
            materialId,
            cancellationToken);
        return workspace is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(workspace));
    }

    private static async Task<IResult> SaveAsync(
        Guid materialId,
        MaterialSharingRequest request,
        HttpContext context,
        IMaterialSharingService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (!TryMap(request, out var command))
        {
            return Problem(400, "material_share_invalid_request");
        }

        var result = await service.SaveAsync(
            teacherAccountId,
            materialId,
            command,
            cancellationToken);
        return result.Outcome switch
        {
            MaterialSharingOutcome.Success => TypedResults.Ok(new { saved = true }),
            MaterialSharingOutcome.InvalidInput => Problem(400, "material_share_invalid_request"),
            MaterialSharingOutcome.NotFound => Problem(404, "material_not_found"),
            MaterialSharingOutcome.InvalidRecipient => Problem(400, "material_share_invalid_recipient"),
            MaterialSharingOutcome.Conflict => Problem(409, "concurrency_conflict"),
            _ => Problem(500, "material_share_failed"),
        };
    }

    private static MaterialSharingResponse Map(MaterialSharingWorkspace workspace) => new(
        workspace.MaterialId,
        workspace.Title,
        workspace.RowVersion,
        workspace.Visibility == MaterialSharingVisibility.Shared ? "shared" : "private",
        workspace.Grants.Select(grant => new MaterialSharingGrantResponse(
            grant.TeacherAccountId,
            grant.Email,
            grant.Access == MaterialSharingAccess.Use ? "use" : "view")).ToList());

    private static bool TryMap(
        MaterialSharingRequest request,
        out MaterialSharingCommand command)
    {
        command = null!;
        if (!TryVisibility(request.Visibility, out var visibility)
            || request.Grants is null)
        {
            return false;
        }

        var grants = new List<MaterialSharingGrantCommand>(request.Grants.Count);
        foreach (var grant in request.Grants)
        {
            if (grant is null || !TryAccess(grant.Access, out var access))
            {
                return false;
            }

            grants.Add(new MaterialSharingGrantCommand(grant.RecipientEmail, access));
        }

        command = new MaterialSharingCommand(
            request.ExpectedRowVersion,
            visibility,
            grants);
        return true;
    }

    private static bool TryVisibility(
        string value,
        out MaterialSharingVisibility visibility)
    {
        visibility = value?.Trim().ToLowerInvariant() switch
        {
            "private" => MaterialSharingVisibility.Private,
            "shared" => MaterialSharingVisibility.Shared,
            _ => 0,
        };
        return visibility != 0;
    }

    private static bool TryAccess(string value, out MaterialSharingAccess access)
    {
        access = value?.Trim().ToLowerInvariant() switch
        {
            "view" => MaterialSharingAccess.View,
            "use" => MaterialSharingAccess.Use,
            _ => 0,
        };
        return access != 0;
    }

    private static async ValueTask<object?> ValidateCsrfAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await context.HttpContext.RequestServices
                .GetRequiredService<IAntiforgery>()
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
        title: "Material sharing request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    public sealed record MaterialSharingRequest(
        string ExpectedRowVersion,
        string Visibility,
        IReadOnlyList<MaterialSharingGrantRequest?>? Grants);

    public sealed record MaterialSharingGrantRequest(
        string RecipientEmail,
        string Access);

    public sealed record MaterialSharingResponse(
        Guid MaterialId,
        string Title,
        string RowVersion,
        string Visibility,
        IReadOnlyList<MaterialSharingGrantResponse> Grants);

    public sealed record MaterialSharingGrantResponse(
        Guid TeacherAccountId,
        string Email,
        string Access);
}
