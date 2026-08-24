using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Plus5.Api.Observability;

internal sealed partial class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public const string TraceHeaderName = "X-Trace-Id";
    private const string LiveHealthRoute = "/health/live";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startedAt = Stopwatch.GetTimestamp();
        var traceId = TraceContext.GetTraceId(context);
        context.Response.Headers[TraceHeaderName] = traceId;

        await next(context);

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var routeTemplate = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";

        if (routeTemplate == LiveHealthRoute
            && context.Response.StatusCode < StatusCodes.Status400BadRequest)
        {
            return;
        }

        LogRequestCompleted(
            logger,
            context.Request.Method,
            routeTemplate,
            context.Response.StatusCode,
            elapsedMilliseconds,
            traceId);
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "HTTP {RequestMethod} {RouteTemplate} responded {StatusCode} in {ElapsedMilliseconds} ms for trace {TraceId}.")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string requestMethod,
        string routeTemplate,
        int statusCode,
        double elapsedMilliseconds,
        string traceId);
}
