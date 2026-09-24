namespace Plus5.Application.Evidence;

public sealed record EvidenceObservationCommand(
    Guid StudentId,
    string SourceKind,
    Guid SourceId,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyList<Guid> KnowledgeComponentIds);

public sealed record EvidenceCorrectionCommand(
    Guid SupersededEvidenceEventId,
    DateTimeOffset CorrectedOccurredAtUtc,
    string ReasonCode,
    IReadOnlyList<Guid> KnowledgeComponentIds);

public sealed record EvidenceInvalidationCommand(
    Guid SupersededEvidenceEventId,
    string ReasonCode);

public enum EvidenceWriteFailure
{
    None = 0,
    Invalid = 1,
    NotFound = 2,
    DuplicateSource = 3,
    Conflict = 4,
    InvalidKnowledgeTarget = 5,
}

public sealed record EvidenceWriteResult(
    Guid? EvidenceEventId,
    EvidenceWriteFailure Failure);

public interface IEvidenceEmissionService
{
    Task<EvidenceWriteResult> RecordObservationAsync(
        Guid owner,
        EvidenceObservationCommand command,
        CancellationToken cancellationToken);

    Task<EvidenceWriteResult> CorrectAsync(
        Guid owner,
        EvidenceCorrectionCommand command,
        CancellationToken cancellationToken);

    Task<EvidenceWriteResult> InvalidateAsync(
        Guid owner,
        EvidenceInvalidationCommand command,
        CancellationToken cancellationToken);
}
