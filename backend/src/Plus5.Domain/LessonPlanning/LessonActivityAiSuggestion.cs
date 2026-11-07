namespace Plus5.Domain.LessonPlanning;

public enum LessonActivityAiSuggestionStatus
{
    Pending = 1,
    Accepted = 2,
    Edited = 3,
    Rejected = 4,
    Replaced = 5,
    Expired = 6,
}

public sealed class LessonActivityAiSuggestion
{
    public const int ProviderMaxLength = 32;
    public const int ModelMaxLength = 64;
    public const int TemplateVersionMaxLength = 32;
    public const int MaximumContentBytes = 16 * 1024;

    private LessonActivityAiSuggestion() { }

    public LessonActivityAiSuggestion(
        Guid id,
        Guid teacherAccountId,
        Guid lessonPlanId,
        Guid lessonPlanVersionId,
        Guid lessonActivityId,
        byte[] basedOnRowVersion,
        string provider,
        string model,
        string promptTemplateVersion,
        string contentJson,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset expiresAtUtc,
        Guid? replacesSuggestionId = null)
    {
        if (id == Guid.Empty || teacherAccountId == Guid.Empty || lessonPlanId == Guid.Empty
            || lessonPlanVersionId == Guid.Empty || lessonActivityId == Guid.Empty)
            throw new ArgumentException("Identifiers are required.");
        if (basedOnRowVersion is not { Length: 8 }) throw new ArgumentException("Row version is invalid.", nameof(basedOnRowVersion));
        if (string.IsNullOrWhiteSpace(provider) || provider.Length > ProviderMaxLength) throw new ArgumentException("Provider is invalid.", nameof(provider));
        if (string.IsNullOrWhiteSpace(model) || model.Length > ModelMaxLength) throw new ArgumentException("Model is invalid.", nameof(model));
        if (string.IsNullOrWhiteSpace(promptTemplateVersion) || promptTemplateVersion.Length > TemplateVersionMaxLength) throw new ArgumentException("Template version is invalid.", nameof(promptTemplateVersion));
        if (string.IsNullOrWhiteSpace(contentJson) || System.Text.Encoding.UTF8.GetByteCount(contentJson) > MaximumContentBytes) throw new ArgumentException("Content is invalid.", nameof(contentJson));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, generatedAtUtc);

        Id = id;
        TeacherAccountId = teacherAccountId;
        LessonPlanId = lessonPlanId;
        LessonPlanVersionId = lessonPlanVersionId;
        LessonActivityId = lessonActivityId;
        BasedOnRowVersion = basedOnRowVersion.ToArray();
        Provider = provider.Trim();
        Model = model.Trim();
        PromptTemplateVersion = promptTemplateVersion.Trim();
        ContentJson = contentJson;
        GeneratedAtUtc = generatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ReplacesSuggestionId = replacesSuggestionId;
        Status = LessonActivityAiSuggestionStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid LessonPlanId { get; private set; }
    public Guid LessonPlanVersionId { get; private set; }
    public Guid LessonActivityId { get; private set; }
    public byte[] BasedOnRowVersion { get; private set; } = [];
    public LessonActivityAiSuggestionStatus Status { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string PromptTemplateVersion { get; private set; } = string.Empty;
    public string? ContentJson { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public Guid? ReplacesSuggestionId { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public void Resolve(LessonActivityAiSuggestionStatus status, DateTimeOffset resolvedAtUtc)
    {
        if (Status != LessonActivityAiSuggestionStatus.Pending
            || status is LessonActivityAiSuggestionStatus.Pending or LessonActivityAiSuggestionStatus.Expired)
            throw new InvalidOperationException("Only a pending suggestion can be resolved.");
        Status = status;
        ResolvedAtUtc = resolvedAtUtc;
        ContentJson = null;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status != LessonActivityAiSuggestionStatus.Pending || !IsExpired(now)) return;
        Status = LessonActivityAiSuggestionStatus.Expired;
        ResolvedAtUtc = now;
        ContentJson = null;
    }
}
