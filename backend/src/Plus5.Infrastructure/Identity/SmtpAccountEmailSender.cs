using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Plus5.Application.Identity;

namespace Plus5.Infrastructure.Identity;

public sealed class SmtpAccountEmailSender(IOptions<EmailDeliveryOptions> options)
    : IAccountEmailSender
{
    private readonly EmailDeliveryOptions _options = options.Value;

    public Task SendEmailVerificationAsync(
        string email,
        string token,
        CancellationToken cancellationToken) =>
        SendAsync(
            email,
            "Potvrdite PLUS 5 e-mail adresu",
            $"Vaš jednokratni kod za potvrdu e-mail adrese je:\n\n{token}\n\nKod vrijedi 24 sata.",
            cancellationToken);

    public Task SendPasswordResetAsync(
        string email,
        string token,
        CancellationToken cancellationToken) =>
        SendAsync(
            email,
            "Reset PLUS 5 lozinke",
            $"Vaš jednokratni kod za postavljanje nove lozinke je:\n\n{token}\n\nKod vrijedi 60 minuta.",
            cancellationToken);

    private async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage(_options.FromAddress, recipient, subject, body);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 10_000,
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
