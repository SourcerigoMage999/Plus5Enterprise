using Microsoft.Extensions.Options;
using Plus5.Infrastructure.LessonPlanning;

namespace Plus5.Api.Configuration;

public sealed class LessonBuilderAiOptionsValidator : IValidateOptions<LessonBuilderAiOptions>
{
    public ValidateOptionsResult Validate(string? name, LessonBuilderAiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TimeoutSeconds is < 1 or > 30)
            return ValidateOptionsResult.Fail("LessonBuilderAi:TimeoutSeconds must be between 1 and 30.");
        if (!string.Equals(options.BaseUrl, LessonBuilderAiOptions.RequiredBaseUrl, StringComparison.Ordinal)
            || !string.Equals(options.Model, LessonBuilderAiOptions.CanonicalModel, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(options.PromptTemplateVersion))
            return ValidateOptionsResult.Fail("LessonBuilderAi must use the locked EU endpoint, canonical model and prompt template version.");
        if (options.Enabled && string.IsNullOrWhiteSpace(options.ApiKey))
            return ValidateOptionsResult.Fail("LessonBuilderAi:ApiKey is required only when paid AI assistance is explicitly enabled.");
        return ValidateOptionsResult.Success;
    }
}
