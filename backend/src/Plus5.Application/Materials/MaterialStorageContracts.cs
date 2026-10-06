using Plus5.Domain.Materials;

namespace Plus5.Application.Materials;

public interface IMaterialObjectStorage
{
    Task PutQuarantineAsync(
        MaterialObjectWrite request,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream> OpenQuarantineReadAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken);

    Task PromoteCleanAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken);

    Task<Uri> CreateReadAccessAsync(
        MaterialObjectReference reference,
        TimeSpan lifetime,
        CancellationToken cancellationToken);
}

public interface IMaterialFileValidator
{
    Task<MaterialFileValidationResult> ValidateAsync(
        MaterialFileValidationRequest request,
        Stream content,
        CancellationToken cancellationToken);
}

public interface IMalwareScanner
{
    Task<MalwareScanResult> ScanAsync(
        Stream content,
        CancellationToken cancellationToken);
}

public interface IMaterialAnalysisService
{
    Task<MaterialAnalysisResult> AnalyzeAsync(
        MaterialAnalysisRequest request,
        Stream cleanContent,
        CancellationToken cancellationToken);
}

public sealed record MaterialObjectReference(
    string StorageProvider,
    string StorageContainer,
    string ObjectKey);

public sealed record MaterialObjectWrite(
    MaterialObjectReference Reference,
    long ContentLength,
    string MediaType,
    string Sha256Checksum);

public sealed record MaterialFileValidationRequest(
    MaterialFileFormat Format,
    string OriginalFileName,
    string DeclaredMediaType,
    long DeclaredSizeBytes);

public sealed record MaterialFileValidationResult(
    bool IsValid,
    string ResultCategory,
    long ActualSizeBytes,
    string? Sha256Checksum);

public sealed record MalwareScanResult(bool IsClean, bool IsMalware, string ResultCategory);

public sealed record MaterialAnalysisRequest(Guid MaterialVersionId, string Purpose);

public sealed record MaterialAnalysisResult(
    bool IsAvailable,
    string? Provider,
    string? Model,
    DateTimeOffset? GeneratedAtUtc,
    IReadOnlyList<MaterialAnalysisSuggestion> Suggestions);

public sealed record MaterialAnalysisSuggestion(string Kind, string SuggestedValue);
