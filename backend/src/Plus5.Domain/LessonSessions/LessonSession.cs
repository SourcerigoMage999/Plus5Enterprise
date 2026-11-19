namespace Plus5.Domain.LessonSessions;

public sealed class LessonSession
{
    public const int TimeZoneIdMaxLength = 128;
    public const int VoidReasonMaxLength = 500;

    private LessonSession() { }

    public LessonSession(Guid id, Guid teacherAccountId, Guid lessonPlanId,
        Guid lessonPlanVersionId, Guid? calendarSessionId, Guid? studentId, Guid? groupId,
        DateTimeOffset startedAtUtc, DateTimeOffset? scheduledStartsAtUtc = null,
        DateTimeOffset? scheduledEndsAtUtc = null, string? scheduledTimeZoneId = null)
    {
        Required(id, nameof(id));
        Required(teacherAccountId, nameof(teacherAccountId));
        Required(lessonPlanId, nameof(lessonPlanId));
        Required(lessonPlanVersionId, nameof(lessonPlanVersionId));
        if (calendarSessionId == Guid.Empty) throw new ArgumentException("Calendar Session identifier is invalid.", nameof(calendarSessionId));
        if (studentId == Guid.Empty || groupId == Guid.Empty || studentId.HasValue == groupId.HasValue)
            throw new ArgumentException("Exactly one Student or Group participant is required.");
        Utc(startedAtUtc, nameof(startedAtUtc));
        var hasSchedule = scheduledStartsAtUtc.HasValue || scheduledEndsAtUtc.HasValue || scheduledTimeZoneId is not null;
        if (calendarSessionId.HasValue != hasSchedule
            || (calendarSessionId.HasValue && (!scheduledStartsAtUtc.HasValue || !scheduledEndsAtUtc.HasValue)))
            throw new ArgumentException("Calendar snapshot fields must be supplied together.");
        if (scheduledStartsAtUtc.HasValue)
        {
            Utc(scheduledStartsAtUtc.Value, nameof(scheduledStartsAtUtc));
            Utc(scheduledEndsAtUtc!.Value, nameof(scheduledEndsAtUtc));
            if (scheduledEndsAtUtc <= scheduledStartsAtUtc) throw new ArgumentOutOfRangeException(nameof(scheduledEndsAtUtc));
            scheduledTimeZoneId = RequiredText(scheduledTimeZoneId, TimeZoneIdMaxLength, nameof(scheduledTimeZoneId));
        }

        Id = id;
        TeacherAccountId = teacherAccountId;
        LessonPlanId = lessonPlanId;
        LessonPlanVersionId = lessonPlanVersionId;
        CalendarSessionId = calendarSessionId;
        StudentId = studentId;
        GroupId = groupId;
        Status = LessonSessionStatus.InProgress;
        ScheduledStartsAtUtc = scheduledStartsAtUtc;
        ScheduledEndsAtUtc = scheduledEndsAtUtc;
        ScheduledTimeZoneId = scheduledTimeZoneId;
        StartedAtUtc = startedAtUtc;
        LastCommittedAtUtc = startedAtUtc;
        Revision = 1;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid LessonPlanId { get; private set; }
    public Guid LessonPlanVersionId { get; private set; }
    public Guid? CalendarSessionId { get; private set; }
    public Guid? StudentId { get; private set; }
    public Guid? GroupId { get; private set; }
    public LessonSessionStatus Status { get; private set; }
    public DateTimeOffset? ScheduledStartsAtUtc { get; private set; }
    public DateTimeOffset? ScheduledEndsAtUtc { get; private set; }
    public string? ScheduledTimeZoneId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTimeOffset LastCommittedAtUtc { get; private set; }
    public long Revision { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void CommitMutation(long expectedRevision, DateTimeOffset committedAtUtc)
    {
        if (Status != LessonSessionStatus.InProgress)
            throw new InvalidOperationException("Only an in-progress Lesson Session accepts mutations.");
        if (expectedRevision != Revision)
            throw new InvalidOperationException("Lesson Session revision is stale.");
        Utc(committedAtUtc, nameof(committedAtUtc));
        ArgumentOutOfRangeException.ThrowIfLessThan(committedAtUtc, LastCommittedAtUtc);
        Revision = checked(Revision + 1);
        LastCommittedAtUtc = committedAtUtc;
    }

    private static void Required(Guid value, string name)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identifier is required.", name);
    }

    private static void Utc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", name);
    }

    private static string RequiredText(string? value, int maximum, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximum)
            throw new ArgumentException("A required bounded value is invalid.", name);
        return normalized;
    }
}

public enum LessonSessionStatus
{
    InProgress = 1,
    Completed = 2,
    Voided = 3,
}
