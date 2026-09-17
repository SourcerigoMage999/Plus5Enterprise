namespace Plus5.Domain.Teaching;

public sealed class KnowledgeComponent
{
    public const int NameMaxLength = 300;
    public const int DescriptionMaxLength = 4000;

    private KnowledgeComponent()
    {
    }

    public KnowledgeComponent(
        Guid id,
        KnowledgeModel knowledgeModel,
        KnowledgeArea knowledgeArea,
        string name,
        int sortOrder,
        KnowledgeComponent? parentComponent = null,
        KnowledgeComponent? supersedesComponent = null,
        KnowledgeModel? supersededComponentModel = null,
        KnowledgeComponentStatus status = KnowledgeComponentStatus.Active,
        string? description = null)
    {
        EnsureIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        knowledgeModel.EnsureDraft();
        EnsureDefinedStatus(status);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        EnsureAreaAndParent(knowledgeModel, knowledgeArea, parentComponent, id);
        EnsureLineage(
            id,
            knowledgeModel,
            supersedesComponent,
            supersededComponentModel);

        Id = id;
        KnowledgeModelId = knowledgeModel.Id;
        KnowledgeAreaId = knowledgeArea.Id;
        ParentComponentId = parentComponent?.Id;
        SupersedesKnowledgeComponentId = supersedesComponent?.Id;
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        Description = NormalizeOptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        SortOrder = sortOrder;
        Status = status;
    }

    public Guid Id { get; private set; }

    public Guid KnowledgeModelId { get; private set; }

    public Guid KnowledgeAreaId { get; private set; }

    public Guid? ParentComponentId { get; private set; }

    public Guid? SupersedesKnowledgeComponentId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public KnowledgeComponentStatus Status { get; private set; }

    public void UpdateDraft(
        KnowledgeModel knowledgeModel,
        KnowledgeArea knowledgeArea,
        string name,
        int sortOrder,
        KnowledgeComponent? parentComponent = null,
        KnowledgeComponentStatus status = KnowledgeComponentStatus.Active,
        string? description = null)
    {
        EnsureOwnedDraft(knowledgeModel);
        EnsureDefinedStatus(status);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        EnsureAreaAndParent(knowledgeModel, knowledgeArea, parentComponent, Id);

        KnowledgeAreaId = knowledgeArea.Id;
        ParentComponentId = parentComponent?.Id;
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        Description = NormalizeOptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        SortOrder = sortOrder;
        Status = status;
    }

    private void EnsureOwnedDraft(KnowledgeModel knowledgeModel)
    {
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        if (knowledgeModel.Id != KnowledgeModelId)
        {
            throw new ArgumentException(
                "Knowledge component must be edited through its owning knowledge model.",
                nameof(knowledgeModel));
        }

        knowledgeModel.EnsureDraft();
    }

    private static void EnsureAreaAndParent(
        KnowledgeModel knowledgeModel,
        KnowledgeArea knowledgeArea,
        KnowledgeComponent? parentComponent,
        Guid componentId)
    {
        ArgumentNullException.ThrowIfNull(knowledgeArea);
        if (knowledgeArea.KnowledgeModelId != knowledgeModel.Id)
        {
            throw new ArgumentException(
                "Knowledge area must belong to the component knowledge model.",
                nameof(knowledgeArea));
        }

        if (parentComponent is null)
        {
            return;
        }

        if (parentComponent.Id == componentId)
        {
            throw new ArgumentException(
                "A knowledge component cannot be its own parent.",
                nameof(parentComponent));
        }

        if (parentComponent.KnowledgeModelId != knowledgeModel.Id
            || parentComponent.KnowledgeAreaId != knowledgeArea.Id)
        {
            throw new ArgumentException(
                "Parent component must belong to the same knowledge model and knowledge area.",
                nameof(parentComponent));
        }
    }

    private static void EnsureLineage(
        Guid id,
        KnowledgeModel knowledgeModel,
        KnowledgeComponent? supersedesComponent,
        KnowledgeModel? supersededComponentModel)
    {
        if (supersedesComponent is null)
        {
            if (supersededComponentModel is not null)
            {
                throw new ArgumentException(
                    "A predecessor knowledge model requires a superseded component.",
                    nameof(supersededComponentModel));
            }

            return;
        }

        ArgumentNullException.ThrowIfNull(supersededComponentModel);
        if (supersedesComponent.Id == id)
        {
            throw new ArgumentException(
                "A knowledge component cannot supersede itself.",
                nameof(supersedesComponent));
        }

        if (supersededComponentModel.Id != supersedesComponent.KnowledgeModelId)
        {
            throw new ArgumentException(
                "The predecessor knowledge model must own the superseded component.",
                nameof(supersededComponentModel));
        }

        if (!string.Equals(
                knowledgeModel.Code,
                supersededComponentModel.Code,
                StringComparison.Ordinal)
            || string.Equals(
                knowledgeModel.Version,
                supersededComponentModel.Version,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Component lineage must stay in the same knowledge model family and cross versions.",
                nameof(supersedesComponent));
        }
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsureDefinedStatus(KnowledgeComponentStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
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
