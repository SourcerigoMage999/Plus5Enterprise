namespace Plus5.Domain.LessonPlanning;

public sealed class LessonPlanVersionKnowledgeComponent
{
    private LessonPlanVersionKnowledgeComponent() { }

    public LessonPlanVersionKnowledgeComponent(Guid lessonPlanVersionId, Guid knowledgeComponentId)
    {
        if (lessonPlanVersionId == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(lessonPlanVersionId));
        if (knowledgeComponentId == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(knowledgeComponentId));
        LessonPlanVersionId = lessonPlanVersionId;
        KnowledgeComponentId = knowledgeComponentId;
    }

    public Guid LessonPlanVersionId { get; private set; }
    public Guid KnowledgeComponentId { get; private set; }
}
