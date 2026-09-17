namespace Plus5.Domain.Teaching;

public sealed class CurriculumOutcomeKnowledgeComponent
{
    private CurriculumOutcomeKnowledgeComponent()
    {
    }

    public CurriculumOutcomeKnowledgeComponent(
        CurriculumOutcome curriculumOutcome,
        KnowledgeComponent knowledgeComponent)
    {
        ArgumentNullException.ThrowIfNull(curriculumOutcome);
        ArgumentNullException.ThrowIfNull(knowledgeComponent);

        CurriculumOutcomeId = curriculumOutcome.Id;
        KnowledgeComponentId = knowledgeComponent.Id;
    }

    public Guid CurriculumOutcomeId { get; private set; }

    public Guid KnowledgeComponentId { get; private set; }
}
