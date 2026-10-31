namespace Plus5.Domain.LessonPlanning;

public sealed class LessonPlanVersion
{
    public const int GoalMaxLength = 300;

    private LessonPlanVersion() { }

    public LessonPlanVersion(
        Guid id,
        Guid lessonPlanId,
        int versionNumber,
        string goal,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(id));
        if (lessonPlanId == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(lessonPlanId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));

        Id = id;
        LessonPlanId = lessonPlanId;
        VersionNumber = versionNumber;
        Goal = NormalizeGoal(goal);
        Status = LessonPlanVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid LessonPlanId { get; private set; }
    public int VersionNumber { get; private set; }
    public LessonPlanVersionStatus Status { get; private set; }
    public string Goal { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private static string NormalizeGoal(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > GoalMaxLength)
        {
            throw new ArgumentException($"Goal is required and may contain at most {GoalMaxLength} characters.", nameof(value));
        }

        return normalized;
    }
}

public enum LessonPlanVersionStatus
{
    Draft = 1,
    Ready = 2,
    Superseded = 3,
}
