using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Plus5.Api.Observability;

public sealed class ObservabilityOptionsValidator : IValidateOptions<ObservabilityOptions>
{
    private readonly string environmentName;

    public ObservabilityOptionsValidator(IHostEnvironment environment)
        : this(environment?.EnvironmentName ?? throw new ArgumentNullException(nameof(environment)))
    {
    }

    public ObservabilityOptionsValidator(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        this.environmentName = environmentName;
    }

    public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!double.IsFinite(options.TraceSamplingRatio)
            || options.TraceSamplingRatio is <= 0 or > 1)
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilityOptions.SectionName}:TraceSamplingRatio must be greater than 0 and less than or equal to 1.");
        }

        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            return ValidateOptionsResult.Success;
        }

        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilityOptions.SectionName}:OtlpEndpoint must be an absolute HTTP(S) URI without credentials, query, or fragment.");
        }

        if (!environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase)
            && endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilityOptions.SectionName}:OtlpEndpoint must use HTTPS outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
