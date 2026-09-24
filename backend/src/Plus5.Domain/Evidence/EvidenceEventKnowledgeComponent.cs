namespace Plus5.Domain.Evidence;

public sealed class EvidenceEventKnowledgeComponent
{
    private EvidenceEventKnowledgeComponent()
    {
    }

    internal EvidenceEventKnowledgeComponent(
        Guid evidenceEventId,
        Guid knowledgeComponentId)
    {
        if (evidenceEventId == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(evidenceEventId));
        }

        if (knowledgeComponentId == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(knowledgeComponentId));
        }

        EvidenceEventId = evidenceEventId;
        KnowledgeComponentId = knowledgeComponentId;
    }

    public Guid EvidenceEventId { get; private set; }

    public Guid KnowledgeComponentId { get; private set; }
}
