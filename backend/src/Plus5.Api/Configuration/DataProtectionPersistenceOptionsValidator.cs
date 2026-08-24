using Microsoft.Extensions.Options;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Configuration;

public sealed class DataProtectionPersistenceOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<DataProtectionPersistenceOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        DataProtectionPersistenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var hasCertificatePath = !string.IsNullOrWhiteSpace(options.CertificatePath);
        var hasCertificatePassword = !string.IsNullOrWhiteSpace(options.CertificatePassword);

        if (hasCertificatePath != hasCertificatePassword)
        {
            return ValidateOptionsResult.Fail(
                "DataProtection:CertificatePath and DataProtection:CertificatePassword " +
                "must either both be supplied or both be omitted.");
        }

        if (!environment.IsDevelopment() && !hasCertificatePath)
        {
            return ValidateOptionsResult.Fail(
                "A Data Protection certificate is required outside Development.");
        }

        if (hasCertificatePath && !Path.IsPathFullyQualified(options.CertificatePath!))
        {
            return ValidateOptionsResult.Fail(
                "DataProtection:CertificatePath must be an absolute path.");
        }

        if (hasCertificatePath && !File.Exists(options.CertificatePath))
        {
            return ValidateOptionsResult.Fail(
                "The configured Data Protection certificate file does not exist.");
        }

        return ValidateOptionsResult.Success;
    }
}
