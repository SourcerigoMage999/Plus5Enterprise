using Microsoft.Extensions.Options;
using Plus5.Api.Observability;

namespace Plus5.Api.Tests.Observability;

public sealed class ObservabilityOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidTraceSamplingRatioIsRejected(double ratio)
    {
        var result = Validate(new ObservabilityOptions { TraceSamplingRatio = ratio });

        Assert.True(result.Failed);
        Assert.Contains("TraceSamplingRatio", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    public void ValidTraceSamplingRatioIsAccepted(double ratio)
    {
        var result = Validate(new ObservabilityOptions { TraceSamplingRatio = ratio });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://collector.example.test:4317")]
    public void OptionalOrSecureProductionEndpointIsAccepted(string endpoint)
    {
        var result = Validate(new ObservabilityOptions { OtlpEndpoint = endpoint });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DevelopmentEndpointMayUseHttp()
    {
        var result = Validate(
            new ObservabilityOptions { OtlpEndpoint = "http://localhost:4317" },
            "Development");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("http://collector.example.test:4317")]
    [InlineData("ftp://collector.example.test")]
    [InlineData("https://user:password@collector.example.test")]
    [InlineData("https://collector.example.test?token=secret")]
    [InlineData("https://collector.example.test#fragment")]
    public void UnsafeProductionEndpointIsRejectedWithoutEchoingItsValue(string endpoint)
    {
        var result = Validate(new ObservabilityOptions { OtlpEndpoint = endpoint });

        Assert.True(result.Failed);
        Assert.DoesNotContain(endpoint, result.FailureMessage, StringComparison.Ordinal);
    }

    private static ValidateOptionsResult Validate(
        ObservabilityOptions options,
        string environmentName = "Production") =>
        new ObservabilityOptionsValidator(environmentName).Validate(Options.DefaultName, options);
}
