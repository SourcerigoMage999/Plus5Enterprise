namespace Plus5.Domain.Teaching;

public sealed class KnowledgeArea
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private KnowledgeArea()
    {
    }

    public KnowledgeArea(
        Guid id,
        KnowledgeModel knowledgeModel,
        string name,
        int sortOrder,
        string? description = null)
    {
        EnsureIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        knowledgeModel.EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Id = id;
        KnowledgeModelId = knowledgeModel.Id;
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        Description = NormalizeOptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }

    public Guid KnowledgeModelId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public void UpdateDraft(
        KnowledgeModel knowledgeModel,
        string name,
        int sortOrder,
        string? description = null)
    {
        EnsureOwnedDraft(knowledgeModel);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        Description = NormalizeOptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        SortOrder = sortOrder;
    }

    private void EnsureOwnedDraft(KnowledgeModel knowledgeModel)
    {
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        if (knowledgeModel.Id != KnowledgeModelId)
        {
            throw new ArgumentException(
                "Knowledge area must be edited through its owning knowledge model.",
                nameof(knowledgeModel));
        }

        knowledgeModel.EnsureDraft();
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static string NormalizeRequiredText(
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

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
