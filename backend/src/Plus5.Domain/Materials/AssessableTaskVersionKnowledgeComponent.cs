using Plus5.Domain.Teaching;

namespace Plus5.Domain.Materials;

public sealed class AssessableTaskVersionKnowledgeComponent
{
    private AssessableTaskVersionKnowledgeComponent()
    {
    }

    public AssessableTaskVersionKnowledgeComponent(
        AssessableTaskVersion taskVersion,
        MaterialVersion materialVersion,
        KnowledgeComponent knowledgeComponent,
        KnowledgeModel knowledgeModel,
        bool isLeaf)
    {
        ArgumentNullException.ThrowIfNull(taskVersion);
        ArgumentNullException.ThrowIfNull(materialVersion);
        ArgumentNullException.ThrowIfNull(knowledgeComponent);
        ArgumentNullException.ThrowIfNull(knowledgeModel);
        materialVersion.EnsureDraft();

        if (taskVersion.MaterialVersionId != materialVersion.Id)
        {
            throw new ArgumentException(
                "The task version must belong to the supplied material version.",
                nameof(materialVersion));
        }

        if (knowledgeComponent.KnowledgeModelId != knowledgeModel.Id)
        {
            throw new ArgumentException(
                "The knowledge model must own the mapped component.",
                nameof(knowledgeModel));
        }

        if (knowledgeModel.Status == KnowledgeModelStatus.Draft)
        {
            throw new InvalidOperationException(
                "Assessable task metadata can reference only a published or retired knowledge model version.");
        }

        if (!isLeaf)
        {
            throw new ArgumentException(
                "An assessable task can target only a leaf knowledge component.",
                nameof(isLeaf));
        }

        AssessableTaskVersionId = taskVersion.Id;
        KnowledgeComponentId = knowledgeComponent.Id;
    }

    public Guid AssessableTaskVersionId { get; private set; }

    public Guid KnowledgeComponentId { get; private set; }
}
