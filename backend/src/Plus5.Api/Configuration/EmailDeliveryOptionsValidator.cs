using System.Net.Mail;
using Microsoft.Extensions.Options;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Configuration;

public sealed class EmailDeliveryOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<EmailDeliveryOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailDeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            return ValidateOptionsResult.Fail("Email:Host is required.");
        }

        if (options.Port is < 1 or > 65535)
        {
            return ValidateOptionsResult.Fail("Email:Port must be between 1 and 65535.");
        }

        try
        {
            _ = new MailAddress(options.FromAddress);
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail("Email:FromAddress must be a valid e-mail address.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName) != string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail(
                "Email:UserName and Email:Password must either both be supplied or both be omitted.");
        }

        if (!environment.IsDevelopment() && !options.UseSsl)
        {
            return ValidateOptionsResult.Fail("Email:UseSsl must be true outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
