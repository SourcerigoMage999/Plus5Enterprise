using Plus5.Domain.Teaching;

namespace Plus5.Domain.Evidence;

public sealed class EvidenceKnowledgeTarget
{
    public EvidenceKnowledgeTarget(
        KnowledgeComponent component,
        KnowledgeModel knowledgeModel,
        bool isLeaf)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(knowledgeModel);

        if (component.KnowledgeModelId != knowledgeModel.Id)
        {
            throw new ArgumentException(
                "The knowledge model must own the evidence target component.",
                nameof(knowledgeModel));
        }

        if (knowledgeModel.Status == KnowledgeModelStatus.Draft)
        {
            throw new ArgumentException(
                "Direct evidence cannot target a draft knowledge model.",
                nameof(knowledgeModel));
        }

        if (!isLeaf)
        {
            throw new ArgumentException(
                "Direct evidence can target only a leaf knowledge component.",
                nameof(isLeaf));
        }

        KnowledgeComponentId = component.Id;
    }

    public Guid KnowledgeComponentId { get; }
}
