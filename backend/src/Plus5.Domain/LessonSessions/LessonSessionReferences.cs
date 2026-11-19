namespace Plus5.Domain.LessonSessions;

public sealed class LessonSessionActivityMaterialVersion
{
    private LessonSessionActivityMaterialVersion() { }
    public LessonSessionActivityMaterialVersion(Guid lessonSessionActivityId, Guid materialVersionId)
    {
        if (lessonSessionActivityId == Guid.Empty || materialVersionId == Guid.Empty)
            throw new ArgumentException("Identifier is required.");
        LessonSessionActivityId = lessonSessionActivityId;
        MaterialVersionId = materialVersionId;
    }
    public Guid LessonSessionActivityId { get; private set; }
    public Guid MaterialVersionId { get; private set; }
}

public sealed class LessonSessionActivityAssessableTaskVersion
{
    private LessonSessionActivityAssessableTaskVersion() { }
    public LessonSessionActivityAssessableTaskVersion(Guid lessonSessionActivityId,
        Guid materialVersionId, Guid assessableTaskVersionId)
    {
        if (lessonSessionActivityId == Guid.Empty || materialVersionId == Guid.Empty
            || assessableTaskVersionId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        LessonSessionActivityId = lessonSessionActivityId;
        MaterialVersionId = materialVersionId;
        AssessableTaskVersionId = assessableTaskVersionId;
    }
    public Guid LessonSessionActivityId { get; private set; }
    public Guid MaterialVersionId { get; private set; }
    public Guid AssessableTaskVersionId { get; private set; }
}
