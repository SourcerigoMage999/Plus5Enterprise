namespace Plus5.Domain.LessonPlanning;

public sealed class LessonPlanVersion
{
    public const int GoalMaxLength = 300;
    public const int DefaultTargetDurationMinutes = 60;
    public const int MinimumTargetDurationMinutes = 5;
    public const int MaximumTargetDurationMinutes = 480;

    private LessonPlanVersion() { }

    public LessonPlanVersion(
        Guid id,
        Guid lessonPlanId,
        int versionNumber,
        string goal,
        DateTimeOffset createdAtUtc,
        int targetDurationMinutes = DefaultTargetDurationMinutes)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(id));
        if (lessonPlanId == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(lessonPlanId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));

        Id = id;
        LessonPlanId = lessonPlanId;
        VersionNumber = versionNumber;
        Goal = NormalizeGoal(goal);
        TargetDurationMinutes = ValidateTargetDuration(targetDurationMinutes);
        Status = LessonPlanVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid LessonPlanId { get; private set; }
    public int VersionNumber { get; private set; }
    public LessonPlanVersionStatus Status { get; private set; }
    public string Goal { get; private set; } = string.Empty;
    public int TargetDurationMinutes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void ChangeTargetDuration(int targetDurationMinutes, DateTimeOffset updatedAtUtc)
    {
        EnsureDraft(updatedAtUtc);
        TargetDurationMinutes = ValidateTargetDuration(targetDurationMinutes);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RecordAuthoringChange(DateTimeOffset updatedAtUtc)
    {
        EnsureDraft(updatedAtUtc);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void MarkReady()
    {
        if (Status != LessonPlanVersionStatus.Draft)
            throw new InvalidOperationException("Only a Draft lesson plan version can become Ready.");
        Status = LessonPlanVersionStatus.Ready;
    }

    public void MarkSuperseded()
    {
        if (Status != LessonPlanVersionStatus.Ready)
            throw new InvalidOperationException("Only a Ready lesson plan version can become Superseded.");
        Status = LessonPlanVersionStatus.Superseded;
    }

    private void EnsureDraft(DateTimeOffset updatedAtUtc)
    {
        if (Status != LessonPlanVersionStatus.Draft)
            throw new InvalidOperationException("Only a Draft lesson plan version can be changed.");
        if (updatedAtUtc.Offset != TimeSpan.Zero || updatedAtUtc < UpdatedAtUtc)
            throw new ArgumentOutOfRangeException(nameof(updatedAtUtc));
    }

    private static int ValidateTargetDuration(int value)
    {
        if (value is < MinimumTargetDurationMinutes or > MaximumTargetDurationMinutes)
            throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }

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
