namespace Plus5.Domain.Teaching;

public sealed class KnowledgeModel
{
    public const int CodeMaxLength = 64;
    public const int VersionMaxLength = 64;

    private KnowledgeModel()
    {
    }

    public KnowledgeModel(Guid id, string code, string version)
    {
        EnsureIdentifier(id, nameof(id));

        Id = id;
        Code = NormalizeIdentifier(code, CodeMaxLength, nameof(code));
        Version = NormalizeIdentifier(version, VersionMaxLength, nameof(version));
        Status = KnowledgeModelStatus.Draft;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public KnowledgeModelStatus Status { get; private set; }

    public void Publish()
    {
        if (Status != KnowledgeModelStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft knowledge model can be published.");
        }

        Status = KnowledgeModelStatus.Published;
    }

    public void Retire()
    {
        if (Status != KnowledgeModelStatus.Published)
        {
            throw new InvalidOperationException("Only a published knowledge model can be retired.");
        }

        Status = KnowledgeModelStatus.Retired;
    }

    internal void EnsureDraft()
    {
        if (Status != KnowledgeModelStatus.Draft)
        {
            throw new InvalidOperationException(
                "Published or retired knowledge model semantics are immutable.");
        }
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static string NormalizeIdentifier(
        string value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value is required and may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized.ToUpperInvariant();
    }
}
