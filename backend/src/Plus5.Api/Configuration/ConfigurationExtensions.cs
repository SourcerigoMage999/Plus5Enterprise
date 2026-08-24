using Microsoft.Extensions.Options;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Configuration;

public static class ConfigurationExtensions
{
    private static readonly HashSet<string> SupportedEnvironments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Environments.Development,
            Environments.Staging,
            Environments.Production,
        };

    public static WebApplicationBuilder AddValidatedConfiguration(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EnsureEnvironmentIsSupported(builder.Environment.EnvironmentName);
        EnsureAllowedHostsAreRestricted(builder.Configuration["AllowedHosts"]);

        builder.Services.AddSingleton<IValidateOptions<FrontendOptions>, FrontendOptionsValidator>();
        builder.Services.AddSingleton<IValidateOptions<EmailDeliveryOptions>, EmailDeliveryOptionsValidator>();
        builder.Services.AddSingleton<
            IValidateOptions<DataProtectionPersistenceOptions>,
            DataProtectionPersistenceOptionsValidator>();
        builder.Services
            .AddOptions<FrontendOptions>()
            .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
            .ValidateOnStart();
        builder.Services
            .AddOptions<EmailDeliveryOptions>()
            .Bind(builder.Configuration.GetSection(EmailDeliveryOptions.SectionName))
            .ValidateOnStart();
        builder.Services
            .AddOptions<DataProtectionPersistenceOptions>()
            .Bind(builder.Configuration.GetSection(DataProtectionPersistenceOptions.SectionName))
            .ValidateOnStart();

        return builder;
    }

    public static void EnsureEnvironmentIsSupported(string environmentName)
    {
        if (!SupportedEnvironments.Contains(environmentName))
        {
            throw new InvalidOperationException(
                $"Unsupported ASP.NET Core environment '{environmentName}'. " +
                $"Supported values are {string.Join(", ", SupportedEnvironments.Order())}.");
        }
    }

    public static void EnsureAllowedHostsAreRestricted(string? allowedHosts)
    {
        var hosts = allowedHosts?.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hosts is not { Length: > 0 }
            || hosts.Any(host => host is "*" or "+"))
        {
            throw new InvalidOperationException(
                "AllowedHosts must contain an explicit semicolon-separated host allowlist; wildcards are not permitted.");
        }
    }
}
