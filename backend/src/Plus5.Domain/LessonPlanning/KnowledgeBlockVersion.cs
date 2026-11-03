namespace Plus5.Domain.LessonPlanning;

public sealed class KnowledgeBlockVersion
{
    public const int DisplayNameMaxLength = 120;

    private KnowledgeBlockVersion() { }

    public KnowledgeBlockVersion(Guid id, Guid knowledgeBlockId, int versionNumber,
        string displayName, DateTimeOffset createdAtUtc,
        KnowledgeBlockParticipantApplicability participantApplicability = KnowledgeBlockParticipantApplicability.Both,
        KnowledgeBlockHomeworkMode homeworkMode = KnowledgeBlockHomeworkMode.NotRelevant,
        KnowledgeBlockAssessmentCapability assessmentCapability = KnowledgeBlockAssessmentCapability.NotAssessment)
    {
        if (id == Guid.Empty || knowledgeBlockId == Guid.Empty) throw new ArgumentException("Identifier is required.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
        var normalized = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > DisplayNameMaxLength)
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));
        if (!Enum.IsDefined(participantApplicability) || !Enum.IsDefined(homeworkMode) || !Enum.IsDefined(assessmentCapability))
            throw new ArgumentOutOfRangeException(nameof(participantApplicability));
        Id = id;
        KnowledgeBlockId = knowledgeBlockId;
        VersionNumber = versionNumber;
        DisplayName = normalized;
        ParticipantApplicability = participantApplicability;
        HomeworkMode = homeworkMode;
        AssessmentCapability = assessmentCapability;
        Status = KnowledgeBlockVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid KnowledgeBlockId { get; private set; }
    public int VersionNumber { get; private set; }
    public KnowledgeBlockVersionStatus Status { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public int? MinDurationMinutes { get; private set; }
    public int? OptimalDurationMinutes { get; private set; }
    public int? MaxDurationMinutes { get; private set; }
    public KnowledgeBlockParticipantApplicability ParticipantApplicability { get; private set; }
    public KnowledgeBlockHomeworkMode HomeworkMode { get; private set; }
    public KnowledgeBlockAssessmentCapability AssessmentCapability { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public void Publish(int minimumMinutes, int optimalMinutes, int maximumMinutes, DateTimeOffset publishedAtUtc)
    {
        if (Status != KnowledgeBlockVersionStatus.Draft) throw new InvalidOperationException("Only Draft can be published.");
        if (minimumMinutes is < 1 or > 480 || optimalMinutes < minimumMinutes || maximumMinutes < optimalMinutes || maximumMinutes > 480)
            throw new ArgumentOutOfRangeException(nameof(minimumMinutes));
        if (publishedAtUtc.Offset != TimeSpan.Zero || publishedAtUtc < CreatedAtUtc)
            throw new ArgumentOutOfRangeException(nameof(publishedAtUtc));
        MinDurationMinutes = minimumMinutes;
        OptimalDurationMinutes = optimalMinutes;
        MaxDurationMinutes = maximumMinutes;
        PublishedAtUtc = publishedAtUtc;
        Status = KnowledgeBlockVersionStatus.Published;
    }
}

public enum KnowledgeBlockVersionStatus { Draft = 1, Published = 2, Retired = 3 }
public enum KnowledgeBlockParticipantApplicability { Individual = 1, Group = 2, Both = 3 }
public enum KnowledgeBlockHomeworkMode { NotRelevant = 1, Optional = 2, RequiresHomeworkContext = 3 }
public enum KnowledgeBlockAssessmentCapability { NotAssessment = 1, MayContainAssessment = 2, AssessmentFocused = 3 }
