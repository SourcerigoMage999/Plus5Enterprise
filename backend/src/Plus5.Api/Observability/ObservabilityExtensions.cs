using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Plus5.Api.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "plus5-api";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var section = builder.Configuration.GetSection(ObservabilityOptions.SectionName);
        var options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        var validationResult = new ObservabilityOptionsValidator(builder.Environment)
            .Validate(Options.DefaultName, options);

        if (validationResult.Failed)
        {
            throw new InvalidOperationException(string.Join(" ", validationResult.Failures));
        }

        builder.Services.AddSingleton<IValidateOptions<ObservabilityOptions>, ObservabilityOptionsValidator>();
        builder.Services
            .AddOptions<ObservabilityOptions>()
            .Bind(section)
            .ValidateOnStart();

        builder.Logging.ClearProviders();
        builder.Logging.Configure(loggingOptions =>
            loggingOptions.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
        builder.Logging.AddJsonConsole(consoleOptions =>
        {
            consoleOptions.IncludeScopes = true;
            consoleOptions.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            consoleOptions.UseUtcTimestamp = true;
        });

        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString();
        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ServiceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>(
                        "deployment.environment.name",
                        builder.Environment.EnvironmentName),
                ]));

        openTelemetry.WithTracing(tracing =>
        {
            tracing
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))
                .AddAspNetCoreInstrumentation(instrumentation =>
                {
                    instrumentation.Filter = context => context.Request.Path != "/health/live";
                    instrumentation.RecordException = false;
                })
                .AddProcessor(new SensitiveTelemetrySanitizerProcessor());

            if (TryGetOtlpEndpoint(options, out var endpoint))
            {
                tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
            }
        });

        openTelemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation();

            if (TryGetOtlpEndpoint(options, out var endpoint))
            {
                metrics.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
            }
        });

        return builder;
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestObservabilityMiddleware>();
    }

    private static bool TryGetOtlpEndpoint(
        ObservabilityOptions options,
        out Uri endpoint) =>
        Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out endpoint!);
}
