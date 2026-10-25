using Microsoft.Extensions.Options;
using Plus5.Infrastructure.Materials;

namespace Plus5.Api.Configuration;

public sealed class PresentationAiOptionsValidator : IValidateOptions<PresentationAiOptions>
{
    public ValidateOptionsResult Validate(string? name, PresentationAiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TimeoutSeconds is < 1 or > 30)
        {
            return ValidateOptionsResult.Fail("PresentationAi:TimeoutSeconds must be between 1 and 30.");
        }

        if (!string.Equals(options.BaseUrl, PresentationAiOptions.RequiredBaseUrl, StringComparison.Ordinal)
            || !string.Equals(options.Model, PresentationAiOptions.CanonicalModel, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(options.PromptTemplateVersion))
        {
            return ValidateOptionsResult.Fail(
                "PresentationAi must use the locked EU endpoint, canonical model and prompt template version.");
        }

        if (options.Enabled && string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "PresentationAi:ApiKey is required only when paid AI assistance is explicitly enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
