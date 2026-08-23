using Microsoft.Extensions.Options;
using Plus5.Api.Configuration;

namespace Plus5.Api.Tests.Configuration;

public sealed class ConfigurationValidationTests
{
    private readonly FrontendOptionsValidator validator = new();

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void SupportedEnvironmentIsAccepted(string environmentName)
    {
        ConfigurationExtensions.EnsureEnvironmentIsSupported(environmentName);
    }

    [Fact]
    public void UnsupportedEnvironmentFailsFast()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ConfigurationExtensions.EnsureEnvironmentIsSupported("Local"));

        Assert.Contains("Unsupported ASP.NET Core environment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingAllowedHostsFailsFast()
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfigurationExtensions.EnsureAllowedHostsAreRestricted(null));
    }

    [Fact]
    public void WildcardAllowedHostsFailsFast()
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfigurationExtensions.EnsureAllowedHostsAreRestricted("*"));
    }

    [Fact]
    public void ExplicitAllowedHostsIsAccepted()
    {
        ConfigurationExtensions.EnsureAllowedHostsAreRestricted("localhost;127.0.0.1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:5173")]
    [InlineData("ftp://example.test")]
    [InlineData("https://user:password@example.test")]
    [InlineData("https://example.test/path")]
    [InlineData("https://example.test?value=1")]
    public void InvalidFrontendPublicOriginIsRejected(string publicOrigin)
    {
        var result = validator.Validate(
            Options.DefaultName,
            new FrontendOptions { PublicOrigin = publicOrigin });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("https://plus5.example.test")]
    public void ValidFrontendPublicOriginIsAccepted(string publicOrigin)
    {
        var result = validator.Validate(
            Options.DefaultName,
            new FrontendOptions { PublicOrigin = publicOrigin });

        Assert.True(result.Succeeded);
    }
}
