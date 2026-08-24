namespace Plus5.Infrastructure.Identity;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public bool UseSsl { get; init; } = true;

    public string FromAddress { get; init; } = string.Empty;

    public string? UserName { get; init; }

    public string? Password { get; init; }
}
