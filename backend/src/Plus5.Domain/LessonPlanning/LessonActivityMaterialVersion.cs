namespace Plus5.Domain.LessonPlanning;

public sealed class LessonActivityMaterialVersion
{
    private LessonActivityMaterialVersion() { }

    public LessonActivityMaterialVersion(Guid lessonActivityId, Guid materialVersionId)
    {
        if (lessonActivityId == Guid.Empty)
            throw new ArgumentException("Identifier is required.", nameof(lessonActivityId));
        if (materialVersionId == Guid.Empty)
            throw new ArgumentException("Identifier is required.", nameof(materialVersionId));

        LessonActivityId = lessonActivityId;
        MaterialVersionId = materialVersionId;
    }

    public Guid LessonActivityId { get; private set; }

    public Guid MaterialVersionId { get; private set; }
}
