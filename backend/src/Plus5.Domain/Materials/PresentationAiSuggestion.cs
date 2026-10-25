using System.Text;

namespace Plus5.Domain.Materials;

public enum PresentationAiSuggestionStatus
{
    Pending = 1,
    Accepted = 2,
    Edited = 3,
    Rejected = 4,
    Expired = 5,
}

public sealed class PresentationAiSuggestion
{
    public const int KindMaxLength = 48;
    public const int ProviderMaxLength = 32;
    public const int ModelMaxLength = 64;
    public const int TemplateVersionMaxLength = 32;
    public const int MaximumContentBytes = 128 * 1024;

    private PresentationAiSuggestion()
    {
    }

    public PresentationAiSuggestion(
        Guid id,
        Guid teacherAccountId,
        Guid materialId,
        Guid materialVersionId,
        Guid presentationDocumentId,
        string suggestionKind,
        string provider,
        string model,
        string promptTemplateVersion,
        string generatedContentJson,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset expiresAtUtc,
        Guid? targetSlideId = null,
        Guid? targetElementId = null)
    {
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Identifier(teacherAccountId, nameof(teacherAccountId));
        MaterialGuard.Identifier(materialId, nameof(materialId));
        MaterialGuard.Identifier(materialVersionId, nameof(materialVersionId));
        MaterialGuard.Identifier(presentationDocumentId, nameof(presentationDocumentId));
        MaterialGuard.Utc(generatedAtUtc, nameof(generatedAtUtc));
        MaterialGuard.Utc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= generatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "AI suggestion expiry must follow generation time.");
        }

        Id = id;
        TeacherAccountId = teacherAccountId;
        MaterialId = materialId;
        MaterialVersionId = materialVersionId;
        PresentationDocumentId = presentationDocumentId;
        SuggestionKind = MaterialGuard.Code(
            suggestionKind,
            KindMaxLength,
            nameof(suggestionKind));
        Provider = MaterialGuard.Code(provider, ProviderMaxLength, nameof(provider));
        Model = MaterialGuard.Code(model, ModelMaxLength, nameof(model));
        PromptTemplateVersion = MaterialGuard.Code(
            promptTemplateVersion,
            TemplateVersionMaxLength,
            nameof(promptTemplateVersion));
        GeneratedContentJson = ValidateContent(generatedContentJson);
        GeneratedAtUtc = generatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        TargetSlideId = targetSlideId;
        TargetElementId = targetElementId;
        Status = PresentationAiSuggestionStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid MaterialId { get; private set; }
    public Guid MaterialVersionId { get; private set; }
    public Guid PresentationDocumentId { get; private set; }
    public Guid? TargetSlideId { get; private set; }
    public Guid? TargetElementId { get; private set; }
    public string SuggestionKind { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string PromptTemplateVersion { get; private set; } = string.Empty;
    public string? GeneratedContentJson { get; private set; }
    public PresentationAiSuggestionStatus Status { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        MaterialGuard.Utc(nowUtc, nameof(nowUtc));
        return Status == PresentationAiSuggestionStatus.Pending && nowUtc >= ExpiresAtUtc;
    }

    public void Resolve(PresentationAiSuggestionStatus status, DateTimeOffset resolvedAtUtc)
    {
        MaterialGuard.Utc(resolvedAtUtc, nameof(resolvedAtUtc));
        if (Status != PresentationAiSuggestionStatus.Pending
            || status == PresentationAiSuggestionStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending AI suggestion can be resolved.");
        }

        if (resolvedAtUtc < GeneratedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resolvedAtUtc),
                "AI suggestion resolution cannot precede generation.");
        }

        Status = status;
        ResolvedAtUtc = resolvedAtUtc;
        GeneratedContentJson = null;
    }

    private static string ValidateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (Encoding.UTF8.GetByteCount(content) > MaximumContentBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(content),
                "AI suggestion content exceeds the bounded size.");
        }

        return content;
    }
}
