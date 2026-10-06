namespace Plus5.Infrastructure.Materials;

public sealed class MaterialStorageOptions
{
    public const string SectionName = "MaterialStorage";

    public string ServiceUrl { get; init; } = string.Empty;

    public string Region { get; init; } = "auto";

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;

    public string QuarantineBucket { get; init; } = string.Empty;

    public string CleanBucket { get; init; } = string.Empty;

    public string ProviderCode { get; init; } = "S3";

    public bool ForcePathStyle { get; init; }
}

public sealed class MalwareScannerOptions
{
    public const string SectionName = "MalwareScanner";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 3310;

    public int TimeoutSeconds { get; init; } = 120;
}
