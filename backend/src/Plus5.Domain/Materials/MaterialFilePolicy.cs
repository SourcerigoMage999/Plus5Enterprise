namespace Plus5.Domain.Materials;

public static class MaterialFilePolicy
{
    public const long Megabyte = 1024 * 1024;
    public const int OriginalFileNameMaxLength = 255;
    public const int MediaTypeMaxLength = 160;
    public const int StorageProviderMaxLength = 64;
    public const int StorageContainerMaxLength = 128;
    public const int ObjectKeyMaxLength = 512;
    public const int ChecksumMaxLength = 64;
    public const int ScanResultCategoryMaxLength = 64;
    public const int MaximumScanAttempts = 3;
    public const int MaximumZipEntries = 500;
    public const long MaximumZipExpandedBytes = 500 * Megabyte;
    public const decimal MaximumZipEntryCompressionRatio = 100m;

    public static TimeSpan MaximumReadAccessLifetime => TimeSpan.FromMinutes(5);

    public static long MaximumSizeBytes(MaterialFileFormat format) => format switch
    {
        MaterialFileFormat.Pdf => 50 * Megabyte,
        MaterialFileFormat.Docx => 25 * Megabyte,
        MaterialFileFormat.Pptx => 100 * Megabyte,
        MaterialFileFormat.Mp4 => 250 * Megabyte,
        MaterialFileFormat.Zip => 100 * Megabyte,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static string RequiredExtension(MaterialFileFormat format) => format switch
    {
        MaterialFileFormat.Pdf => ".pdf",
        MaterialFileFormat.Docx => ".docx",
        MaterialFileFormat.Pptx => ".pptx",
        MaterialFileFormat.Mp4 => ".mp4",
        MaterialFileFormat.Zip => ".zip",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsAllowedMediaType(MaterialFileFormat format, string mediaType)
    {
        var normalized = mediaType?.Trim();
        return format switch
        {
            MaterialFileFormat.Pdf => normalized == "application/pdf",
            MaterialFileFormat.Docx => normalized
                == "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            MaterialFileFormat.Pptx => normalized
                == "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            MaterialFileFormat.Mp4 => normalized == "video/mp4",
            MaterialFileFormat.Zip => normalized is "application/zip"
                or "application/x-zip-compressed",
            _ => false,
        };
    }

    public static void EnsureReadAccessLifetime(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > MaximumReadAccessLifetime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "Material read access must expire within five minutes.");
        }
    }
}
