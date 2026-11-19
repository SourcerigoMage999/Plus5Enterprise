namespace Plus5.Domain.LessonSessions;

public sealed class LessonSessionOperation
{
    public const int RequestHashMaxLength = 64;
    private LessonSessionOperation() { }

    public LessonSessionOperation(Guid id, Guid teacherAccountId, Guid lessonSessionId,
        Guid operationId, LessonSessionOperationType operationType, string requestHash,
        long expectedRevision, long appliedRevision, DateTimeOffset committedAtUtc)
    {
        if (id == Guid.Empty || teacherAccountId == Guid.Empty || lessonSessionId == Guid.Empty
            || operationId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        if (!Enum.IsDefined(operationType)) throw new ArgumentOutOfRangeException(nameof(operationType));
        if (expectedRevision < 0 || appliedRevision <= 0 || appliedRevision < expectedRevision)
            throw new ArgumentOutOfRangeException(nameof(appliedRevision));
        var normalized = requestHash?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length != RequestHashMaxLength
            || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("Request hash must be a SHA-256 hex value.", nameof(requestHash));
        if (committedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(committedAtUtc));
        Id = id;
        TeacherAccountId = teacherAccountId;
        LessonSessionId = lessonSessionId;
        OperationId = operationId;
        OperationType = operationType;
        RequestHash = normalized;
        ExpectedRevision = expectedRevision;
        AppliedRevision = appliedRevision;
        CommittedAtUtc = committedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid LessonSessionId { get; private set; }
    public Guid OperationId { get; private set; }
    public LessonSessionOperationType OperationType { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public long ExpectedRevision { get; private set; }
    public long AppliedRevision { get; private set; }
    public DateTimeOffset CommittedAtUtc { get; private set; }
}

public enum LessonSessionOperationType { Start = 1, ReorderTimeline = 2 }

public sealed class LessonSessionAuditEvent
{
    public const int ActionMaxLength = 80;
    private LessonSessionAuditEvent() { }
    public LessonSessionAuditEvent(Guid id, Guid lessonSessionId, Guid teacherAccountId,
        Guid operationId, string action, LessonSessionStatus? oldStatus,
        LessonSessionStatus newStatus, DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty || lessonSessionId == Guid.Empty || teacherAccountId == Guid.Empty
            || operationId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        var normalized = action?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > ActionMaxLength)
            throw new ArgumentException("Action is required.", nameof(action));
        if (oldStatus.HasValue && !Enum.IsDefined(oldStatus.Value) || !Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        if (occurredAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(occurredAtUtc));
        Id = id;
        LessonSessionId = lessonSessionId;
        TeacherAccountId = teacherAccountId;
        OperationId = operationId;
        Action = normalized;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        OccurredAtUtc = occurredAtUtc;
    }
    public Guid Id { get; private set; }
    public Guid LessonSessionId { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public Guid OperationId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public LessonSessionStatus? OldStatus { get; private set; }
    public LessonSessionStatus NewStatus { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
