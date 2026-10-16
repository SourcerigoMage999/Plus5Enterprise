using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Plus5.Api.Identity;
using Plus5.Application.Materials;

namespace Plus5.Api.Materials;

public static class PresentationEditorEndpoints
{
    private const long CommandBodyLimit = 16 * 1024;
    private const long ImageBodyLimit = 11 * 1024 * 1024;

    public static IEndpointRouteBuilder MapPresentationEditor(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/materials/{materialId:guid}/presentation")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("", GetAsync);
        ConfigureWrite(group.MapPost("", InitializeAsync));
        ConfigureWrite(group.MapPost("/slides", AddAsync));
        ConfigureWrite(group.MapDelete("/slides/{slideId:guid}", DeleteAsync));
        ConfigureWrite(group.MapPost("/slides/{slideId:guid}/duplicate", DuplicateAsync));
        ConfigureWrite(group.MapPut("/slides/order", ReorderAsync));
        ConfigureWrite(group.MapPost("/slides/{slideId:guid}/elements", CreateElementAsync));
        ConfigureImageWrite(group.MapPost("/slides/{slideId:guid}/images", UploadImageAsync));
        ConfigureWrite(group.MapPut(
            "/slides/{slideId:guid}/elements/{elementId:guid}",
            UpdateElementAsync));
        ConfigureWrite(group.MapDelete(
            "/slides/{slideId:guid}/elements/{elementId:guid}",
            DeleteElementAsync));
        group.MapGet("/assets/{assetId:guid}", OpenAssetAsync);
        return endpoints;
    }

    private static async Task<IResult> OpenAssetAsync(
        Guid materialId,
        Guid assetId,
        HttpContext context,
        IPresentationElementService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var asset = await service.OpenAssetAsync(
            teacherId,
            materialId,
            assetId,
            cancellationToken);
        return asset is null
            ? TypedResults.NotFound()
            : Results.Stream(
                asset.Content,
                asset.MediaType,
                enableRangeProcessing: true);
    }

    private static async Task<IResult> GetAsync(
        Guid materialId,
        HttpContext context,
        IPresentationEditorQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await query.GetAsync(
            teacherId,
            materialId,
            cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> InitializeAsync(
        Guid materialId,
        InitializePresentationCommand request,
        HttpContext context,
        IPresentationSlideService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.InitializeAsync(
            teacherId,
            materialId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> AddAsync(
        Guid materialId,
        AddPresentationSlideCommand request,
        HttpContext context,
        IPresentationSlideService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.AddAsync(
            teacherId,
            materialId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> DeleteAsync(
        Guid materialId,
        Guid slideId,
        [FromBody] PresentationConcurrencyCommand request,
        HttpContext context,
        IPresentationSlideService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.DeleteAsync(
            teacherId,
            materialId,
            slideId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> DuplicateAsync(
        Guid materialId,
        Guid slideId,
        PresentationConcurrencyCommand request,
        HttpContext context,
        IPresentationSlideService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.DuplicateAsync(
            teacherId,
            materialId,
            slideId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(
        Guid materialId,
        ReorderPresentationSlidesCommand request,
        HttpContext context,
        IPresentationSlideService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.ReorderAsync(
            teacherId,
            materialId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> CreateElementAsync(
        Guid materialId,
        Guid slideId,
        CreatePresentationElementCommand request,
        HttpContext context,
        IPresentationElementService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.CreateAsync(
            teacherId,
            materialId,
            slideId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> UploadImageAsync(
        Guid materialId,
        Guid slideId,
        HttpContext context,
        IPresentationElementService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (!context.Request.HasFormContentType)
        {
            return Problem(400, "presentation_invalid_asset");
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.Count == 1 ? form.Files[0] : null;
        if (file is null
            || !decimal.TryParse(
                form["x"],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var x)
            || !decimal.TryParse(
                form["y"],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var y)
            || !decimal.TryParse(
                form["width"],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var width)
            || !decimal.TryParse(
                form["height"],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var height))
        {
            return Problem(400, "presentation_invalid_asset");
        }

        return Map(await service.UploadImageAsync(
            teacherId,
            materialId,
            slideId,
            new UploadPresentationImageCommand(
                form["expectedDocumentRowVersion"].ToString(),
                Path.GetFileName(file.FileName),
                file.ContentType,
                file.Length,
                x,
                y,
                width,
                height,
                form["altText"].ToString(),
                form["fit"].ToString(),
                file.OpenReadStream),
            cancellationToken));
    }

    private static async Task<IResult> UpdateElementAsync(
        Guid materialId,
        Guid slideId,
        Guid elementId,
        UpdatePresentationElementCommand request,
        HttpContext context,
        IPresentationElementService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.UpdateAsync(
            teacherId,
            materialId,
            slideId,
            elementId,
            request,
            cancellationToken));
    }

    private static async Task<IResult> DeleteElementAsync(
        Guid materialId,
        Guid slideId,
        Guid elementId,
        [FromBody] PresentationConcurrencyCommand request,
        HttpContext context,
        IPresentationElementService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        return Map(await service.DeleteAsync(
            teacherId,
            materialId,
            slideId,
            elementId,
            request,
            cancellationToken));
    }

    private static void ConfigureWrite(RouteHandlerBuilder builder) => builder
        .WithMetadata(new RequestSizeLimitAttribute(CommandBodyLimit))
        .RequireRateLimiting(IdentityServiceExtensions.PresentationEditingRateLimitPolicy)
        .AddEndpointFilter(async (context, next) =>
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
        });

    private static void ConfigureImageWrite(RouteHandlerBuilder builder) => builder
        .WithMetadata(new RequestSizeLimitAttribute(ImageBodyLimit))
        .RequireRateLimiting(IdentityServiceExtensions.PresentationEditingRateLimitPolicy)
        .AddEndpointFilter(async (context, next) =>
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
        });

    private static IResult Map(PresentationMutationResult result) => result.Outcome switch
    {
        PresentationMutationOutcome.Success => TypedResults.Ok(new
        {
            slideId = result.SlideId,
            elementId = result.ElementId,
        }),
        PresentationMutationOutcome.InvalidInput => Problem(
            400,
            "presentation_invalid_request"),
        PresentationMutationOutcome.NotFound => Problem(404, "material_not_found"),
        PresentationMutationOutcome.Conflict => Problem(409, "concurrency_conflict"),
        PresentationMutationOutcome.SlideLimitReached => Problem(
            409,
            "presentation_slide_limit_reached"),
        PresentationMutationOutcome.LastSlide => Problem(
            409,
            "presentation_last_slide_required"),
        PresentationMutationOutcome.ElementLimitReached => Problem(
            409,
            "presentation_element_limit_reached"),
        PresentationMutationOutcome.PayloadLimitReached => Problem(
            409,
            "presentation_payload_limit_reached"),
        PresentationMutationOutcome.InvalidAsset => Problem(
            400,
            "presentation_invalid_asset"),
        PresentationMutationOutcome.MalwareDetected => Problem(
            422,
            "presentation_asset_malware_detected"),
        PresentationMutationOutcome.ScannerUnavailable => Problem(
            503,
            "presentation_asset_scanner_unavailable"),
        PresentationMutationOutcome.StorageFailure => Problem(
            503,
            "material_storage_unavailable"),
        _ => Problem(500, "presentation_write_failed"),
    };

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Presentation editor request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
