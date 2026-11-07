namespace Plus5.Domain.LessonPlanning;

public sealed class LessonActivity
{
    public const int NameMaxLength = 160;
    public const int DescriptionMaxLength = 300;
    public const int PurposeDescriptionMaxLength = 300;
    public const int InstructionsMaxLength = 1000;
    public const int NotesMaxLength = 500;
    private LessonActivity() { }

    public LessonActivity(Guid id, Guid lessonPlanVersionId, Guid? knowledgeBlockVersionId,
        int sortOrder, string name, LessonActivityPurpose purpose, int durationMinutes)
    {
        if (id == Guid.Empty || lessonPlanVersionId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        if (knowledgeBlockVersionId == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(knowledgeBlockVersionId));
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        var normalized = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > NameMaxLength) throw new ArgumentException("Name is required.", nameof(name));
        if (!Enum.IsDefined(purpose)) throw new ArgumentOutOfRangeException(nameof(purpose));
        if (durationMinutes is < 1 or > 480) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        Id = id;
        LessonPlanVersionId = lessonPlanVersionId;
        KnowledgeBlockVersionId = knowledgeBlockVersionId;
        SortOrder = sortOrder;
        Name = normalized;
        Purpose = purpose;
        DurationMinutes = durationMinutes;
        WorkMode = LessonActivityWorkMode.Individual;
        ShowTeacherInstructionsOnBoard = true;
        ShowStudentInstructionsOnBoard = true;
    }

    public Guid Id { get; private set; }
    public Guid LessonPlanVersionId { get; private set; }
    public Guid? KnowledgeBlockVersionId { get; private set; }
    public int SortOrder { get; private set; }
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

    public void UpdateCore(
        string name,
        LessonActivityPurpose purpose,
        int durationMinutes,
        LessonActivityWorkMode workMode,
        string? description,
        string? purposeDescription,
        string? teacherInstructions,
        string? studentInstructions,
        string? notes,
        bool showTeacherInstructionsOnBoard,
        bool showStudentInstructionsOnBoard,
        bool showProgressOnBoard)
    {
        var normalizedName = Required(name, NameMaxLength, nameof(name));
        if (!Enum.IsDefined(purpose)) throw new ArgumentOutOfRangeException(nameof(purpose));
        if (durationMinutes is < 1 or > 480) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (!Enum.IsDefined(workMode)) throw new ArgumentOutOfRangeException(nameof(workMode));

        Name = normalizedName;
        Purpose = purpose;
        DurationMinutes = durationMinutes;
        WorkMode = workMode;
        Description = Optional(description, DescriptionMaxLength, nameof(description));
        PurposeDescription = Optional(purposeDescription, PurposeDescriptionMaxLength, nameof(purposeDescription));
        TeacherInstructions = Optional(teacherInstructions, InstructionsMaxLength, nameof(teacherInstructions));
        StudentInstructions = Optional(studentInstructions, InstructionsMaxLength, nameof(studentInstructions));
        Notes = Optional(notes, NotesMaxLength, nameof(notes));
        ShowTeacherInstructionsOnBoard = showTeacherInstructionsOnBoard;
        ShowStudentInstructionsOnBoard = showStudentInstructionsOnBoard;
        ShowProgressOnBoard = showProgressOnBoard;
    }

    public void ApplyAiContent(
        string name,
        string? purposeDescription,
        string? description,
        string? teacherInstructions,
        string? studentInstructions,
        LessonActivityWorkMode workMode) => UpdateCore(
            name,
            Purpose,
            DurationMinutes,
            workMode,
            description,
            purposeDescription,
            teacherInstructions,
            studentInstructions,
            Notes,
            ShowTeacherInstructionsOnBoard,
            ShowStudentInstructionsOnBoard,
            ShowProgressOnBoard);

    public void ChangeSortOrder(int sortOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        SortOrder = sortOrder;
    }

    public LessonActivity Duplicate(Guid id, int sortOrder)
    {
        var copy = new LessonActivity(id, LessonPlanVersionId, KnowledgeBlockVersionId,
            sortOrder, Name, Purpose, DurationMinutes);
        copy.UpdateCore(Name, Purpose, DurationMinutes, WorkMode, Description,
            PurposeDescription, TeacherInstructions, StudentInstructions, Notes,
            ShowTeacherInstructionsOnBoard, ShowStudentInstructionsOnBoard,
            ShowProgressOnBoard);
        return copy;
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

public enum LessonActivityPurpose
{
    Engagement = 1,
    ActivatePriorKnowledge = 2,
    Instruction = 3,
    Practice = 4,
    CheckForUnderstanding = 5,
    Other = 6,
}

public enum LessonActivityWorkMode
{
    Individual = 1,
    Pair = 2,
    Group = 3,
}
