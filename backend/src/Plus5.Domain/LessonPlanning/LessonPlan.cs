namespace Plus5.Domain.LessonPlanning;

public sealed class LessonPlan
{
    private LessonPlan() { }

    public LessonPlan(
        Guid id,
        Guid teacherAccountId,
        Guid? studentId,
        Guid? groupId,
        Guid? sessionId,
        DateTimeOffset createdAtUtc)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureParticipant(studentId, groupId);
        if (sessionId == Guid.Empty) throw new ArgumentException("Session identifier cannot be empty.", nameof(sessionId));
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));

        Id = id;
        TeacherAccountId = teacherAccountId;
        StudentId = studentId;
        GroupId = groupId;
        SessionId = sessionId;
        LifecycleStatus = LessonPlanLifecycleStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid? StudentId { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid? SessionId { get; private set; }
    public LessonPlanLifecycleStatus LifecycleStatus { get; private set; }
    public Guid? CurrentReadyVersionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private static void EnsureParticipant(Guid? studentId, Guid? groupId)
    {
        if (studentId == Guid.Empty || groupId == Guid.Empty || studentId.HasValue == groupId.HasValue)
        {
            throw new ArgumentException("Exactly one Student or Group participant is required.");
        }
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identifier is required.", parameterName);
    }
}

public enum LessonPlanLifecycleStatus
{
    Active = 1,
    Archived = 2,
}
