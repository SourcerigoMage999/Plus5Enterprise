namespace Plus5.Infrastructure.Identity;

public sealed class DataProtectionPersistenceOptions
{
    public const string SectionName = "DataProtection";

    public string? CertificatePath { get; init; }

    public string? CertificatePassword { get; init; }
}
