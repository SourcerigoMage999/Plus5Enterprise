namespace Plus5.Domain.Materials;

public sealed class MaterialFile
{
    private MaterialFile()
    {
    }

    public MaterialFile(
        Guid id,
        Material material,
        MaterialVersion version,
        MaterialFileFormat format,
        string originalFileName,
        string declaredMediaType,
        long declaredSizeBytes,
        string storageProvider,
        string storageContainer,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(version);
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        if (version.MaterialId != material.Id || version.Status != MaterialVersionStatus.Draft)
        {
            throw new ArgumentException(
                "A file can be created only for a draft version of the material.",
                nameof(version));
        }

        EnsureFormat(format);
        EnsureSize(format, declaredSizeBytes, nameof(declaredSizeBytes));
        var normalizedFileName = NormalizeFileName(originalFileName, format);
        var normalizedMediaType = MaterialGuard.RequiredText(
            declaredMediaType,
            MaterialFilePolicy.MediaTypeMaxLength,
            nameof(declaredMediaType));
        if (!MaterialFilePolicy.IsAllowedMediaType(format, normalizedMediaType))
        {
            throw new ArgumentException(
                "Declared media type does not match the allowed file format.",
                nameof(declaredMediaType));
        }

        Id = id;
        MaterialVersionId = version.Id;
        Format = format;
        OriginalFileName = normalizedFileName;
        DeclaredMediaType = normalizedMediaType;
        DeclaredSizeBytes = declaredSizeBytes;
        StorageProvider = MaterialGuard.Code(
            storageProvider,
            MaterialFilePolicy.StorageProviderMaxLength,
            nameof(storageProvider));
        StorageContainer = MaterialGuard.RequiredText(
            storageContainer,
            MaterialFilePolicy.StorageContainerMaxLength,
            nameof(storageContainer));
        ObjectKey = CreateObjectKey(material, version, id);
        Status = MaterialFileStatus.PendingUpload;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialVersionId { get; private set; }

    public MaterialFileFormat Format { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string DeclaredMediaType { get; private set; } = string.Empty;

    public long DeclaredSizeBytes { get; private set; }

    public long? ActualSizeBytes { get; private set; }

    public string StorageProvider { get; private set; } = string.Empty;

    public string StorageContainer { get; private set; } = string.Empty;

    public string ObjectKey { get; private set; } = string.Empty;

    public string? Sha256Checksum { get; private set; }

    public MaterialFileStatus Status { get; private set; }

    public int ScanAttemptCount { get; private set; }

    public string? LastScanResultCategory { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? UploadedAtUtc { get; private set; }

    public DateTimeOffset? ScannedAtUtc { get; private set; }

    public void MarkUploaded(
        long actualSizeBytes,
        string sha256Checksum,
        DateTimeOffset uploadedAtUtc)
    {
        EnsureStatus(MaterialFileStatus.PendingUpload);
        EnsureTimestamp(uploadedAtUtc, nameof(uploadedAtUtc));
        EnsureSize(Format, actualSizeBytes, nameof(actualSizeBytes));
        if (actualSizeBytes != DeclaredSizeBytes)
        {
            throw new InvalidOperationException(
                "Uploaded size must match the validated declared size.");
        }

        ActualSizeBytes = actualSizeBytes;
        Sha256Checksum = NormalizeSha256(sha256Checksum);
        UploadedAtUtc = uploadedAtUtc;
        UpdatedAtUtc = uploadedAtUtc;
        Status = MaterialFileStatus.Uploaded;
    }

    public void MarkCleanCopy(MaterialFile source, DateTimeOffset copiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureStatus(MaterialFileStatus.PendingUpload);
        EnsureTimestamp(copiedAtUtc, nameof(copiedAtUtc));
        if (source.Status != MaterialFileStatus.Clean
            || source.Format != Format
            || source.ActualSizeBytes is null
            || string.IsNullOrWhiteSpace(source.Sha256Checksum))
        {
            throw new ArgumentException("Only a clean, fully verified file can be copied.", nameof(source));
        }

        ActualSizeBytes = source.ActualSizeBytes;
        Sha256Checksum = source.Sha256Checksum;
        UploadedAtUtc = copiedAtUtc;
        ScannedAtUtc = copiedAtUtc;
        LastScanResultCategory = "CLEAN_COPY";
        UpdatedAtUtc = copiedAtUtc;
        Status = MaterialFileStatus.Clean;
    }

    public void StartScanning(DateTimeOffset startedAtUtc)
    {
        if (Status is not (MaterialFileStatus.Uploaded or MaterialFileStatus.Failed))
        {
            throw new InvalidOperationException(
                "Only an uploaded file or a failed transient scan can start scanning.");
        }

        if (ScanAttemptCount >= MaterialFilePolicy.MaximumScanAttempts)
        {
            throw new InvalidOperationException("Maximum malware scan attempts reached.");
        }

        EnsureTimestamp(startedAtUtc, nameof(startedAtUtc));
        ScanAttemptCount++;
        LastScanResultCategory = null;
        UpdatedAtUtc = startedAtUtc;
        Status = MaterialFileStatus.Scanning;
    }

    public void MarkClean(DateTimeOffset scannedAtUtc)
    {
        CompleteScan(MaterialFileStatus.Clean, "CLEAN", scannedAtUtc);
    }

    public void MarkQuarantined(string resultCategory, DateTimeOffset scannedAtUtc)
    {
        CompleteScan(MaterialFileStatus.Quarantined, resultCategory, scannedAtUtc);
    }

    public void MarkFailed(string resultCategory, DateTimeOffset failedAtUtc)
    {
        CompleteScan(MaterialFileStatus.Failed, resultCategory, failedAtUtc);
    }

    public void Reject(string resultCategory, DateTimeOffset rejectedAtUtc)
    {
        if (Status is not (MaterialFileStatus.PendingUpload or MaterialFileStatus.Uploaded))
        {
            throw new InvalidOperationException(
                "Only pending or uploaded content can be rejected by validation.");
        }

        EnsureTimestamp(rejectedAtUtc, nameof(rejectedAtUtc));
        LastScanResultCategory = MaterialGuard.Code(
            resultCategory,
            MaterialFilePolicy.ScanResultCategoryMaxLength,
            nameof(resultCategory));
        ScannedAtUtc = rejectedAtUtc;
        UpdatedAtUtc = rejectedAtUtc;
        Status = MaterialFileStatus.Rejected;
    }

    private void CompleteScan(
        MaterialFileStatus targetStatus,
        string resultCategory,
        DateTimeOffset completedAtUtc)
    {
        EnsureStatus(MaterialFileStatus.Scanning);
        EnsureTimestamp(completedAtUtc, nameof(completedAtUtc));
        LastScanResultCategory = MaterialGuard.Code(
            resultCategory,
            MaterialFilePolicy.ScanResultCategoryMaxLength,
            nameof(resultCategory));
        ScannedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
        Status = targetStatus;
    }

    private void EnsureStatus(MaterialFileStatus requiredStatus)
    {
        if (Status != requiredStatus)
        {
            throw new InvalidOperationException(
                $"File must be in {requiredStatus} status for this operation.");
        }
    }

    private void EnsureTimestamp(DateTimeOffset value, string parameterName)
    {
        MaterialGuard.Utc(value, parameterName);
        if (value < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Lifecycle timestamp cannot precede the previous update.");
        }
    }

    private static void EnsureFormat(MaterialFileFormat format)
    {
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void EnsureSize(
        MaterialFileFormat format,
        long sizeBytes,
        string parameterName)
    {
        if (sizeBytes <= 0 || sizeBytes > MaterialFilePolicy.MaximumSizeBytes(format))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"File size must be within the limit for {format}.");
        }
    }

    private static string NormalizeFileName(string value, MaterialFileFormat format)
    {
        var normalized = MaterialGuard.RequiredText(
            value,
            MaterialFilePolicy.OriginalFileNameMaxLength,
            nameof(value));
        if (normalized.IndexOfAny(['/', '\\']) >= 0
            || normalized.Any(char.IsControl)
            || normalized is "." or ".."
            || !string.Equals(
                Path.GetExtension(normalized),
                MaterialFilePolicy.RequiredExtension(format),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Original filename must be a safe display name with the expected extension.",
                nameof(value));
        }

        return normalized;
    }

    private static string NormalizeSha256(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (normalized?.Length != MaterialFilePolicy.ChecksumMaxLength
            || normalized.Any(character =>
                !(character is >= 'A' and <= 'F' or >= '0' and <= '9')))
        {
            throw new ArgumentException(
                "SHA-256 checksum must contain exactly 64 hexadecimal characters.",
                nameof(value));
        }

        return normalized;
    }

    private static string CreateObjectKey(
        Material material,
        MaterialVersion version,
        Guid fileId) =>
        $"materials/{material.OwnerTeacherId:N}/{material.Id:N}/{version.Id:N}/{fileId:N}";
}
