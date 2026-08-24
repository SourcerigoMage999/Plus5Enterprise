namespace Plus5.Api.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";
    public const double DefaultTraceSamplingRatio = 0.1;

    public string OtlpEndpoint { get; init; } = string.Empty;

    public double TraceSamplingRatio { get; init; } = DefaultTraceSamplingRatio;
}
