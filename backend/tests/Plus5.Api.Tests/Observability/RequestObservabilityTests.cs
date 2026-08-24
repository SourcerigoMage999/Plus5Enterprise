using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plus5.Api.Observability;

namespace Plus5.Api.Tests.Observability;

public sealed class RequestObservabilityTests
{
    private const string InboundTraceId = "0af7651916cd43dd8448eb211c80319c";

    [Fact]
    public async Task RequestLogUsesRouteTemplateAndPropagatesTraceIdWithoutSensitiveValues()
    {
        await using var app = await StartTestApplication();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/probes/sensitive-path-value?secret=sensitive-query-value");
        request.Headers.TryAddWithoutValidation(
            "traceparent",
            $"00-{InboundTraceId}-b7ad6b7169203331-01");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer sensitive-token-value");

        using var response = await client.SendAsync(request, CancellationToken.None);
        var log = Assert.Single(app.Services.GetRequiredService<CollectingLoggerProvider>().Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InboundTraceId, response.Headers.GetValues(RequestObservabilityMiddleware.TraceHeaderName).Single());
        Assert.Equal(2000, log.EventId.Id);
        Assert.Equal("GET", log.Properties["RequestMethod"]);
        Assert.Equal("/api/v1/probes/{id}", log.Properties["RouteTemplate"]);
        Assert.Equal(StatusCodes.Status200OK, log.Properties["StatusCode"]);
        Assert.Equal(InboundTraceId, log.Properties["TraceId"]);
        Assert.DoesNotContain("sensitive-path-value", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-query-value", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-token-value", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuccessfulLiveHealthRequestDoesNotCreateCompletionLog()
    {
        await using var app = await StartTestApplication();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/health/live", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(app.Services.GetRequiredService<CollectingLoggerProvider>().Entries);
    }

    [Fact]
    public void TelemetrySanitizerRemovesSensitiveHttpTags()
    {
        using var activity = new Activity("request");
        activity.Start();
        activity.SetTag("http.request.method", "GET");
        activity.SetTag("url.path", "/api/v1/probes/42");
        activity.SetTag("url.query", "?secret=sensitive-query-value");
        activity.SetTag("url.full", "https://example.test/api/v1/probes/42?secret=sensitive-query-value");
        activity.SetTag("user_agent.original", "sensitive-user-agent");

        new SensitiveTelemetrySanitizerProcessor().OnEnd(activity);

        Assert.Equal("GET", activity.GetTagItem("http.request.method"));
        Assert.Null(activity.GetTagItem("url.path"));
        Assert.Null(activity.GetTagItem("url.query"));
        Assert.Null(activity.GetTagItem("url.full"));
        Assert.Null(activity.GetTagItem("user_agent.original"));
    }

    private static async Task<WebApplication> StartTestApplication()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(RequestObservabilityTests).Assembly.FullName,
            EnvironmentName = "Development",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:TraceSamplingRatio"] = "1",
        });
        builder.WebHost.UseTestServer();
        builder.AddObservability();
        builder.Logging.ClearProviders();

        var loggerProvider = new CollectingLoggerProvider();
        builder.Services.AddSingleton(loggerProvider);
        builder.Logging.AddProvider(loggerProvider);

        var app = builder.Build();
        app.UseObservability();
        app.MapGet("/api/v1/probes/{id}", (string id) => TypedResults.Ok(id));
        app.MapGet("/health/live", () => TypedResults.Ok());
        await app.StartAsync(CancellationToken.None);

        return app;
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<CapturedLog> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class CollectingLogger(CollectingLoggerProvider provider, string categoryName) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (categoryName != typeof(RequestObservabilityMiddleware).FullName
                    || eventId.Id != 2000)
                {
                    return;
                }

                var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                    ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
                    : new Dictionary<string, object?>();

                provider.Entries.Enqueue(new CapturedLog(eventId, formatter(state, exception), properties));
            }
        }
    }

    private sealed record CapturedLog(
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
