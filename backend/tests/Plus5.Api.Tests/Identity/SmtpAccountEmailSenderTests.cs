using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Tests.Identity;

public sealed class SmtpAccountEmailSenderTests
{
    [Fact]
    public async Task VerificationMessageUsesConfiguredSmtpTransport()
    {
        await using var server = new TestSmtpServer();
        var sender = new SmtpAccountEmailSender(Options.Create(new EmailDeliveryOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port,
            UseSsl = false,
            FromAddress = "no-reply@plus5.local",
        }));

        await sender.SendEmailVerificationAsync(
            "teacher@example.test",
            "TEST-VERIFICATION-TOKEN",
            CancellationToken.None);
        var message = await server.Message;

        Assert.Contains("RCPT TO:<teacher@example.test>", message.Commands, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("TEST-VERIFICATION-TOKEN", DecodeMimeBody(message.Body), StringComparison.Ordinal);
    }

    private static string DecodeMimeBody(string message)
    {
        var normalizedMessage = message.Replace("\r\n", "\n", StringComparison.Ordinal);
        var separator = normalizedMessage.IndexOf("\n\n", StringComparison.Ordinal);
        if (separator < 0
            || !normalizedMessage.Contains(
                "Content-Transfer-Encoding: base64",
                StringComparison.OrdinalIgnoreCase))
        {
            return normalizedMessage;
        }

        var encodedBody = normalizedMessage[(separator + 2)..]
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        return Encoding.UTF8.GetString(Convert.FromBase64String(encodedBody));
    }

    private sealed class TestSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Task<SmtpMessage> _message;

        public TestSmtpServer()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _message = ReceiveAsync();
        }

        public int Port { get; }

        public Task<SmtpMessage> Message => _message;

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            try
            {
                await _message;
            }
            catch (SocketException)
            {
            }
        }

        private async Task<SmtpMessage> ReceiveAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
            {
                AutoFlush = true,
                NewLine = "\r\n",
            };
            var commands = new List<string>();
            var body = new StringBuilder();

            await writer.WriteLineAsync("220 localhost test smtp");
            while (await reader.ReadLineAsync() is { } line)
            {
                commands.Add(line);
                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    while (await reader.ReadLineAsync() is { } dataLine && dataLine != ".")
                    {
                        body.AppendLine(dataLine);
                    }

                    await writer.WriteLineAsync("250 queued");
                }
                else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }

            return new SmtpMessage(commands, body.ToString());
        }
    }

    private sealed record SmtpMessage(IReadOnlyList<string> Commands, string Body);
}
