namespace Plus5.Domain.Evidence;

public sealed class EvidenceEvent
{
    public const int SourceKindMaxLength = 64;
    public const int ReasonCodeMaxLength = 64;

    private readonly List<EvidenceEventKnowledgeComponent> knowledgeComponents = [];

    private EvidenceEvent()
    {
    }

    private EvidenceEvent(
        Guid id,
        Guid studentId,
        EvidenceEventKind kind,
        string sourceKind,
        Guid sourceId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset recordedAtUtc,
        Guid? supersedesEvidenceEventId,
        string? reasonCode,
        decimal? performanceScore,
        EvidenceMetadata? metadata)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(studentId, nameof(studentId));
        EnsureIdentifier(sourceId, nameof(sourceId));
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureUtc(recordedAtUtc, nameof(recordedAtUtc));

        Id = id;
        StudentId = studentId;
        Kind = kind;
        SourceKind = NormalizeCode(sourceKind, SourceKindMaxLength, nameof(sourceKind));
        SourceId = sourceId;
        OccurredAtUtc = occurredAtUtc;
        RecordedAtUtc = recordedAtUtc;
        SupersedesEvidenceEventId = supersedesEvidenceEventId;
        ReasonCode = reasonCode;
        PerformanceScore = performanceScore;
        Difficulty = metadata?.Difficulty;
        EvidenceType = metadata?.EvidenceType;
        AssistanceLevel = metadata?.AssistanceLevel;
        EvidenceContext = metadata?.EvidenceContext;
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public EvidenceEventKind Kind { get; private set; }

    public string SourceKind { get; private set; } = string.Empty;

    public Guid SourceId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public Guid? SupersedesEvidenceEventId { get; private set; }

    public string? ReasonCode { get; private set; }

    public decimal? PerformanceScore { get; private set; }

    public int? Difficulty { get; private set; }

    public EvidenceType? EvidenceType { get; private set; }

    public AssistanceLevel? AssistanceLevel { get; private set; }

    public EvidenceContext? EvidenceContext { get; private set; }

    public IReadOnlyCollection<EvidenceEventKnowledgeComponent> KnowledgeComponents =>
        knowledgeComponents;

    public static EvidenceEvent CreateObservation(
        Guid id,
        Guid studentId,
        string sourceKind,
        Guid sourceId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset recordedAtUtc,
        EvidenceMetadata metadata,
        decimal performanceScore,
        IReadOnlyCollection<EvidenceKnowledgeTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        EnsurePerformanceScore(performanceScore);
        var evidenceEvent = new EvidenceEvent(
            id,
            studentId,
            EvidenceEventKind.Observation,
            sourceKind,
            sourceId,
            occurredAtUtc,
            recordedAtUtc,
            supersedesEvidenceEventId: null,
            reasonCode: null,
            performanceScore,
            metadata);
        evidenceEvent.SetTargets(targets);
        return evidenceEvent;
    }

    public static EvidenceEvent CreateCorrection(
        Guid id,
        EvidenceEvent predecessor,
        DateTimeOffset correctedOccurredAtUtc,
        DateTimeOffset recordedAtUtc,
        string reasonCode,
        EvidenceMetadata metadata,
        decimal performanceScore,
        IReadOnlyCollection<EvidenceKnowledgeTarget> targets)
    {
        EnsureCanSupersede(id, predecessor);
        ArgumentNullException.ThrowIfNull(metadata);
        EnsurePerformanceScore(performanceScore);

        var evidenceEvent = new EvidenceEvent(
            id,
            predecessor.StudentId,
            EvidenceEventKind.Correction,
            predecessor.SourceKind,
            predecessor.SourceId,
            correctedOccurredAtUtc,
            recordedAtUtc,
            predecessor.Id,
            NormalizeCode(reasonCode, ReasonCodeMaxLength, nameof(reasonCode)),
            performanceScore,
            metadata);
        evidenceEvent.SetTargets(targets);
        return evidenceEvent;
    }

    public static EvidenceEvent CreateInvalidation(
        Guid id,
        EvidenceEvent predecessor,
        DateTimeOffset recordedAtUtc,
        string reasonCode)
    {
        EnsureCanSupersede(id, predecessor);

        return new EvidenceEvent(
            id,
            predecessor.StudentId,
            EvidenceEventKind.Invalidation,
            predecessor.SourceKind,
            predecessor.SourceId,
            predecessor.OccurredAtUtc,
            recordedAtUtc,
            predecessor.Id,
            NormalizeCode(reasonCode, ReasonCodeMaxLength, nameof(reasonCode)),
            performanceScore: null,
            metadata: null);
    }

    private void SetTargets(IReadOnlyCollection<EvidenceKnowledgeTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0)
        {
            throw new ArgumentException(
                "Observation and correction evidence require at least one knowledge target.",
                nameof(targets));
        }

        var componentIds = targets.Select(target =>
        {
            ArgumentNullException.ThrowIfNull(target);
            return target.KnowledgeComponentId;
        }).ToArray();
        if (componentIds.Distinct().Count() != componentIds.Length)
        {
            throw new ArgumentException(
                "An evidence event cannot target the same knowledge component twice.",
                nameof(targets));
        }

        knowledgeComponents.AddRange(componentIds.Select(componentId =>
            new EvidenceEventKnowledgeComponent(Id, componentId)));
    }

    private static void EnsureCanSupersede(Guid id, EvidenceEvent predecessor)
    {
        EnsureIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(predecessor);
        if (id == predecessor.Id)
        {
            throw new ArgumentException(
                "An evidence event cannot supersede itself.",
                nameof(id));
        }

        if (predecessor.Kind == EvidenceEventKind.Invalidation)
        {
            throw new InvalidOperationException(
                "Invalidation is terminal and cannot be superseded.");
        }
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsurePerformanceScore(decimal value)
    {
        if (value is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Performance score must be between 0 and 1.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }

    private static string NormalizeCode(
        string value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Code is required and may contain at most {maxLength} characters.",
                parameterName);
        }

        if (!normalized.All(character =>
                character is >= 'A' and <= 'Z'
                    or >= '0' and <= '9'
                    or '_'
                    or '-'
                    or '.'))
        {
            throw new ArgumentException(
                "Code may contain only A-Z, 0-9, underscore, hyphen, or period.",
                parameterName);
        }

        return normalized;
    }
}
