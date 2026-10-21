using Plus5.Domain.Evidence;

namespace Plus5.Application.Materials;

public interface IPresentationEditorQuery
{
    Task<PresentationEditorWorkspace?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken);

    Task<PresentationKnowledgeComponentPage?> SearchKnowledgeComponentsAsync(
        Guid teacherAccountId,
        Guid materialId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IPresentationSlideService
{
    Task<PresentationMutationResult> InitializeAsync(
        Guid teacherAccountId,
        Guid materialId,
        InitializePresentationCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> AddAsync(
        Guid teacherAccountId,
        Guid materialId,
        AddPresentationSlideCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> DeleteAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> DuplicateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> ReorderAsync(
        Guid teacherAccountId,
        Guid materialId,
        ReorderPresentationSlidesCommand command,
        CancellationToken cancellationToken);
}

public interface IPresentationElementService
{
    Task<PresentationAssetContent?> OpenAssetAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid assetId,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> CreateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        CreatePresentationElementCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> UploadImageAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        UploadPresentationImageCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> UploadMediaAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        UploadPresentationMediaCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> CreateQuickCheckAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        UpsertPresentationQuickCheckCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> UpdateQuickCheckAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        Guid elementId,
        UpsertPresentationQuickCheckCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> UpdateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        Guid elementId,
        UpdatePresentationElementCommand command,
        CancellationToken cancellationToken);

    Task<PresentationMutationResult> DeleteAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        Guid elementId,
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken);
}

public sealed record PresentationAssetContent(
    Stream Content,
    string MediaType,
    string FileName);

public sealed record InitializePresentationCommand(
    Guid MaterialVersionId,
    string ExpectedMaterialRowVersion);

public sealed record AddPresentationSlideCommand(
    string ExpectedDocumentRowVersion,
    Guid? AfterSlideId);

public sealed record PresentationConcurrencyCommand(string ExpectedDocumentRowVersion);

public sealed record ReorderPresentationSlidesCommand(
    string ExpectedDocumentRowVersion,
    IReadOnlyList<Guid> OrderedSlideIds);

public sealed record CreatePresentationElementCommand(
    string ExpectedDocumentRowVersion,
    string ElementTypeCode,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string PayloadJson);

public sealed record UpdatePresentationElementCommand(
    string ExpectedDocumentRowVersion,
    int ZIndex,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string PayloadJson);

public sealed record UploadPresentationImageCommand(
    string ExpectedDocumentRowVersion,
    string OriginalFileName,
    string DeclaredMediaType,
    long DeclaredSizeBytes,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string AltText,
    string Fit,
    Func<Stream> OpenReadStream);

public sealed record UploadPresentationMediaCommand(
    string ExpectedDocumentRowVersion,
    string ElementTypeCode,
    string OriginalFileName,
    string DeclaredMediaType,
    long DeclaredSizeBytes,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string PayloadJson,
    Func<Stream> OpenReadStream);

public sealed record UpsertPresentationQuickCheckCommand(
    string ExpectedDocumentRowVersion,
    int ZIndex,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    int Difficulty,
    EvidenceType EvidenceType,
    IReadOnlyList<Guid> KnowledgeComponentIds);

public enum PresentationMutationOutcome
{
    Success,
    InvalidInput,
    InvalidKnowledgeTarget,
    NotFound,
    Conflict,
    SlideLimitReached,
    LastSlide,
    ElementLimitReached,
    PayloadLimitReached,
    InvalidAsset,
    MalwareDetected,
    ScannerUnavailable,
    StorageFailure,
}

public sealed record PresentationMutationResult(
    PresentationMutationOutcome Outcome,
    Guid? SlideId = null,
    Guid? ElementId = null);

public sealed record PresentationEditorWorkspace(
    Guid MaterialId,
    Guid MaterialVersionId,
    int MaterialVersionNumber,
    string Title,
    string MaterialRowVersion,
    PresentationDocumentView? Document);

public sealed record PresentationDocumentView(
    int DocumentSchemaVersion,
    int CanvasWidth,
    int CanvasHeight,
    string RowVersion,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PresentationSlideView> Slides);

public sealed record PresentationSlideView(
    Guid Id,
    int SortOrder,
    IReadOnlyList<PresentationElementView> Elements);

public sealed record PresentationElementView(
    Guid Id,
    string ElementTypeCode,
    int ElementSchemaVersion,
    int ZIndex,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string PayloadJson,
    PresentationAssetView? Asset,
    PresentationQuickCheckView? QuickCheck);

public sealed record PresentationAssetView(
    Guid Id,
    string AssetKindCode,
    string OriginalFileName,
    string MediaType,
    long SizeBytes,
    string ReadPath);

public sealed record PresentationQuickCheckView(
    Guid AssessableTaskVersionId,
    string Prompt,
    IReadOnlyList<QuickCheckOptionView> Options,
    string CorrectOptionKey,
    int Difficulty,
    EvidenceType EvidenceType,
    decimal MaxPoints,
    bool KnowledgeMappingPending,
    IReadOnlyList<PresentationKnowledgeComponentView> KnowledgeComponents);

public sealed record QuickCheckOptionView(string Key, string Text);

public sealed record PresentationKnowledgeComponentView(
    Guid Id,
    string Name,
    string KnowledgeAreaName,
    string KnowledgeModelCode,
    string KnowledgeModelVersion,
    string KnowledgeModelStatus,
    string KnowledgeComponentStatus);

public sealed record PresentationKnowledgeComponentPage(
    IReadOnlyList<PresentationKnowledgeComponentView> Items,
    int Page,
    int PageSize,
    int TotalCount);
