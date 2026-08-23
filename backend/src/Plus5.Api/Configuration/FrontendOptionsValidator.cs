using Microsoft.Extensions.Options;

namespace Plus5.Api.Configuration;

public sealed class FrontendOptionsValidator : IValidateOptions<FrontendOptions>
{
    public ValidateOptionsResult Validate(string? name, FrontendOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Uri.TryCreate(options.PublicOrigin, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
            || origin.AbsolutePath != "/"
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment)
            || !string.IsNullOrEmpty(origin.UserInfo))
        {
            return ValidateOptionsResult.Fail(
                $"{FrontendOptions.SectionName}:PublicOrigin must be an absolute HTTP(S) origin without credentials, a path, query, or fragment.");
        }

        return ValidateOptionsResult.Success;
    }
}
