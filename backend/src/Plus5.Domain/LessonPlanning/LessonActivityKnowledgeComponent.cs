namespace Plus5.Domain.LessonPlanning;

public sealed class LessonActivityKnowledgeComponent
{
    private LessonActivityKnowledgeComponent() { }
    public LessonActivityKnowledgeComponent(Guid lessonActivityId, Guid knowledgeComponentId)
    {
        if (lessonActivityId == Guid.Empty || knowledgeComponentId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        LessonActivityId = lessonActivityId;
        KnowledgeComponentId = knowledgeComponentId;
    }
    public Guid LessonActivityId { get; private set; }
    public Guid KnowledgeComponentId { get; private set; }
}
