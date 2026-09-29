using Plus5.Domain.Teaching;

namespace Plus5.Domain.Materials;

public sealed class MaterialVersionKnowledgeComponent
{
    private MaterialVersionKnowledgeComponent()
    {
    }

    public MaterialVersionKnowledgeComponent(
        MaterialVersion materialVersion,
        KnowledgeComponent knowledgeComponent,
        KnowledgeModel knowledgeModel)
    {
        ArgumentNullException.ThrowIfNull(materialVersion);
        ArgumentNullException.ThrowIfNull(knowledgeComponent);
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        materialVersion.EnsureDraft();

        if (knowledgeComponent.KnowledgeModelId != knowledgeModel.Id)
        {
            throw new ArgumentException(
                "The knowledge model must own the mapped component.",
                nameof(knowledgeModel));
        }

        if (knowledgeModel.Status == KnowledgeModelStatus.Draft)
        {
            throw new InvalidOperationException(
                "Material metadata can reference only a published or retired knowledge model version.");
        }

        MaterialVersionId = materialVersion.Id;
        KnowledgeComponentId = knowledgeComponent.Id;
    }

    public Guid MaterialVersionId { get; private set; }

    public Guid KnowledgeComponentId { get; private set; }
}
