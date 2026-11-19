using Plus5.Domain.LessonPlanning;

namespace Plus5.Domain.LessonSessions;

public sealed class LessonSessionActivity
{
    private LessonSessionActivity() { }

    public LessonSessionActivity(Guid id, Guid lessonSessionId, Guid? sourceLessonActivityId,
        LessonSessionActivityOrigin origin, int sortOrder, Guid? knowledgeBlockVersionId,
        string name, LessonActivityPurpose purpose, int durationMinutes,
        LessonActivityWorkMode workMode, string? description, string? purposeDescription,
        string? teacherInstructions, string? studentInstructions, string? notes,
        bool showTeacherInstructionsOnBoard, bool showStudentInstructionsOnBoard,
        bool showProgressOnBoard)
    {
        if (id == Guid.Empty || lessonSessionId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        if (sourceLessonActivityId == Guid.Empty || knowledgeBlockVersionId == Guid.Empty)
            throw new ArgumentException("Optional identifier is invalid.");
        if (!Enum.IsDefined(origin) || !Enum.IsDefined(purpose) || !Enum.IsDefined(workMode))
            throw new ArgumentOutOfRangeException(nameof(origin));
        if (origin == LessonSessionActivityOrigin.Planned != sourceLessonActivityId.HasValue)
            throw new ArgumentException("Planned runtime activity requires source activity; ad-hoc activity must not have one.");
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        if (durationMinutes is < 1 or > 480) throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        Id = id;
        LessonSessionId = lessonSessionId;
        SourceLessonActivityId = sourceLessonActivityId;
        Origin = origin;
        SortOrder = sortOrder;
        Status = LessonSessionActivityStatus.Pending;
        KnowledgeBlockVersionId = knowledgeBlockVersionId;
        Name = Required(name, LessonActivity.NameMaxLength, nameof(name));
        Purpose = purpose;
        DurationMinutes = durationMinutes;
        WorkMode = workMode;
        Description = Optional(description, LessonActivity.DescriptionMaxLength, nameof(description));
        PurposeDescription = Optional(purposeDescription, LessonActivity.PurposeDescriptionMaxLength, nameof(purposeDescription));
        TeacherInstructions = Optional(teacherInstructions, LessonActivity.InstructionsMaxLength, nameof(teacherInstructions));
        StudentInstructions = Optional(studentInstructions, LessonActivity.InstructionsMaxLength, nameof(studentInstructions));
        Notes = Optional(notes, LessonActivity.NotesMaxLength, nameof(notes));
        ShowTeacherInstructionsOnBoard = showTeacherInstructionsOnBoard;
        ShowStudentInstructionsOnBoard = showStudentInstructionsOnBoard;
        ShowProgressOnBoard = showProgressOnBoard;
    }

    public Guid Id { get; private set; }
    public Guid LessonSessionId { get; private set; }
    public Guid? SourceLessonActivityId { get; private set; }
    public LessonSessionActivityOrigin Origin { get; private set; }
    public int SortOrder { get; private set; }
    public LessonSessionActivityStatus Status { get; private set; }
    public Guid? KnowledgeBlockVersionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public LessonActivityPurpose Purpose { get; private set; }
    public int DurationMinutes { get; private set; }
    public LessonActivityWorkMode WorkMode { get; private set; }
    public string? Description { get; private set; }
    public string? PurposeDescription { get; private set; }
    public string? TeacherInstructions { get; private set; }
    public string? StudentInstructions { get; private set; }
    public string? Notes { get; private set; }
    public bool ShowTeacherInstructionsOnBoard { get; private set; }
    public bool ShowStudentInstructionsOnBoard { get; private set; }
    public bool ShowProgressOnBoard { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void MoveTo(int sortOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        SortOrder = sortOrder;
    }

    private static string Required(string? value, int maximum, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximum)
            throw new ArgumentException("A required bounded value is invalid.", name);
        return normalized;
    }

    private static string? Optional(string? value, int maximum, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maximum) throw new ArgumentOutOfRangeException(name);
        return normalized;
    }
}

public enum LessonSessionActivityOrigin { Planned = 1, AdHoc = 2 }
public enum LessonSessionActivityStatus
{
    Pending = 1,
    InProgress = 2,
    Paused = 3,
    Completed = 4,
    Partial = 5,
    Skipped = 6,
}
