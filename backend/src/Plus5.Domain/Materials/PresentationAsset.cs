namespace Plus5.Domain.Materials;

public sealed class PresentationAsset
{
    public const long MaximumImageSizeBytes = 10 * MaterialFilePolicy.Megabyte;

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
        if (sizeBytes is <= 0 or > MaximumImageSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        Id = id;
        MaterialVersionId = materialVersionId;
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

    public string OriginalFileName { get; private set; } = string.Empty;

    public string MediaType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StorageProvider { get; private set; } = string.Empty;

    public string StorageContainer { get; private set; } = string.Empty;

    public string ObjectKey { get; private set; } = string.Empty;

    public string Sha256Checksum { get; private set; } = string.Empty;

    public PresentationAssetStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static bool IsAllowedMediaType(string? mediaType) =>
        mediaType is "image/png" or "image/jpeg" or "image/webp";

    public static string RequiredExtension(string mediaType) => mediaType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
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
            throw new ArgumentException("Image filename is not safe or does not match its type.", nameof(value));
        }

        return normalized!;
    }

    private static string NormalizeMediaType(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (!IsAllowedMediaType(normalized))
        {
            throw new ArgumentException("Only PNG, JPEG and WebP images are supported.", nameof(value));
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
}
