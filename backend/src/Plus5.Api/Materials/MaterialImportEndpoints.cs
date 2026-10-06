using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Plus5.Api.Identity;
using Plus5.Application.Materials;

namespace Plus5.Api.Materials;

public static class MaterialImportEndpoints
{
    private const long MaximumRequestBytes = 251L * 1024 * 1024;

    public static IEndpointRouteBuilder MapMaterialImport(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var group = endpoints.MapGroup("/api/v1/materials")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("/import/options", GetOptionsAsync);
        group.MapPost("/import", ImportAsync)
            .RequireRateLimiting(IdentityServiceExtensions.MaterialUploadRateLimitPolicy)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .WithMetadata(new RequestFormLimitsAttribute
            {
                MultipartBodyLengthLimit = MaximumRequestBytes,
                ValueLengthLimit = 256 * 1024,
            });
        return endpoints;
    }

    private static async Task<IResult> GetOptionsAsync(
        HttpContext context,
        IMaterialImportQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var options = await query.GetOptionsAsync(teacherId, cancellationToken);
        return TypedResults.Ok(options);
    }

    private static async Task<IResult> ImportAsync(
        HttpContext context,
        IMaterialImportService service,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherId, out _))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Problem(400, "invalid_csrf_token");
        }

        if (!context.Request.HasFormContentType)
        {
            return Problem(415, "material_import_multipart_required");
        }

        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            var metadataJson = form["metadata"].ToString();
            var metadata = JsonSerializer.Deserialize<ImportMetadataRequest>(
                metadataJson,
                JsonSerializerOptions.Web);
            if (file is null || metadata is null || file.Length <= 0)
            {
                return Problem(400, "material_import_invalid_request");
            }

            var safeFileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
            if (!TryMapFormat(safeFileName, out var format))
            {
                return Problem(400, "material_import_unsupported_format");
            }

            var result = await service.ImportAsync(
                teacherId,
                new MaterialImportCommand(
                    safeFileName,
                    file.ContentType,
                    file.Length,
                    format,
                    file.OpenReadStream,
                    metadata.Title,
                    metadata.MaterialTypeCode,
                    metadata.Description,
                    metadata.Subject,
                    metadata.LanguageCode,
                    metadata.Visibility,
                    metadata.ProgramId,
                    metadata.SchoolGradeId,
                    metadata.ProficiencyLevelId,
                    metadata.LearningGoal,
                    metadata.Tags ?? [],
                    metadata.KnowledgeComponentIds ?? [],
                    metadata.CurriculumOutcomeIds ?? []),
                cancellationToken);

            return result.Outcome switch
            {
                MaterialImportOutcome.Created => TypedResults.Created(
                    $"/api/v1/materials/{result.MaterialId:D}",
                    new MaterialImportCreatedResponse(result.MaterialId!.Value)),
                MaterialImportOutcome.InvalidInput => Problem(400, "material_import_invalid_request"),
                MaterialImportOutcome.InvalidFile => Problem(400, "material_import_file_rejected"),
                MaterialImportOutcome.ReferenceNotFound => Problem(404, "material_import_reference_not_found"),
                MaterialImportOutcome.MalwareDetected => Problem(422, "material_import_malware_detected"),
                MaterialImportOutcome.ScannerUnavailable => Problem(503, "material_import_scanner_unavailable"),
                _ => Problem(500, "material_import_failed"),
            };
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or JsonException)
        {
            return Problem(400, "material_import_invalid_request");
        }
    }

    private static bool TryMapFormat(string fileName, out MaterialImportFileFormat format)
    {
        format = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => MaterialImportFileFormat.Pdf,
            ".docx" => MaterialImportFileFormat.Docx,
            ".pptx" => MaterialImportFileFormat.Pptx,
            ".mp4" => MaterialImportFileFormat.Mp4,
            ".zip" => MaterialImportFileFormat.Zip,
            _ => 0,
        };
        return format != 0;
    }

    private static IResult Problem(int status, string code) => Results.Problem(
        statusCode: status,
        title: "Material import request could not be completed.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private sealed record ImportMetadataRequest(
        string Title,
        string MaterialTypeCode,
        string? Description,
        string? Subject,
        string? LanguageCode,
        MaterialImportVisibility Visibility,
        Guid? ProgramId,
        Guid? SchoolGradeId,
        Guid? ProficiencyLevelId,
        string? LearningGoal,
        IReadOnlyCollection<string>? Tags,
        IReadOnlyCollection<Guid>? KnowledgeComponentIds,
        IReadOnlyCollection<Guid>? CurriculumOutcomeIds);

    private sealed record MaterialImportCreatedResponse(Guid MaterialId);
}
