namespace Plus5.Domain.LessonPlanning;

public sealed class LessonActivity
{
    public const int NameMaxLength = 160;
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
    }

    public Guid Id { get; private set; }
    public Guid LessonPlanVersionId { get; private set; }
    public Guid? KnowledgeBlockVersionId { get; private set; }
    public int SortOrder { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public LessonActivityPurpose Purpose { get; private set; }
    public int DurationMinutes { get; private set; }
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
