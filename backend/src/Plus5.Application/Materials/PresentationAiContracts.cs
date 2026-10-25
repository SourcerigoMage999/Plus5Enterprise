namespace Plus5.Application.Materials;

public static class PresentationAiSuggestionKinds
{
    public const string PresentationDraft = "PRESENTATION_DRAFT";
    public const string ElementContent = "ELEMENT_CONTENT";
    public const string QuickCheck = "QUICK_CHECK";
    public const string KnowledgeMapping = "KNOWLEDGE_MAPPING";

    public static bool IsSupported(string? value) => value is
        PresentationDraft or ElementContent or QuickCheck or KnowledgeMapping;
}

public interface IPresentationAiAssistant
{
    Task<PresentationAiProviderResult> GenerateAsync(
        PresentationAiProviderRequest request,
        CancellationToken cancellationToken);
}

public interface IPresentationAiSuggestionService
{
    Task<PresentationAiSuggestionResult> CreateAsync(
        Guid teacherAccountId,
        Guid materialId,
        CreatePresentationAiSuggestionCommand command,
        CancellationToken cancellationToken);

    Task<PresentationAiSuggestionView?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid suggestionId,
        CancellationToken cancellationToken);

    Task<PresentationAiSuggestionResult> ResolveAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid suggestionId,
        ResolvePresentationAiSuggestionCommand command,
        CancellationToken cancellationToken);
}

public sealed record CreatePresentationAiSuggestionCommand(
    string ExpectedDocumentRowVersion,
    string SuggestionKind,
    string? Instruction,
    Guid? TargetSlideId,
    Guid? TargetElementId);

public sealed record ResolvePresentationAiSuggestionCommand(
    string ExpectedDocumentRowVersion,
    string Decision,
    string? EditedContentJson);

public sealed record PresentationAiProviderRequest(
    string SuggestionKind,
    string Instruction,
    string ContextJson,
    string OutputJsonSchema);

public enum PresentationAiProviderOutcome
{
    Success,
    Disabled,
    Unavailable,
    InvalidOutput,
}

public sealed record PresentationAiProviderResult(
    PresentationAiProviderOutcome Outcome,
    string? ContentJson = null,
    string? Provider = null,
    string? Model = null,
    string? PromptTemplateVersion = null,
    int? InputTokens = null,
    int? OutputTokens = null);

public enum PresentationAiSuggestionOutcome
{
    Success,
    Disabled,
    InvalidInput,
    InvalidOutput,
    NotFound,
    Conflict,
    Expired,
    Unavailable,
}

public sealed record PresentationAiSuggestionResult(
    PresentationAiSuggestionOutcome Outcome,
    PresentationAiSuggestionView? Suggestion = null);

public sealed record PresentationAiSuggestionView(
    Guid Id,
    string SuggestionKind,
    string Status,
    string? ContentJson,
    string Provider,
    string Model,
    string PromptTemplateVersion,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    Guid? TargetSlideId,
    Guid? TargetElementId);

public sealed record PresentationAiStatusView(
    bool Enabled,
    bool PaidExternalService,
    bool AutomaticCalls,
    bool InAppBilling);
