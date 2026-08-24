using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Plus5.Api.Conventions;

internal static class ApiProblemDetailsDefaults
{
    private const string CodeExtensionName = "code";
    private const string TraceIdExtensionName = "traceId";

    internal static void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problemDetails = context.ProblemDetails;
        var status = problemDetails.Status ?? context.HttpContext.Response.StatusCode;
        var hasExplicitCode = problemDetails.Extensions.TryGetValue(CodeExtensionName, out var codeValue)
            && codeValue is string explicitCode
            && !string.IsNullOrWhiteSpace(explicitCode);
        var code = hasExplicitCode
            ? (string)codeValue!
            : GetDefaultCode(status, problemDetails is HttpValidationProblemDetails);

        problemDetails.Status = status;
        problemDetails.Extensions[CodeExtensionName] = code;
        problemDetails.Extensions[TraceIdExtensionName] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        problemDetails.Instance ??= context.HttpContext.Request.Path.Value;

        if (!hasExplicitCode || string.IsNullOrWhiteSpace(problemDetails.Type))
        {
            problemDetails.Type = $"urn:plus5:problem:{code}";
        }

        if (!hasExplicitCode)
        {
            problemDetails.Title = GetDefaultTitle(code, problemDetails.Title);
        }

        if (status == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Detail = null;
        }
    }

    private static string GetDefaultCode(int status, bool isValidationProblem) =>
        (status, isValidationProblem) switch
        {
            (_, true) => ApiProblemCodes.ValidationFailed,
            (StatusCodes.Status400BadRequest, _) => ApiProblemCodes.InvalidRequest,
            (StatusCodes.Status401Unauthorized, _) => ApiProblemCodes.AuthenticationRequired,
            (StatusCodes.Status403Forbidden, _) => ApiProblemCodes.Forbidden,
            (StatusCodes.Status404NotFound, _) => ApiProblemCodes.NotFound,
            (StatusCodes.Status405MethodNotAllowed, _) => ApiProblemCodes.MethodNotAllowed,
            (StatusCodes.Status409Conflict, _) => ApiProblemCodes.Conflict,
            (StatusCodes.Status413PayloadTooLarge, _) => ApiProblemCodes.PayloadTooLarge,
            (StatusCodes.Status415UnsupportedMediaType, _) => ApiProblemCodes.UnsupportedMediaType,
            (StatusCodes.Status422UnprocessableEntity, _) => ApiProblemCodes.ValidationFailed,
            (StatusCodes.Status429TooManyRequests, _) => ApiProblemCodes.TooManyRequests,
            (StatusCodes.Status500InternalServerError, _) => ApiProblemCodes.InternalError,
            (StatusCodes.Status503ServiceUnavailable, _) => ApiProblemCodes.ServiceUnavailable,
            ( >= StatusCodes.Status500InternalServerError, _) => ApiProblemCodes.ServerError,
            _ => ApiProblemCodes.RequestFailed,
        };

    private static string GetDefaultTitle(string code, string? fallbackTitle) =>
        code switch
        {
            ApiProblemCodes.ValidationFailed => "Request validation failed.",
            ApiProblemCodes.InvalidRequest => "The request is invalid.",
            ApiProblemCodes.AuthenticationRequired => "Authentication is required.",
            ApiProblemCodes.Forbidden => "Access is forbidden.",
            ApiProblemCodes.NotFound => "The requested resource was not found.",
            ApiProblemCodes.MethodNotAllowed => "The HTTP method is not allowed.",
            ApiProblemCodes.Conflict => "The request conflicts with the current resource state.",
            ApiProblemCodes.PayloadTooLarge => "The request payload is too large.",
            ApiProblemCodes.UnsupportedMediaType => "The request media type is not supported.",
            ApiProblemCodes.TooManyRequests => "Too many requests.",
            ApiProblemCodes.InternalError => "An unexpected error occurred.",
            ApiProblemCodes.ServiceUnavailable => "The service is temporarily unavailable.",
            ApiProblemCodes.ServerError => "A server error occurred.",
            _ => fallbackTitle ?? "The request could not be completed.",
        };
}
