namespace Plus5.Domain.Materials;

public sealed class PresentationAsset
{
    public const long MaximumImageSizeBytes = 10 * MaterialFilePolicy.Megabyte;
    public const long MaximumVideoSizeBytes = 250 * MaterialFilePolicy.Megabyte;
    public const long MaximumAudioSizeBytes = 25 * MaterialFilePolicy.Megabyte;

    private PresentationAsset()
    {
    }

    public PresentationAsset(
        Guid id,
        Guid materialVersionId,
        Guid ownerTeacherId,
        Guid materialId,
        string originalFileName,
        string mediaType,
        long sizeBytes,
        string storageProvider,
        string storageContainer,
        string sha256Checksum,
        DateTimeOffset createdAtUtc)
    {
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Identifier(materialVersionId, nameof(materialVersionId));
        MaterialGuard.Identifier(ownerTeacherId, nameof(ownerTeacherId));
        MaterialGuard.Identifier(materialId, nameof(materialId));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        var assetKindCode = KindForMediaType(mediaType);
        if (sizeBytes <= 0 || sizeBytes > MaximumSizeBytes(assetKindCode))
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        Id = id;
        MaterialVersionId = materialVersionId;
        AssetKindCode = assetKindCode;
        OriginalFileName = NormalizeFileName(originalFileName, mediaType);
        MediaType = NormalizeMediaType(mediaType);
        SizeBytes = sizeBytes;
        StorageProvider = MaterialGuard.Code(
            storageProvider,
            MaterialFilePolicy.StorageProviderMaxLength,
            nameof(storageProvider));
        StorageContainer = MaterialGuard.RequiredText(
            storageContainer,
            MaterialFilePolicy.StorageContainerMaxLength,
            nameof(storageContainer));
        ObjectKey = $"presentation-assets/{ownerTeacherId:N}/{materialId:N}/{materialVersionId:N}/{id:N}";
        Sha256Checksum = NormalizeSha256(sha256Checksum);
        Status = PresentationAssetStatus.Clean;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialVersionId { get; private set; }

    public string AssetKindCode { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string MediaType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StorageProvider { get; private set; } = string.Empty;

    public string StorageContainer { get; private set; } = string.Empty;

    public string ObjectKey { get; private set; } = string.Empty;

    public string Sha256Checksum { get; private set; } = string.Empty;

    public PresentationAssetStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void MarkQuarantined()
    {
        if (Status != PresentationAssetStatus.Clean)
        {
            throw new InvalidOperationException("Only a newly validated asset can be quarantined.");
        }

        Status = PresentationAssetStatus.Quarantined;
    }

    public static bool IsAllowedMediaType(string? mediaType) =>
        mediaType is "image/png" or "image/jpeg" or "image/webp"
            or "video/mp4" or "audio/mpeg";

    public static string KindForMediaType(string? mediaType) => mediaType switch
    {
        "image/png" or "image/jpeg" or "image/webp" => "IMAGE",
        "video/mp4" => "VIDEO",
        "audio/mpeg" => "AUDIO",
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType)),
    };

    public static long MaximumSizeBytes(string assetKindCode) => assetKindCode switch
    {
        "IMAGE" => MaximumImageSizeBytes,
        "VIDEO" => MaximumVideoSizeBytes,
        "AUDIO" => MaximumAudioSizeBytes,
        _ => throw new ArgumentOutOfRangeException(nameof(assetKindCode)),
    };

    public static string RequiredExtension(string mediaType) => mediaType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        "video/mp4" => ".mp4",
        "audio/mpeg" => ".mp3",
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType)),
    };

    private static string NormalizeFileName(string value, string mediaType)
    {
        var normalized = MaterialGuard.RequiredText(
            value,
            MaterialFilePolicy.OriginalFileNameMaxLength,
            nameof(value));
        var extension = Path.GetExtension(normalized);
        var validExtension = mediaType == "image/jpeg"
            ? extension is ".jpg" or ".jpeg" or ".JPG" or ".JPEG"
            : string.Equals(extension, RequiredExtension(mediaType), StringComparison.OrdinalIgnoreCase);
        if (!validExtension
            || normalized.IndexOfAny(['/', '\\']) >= 0
            || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Asset filename is not safe or does not match its type.", nameof(value));
        }

        return normalized!;
    }

    private static string NormalizeMediaType(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (!IsAllowedMediaType(normalized))
        {
            throw new ArgumentException("Unsupported presentation asset media type.", nameof(value));
        }

        return normalized!;
    }

    private static string NormalizeSha256(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (normalized?.Length != MaterialFilePolicy.ChecksumMaxLength
            || normalized.Any(character =>
                !(character is >= 'A' and <= 'F' or >= '0' and <= '9')))
        {
            throw new ArgumentException("Invalid SHA-256 checksum.", nameof(value));
        }

        return normalized!;
    }
}

public enum PresentationAssetStatus
{
    Clean = 1,
    Quarantined = 2,
}
