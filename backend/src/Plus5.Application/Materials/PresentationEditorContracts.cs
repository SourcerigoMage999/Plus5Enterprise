namespace Plus5.Application.Materials;

public interface IPresentationEditorQuery
{
    Task<PresentationEditorWorkspace?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
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

public enum PresentationMutationOutcome
{
    Success,
    InvalidInput,
    NotFound,
    Conflict,
    SlideLimitReached,
    LastSlide,
}

public sealed record PresentationMutationResult(
    PresentationMutationOutcome Outcome,
    Guid? SlideId = null);

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

public sealed record PresentationSlideView(Guid Id, int SortOrder);
