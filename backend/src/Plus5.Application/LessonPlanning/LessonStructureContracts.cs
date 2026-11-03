namespace Plus5.Application.LessonPlanning;

public enum LessonBalanceStatus { Satisfied = 1, Warning = 2, NotEvaluated = 3 }

public sealed record LessonStructureKnowledge(Guid Id, string Name, string ModelCode, string ModelVersion);
public sealed record LessonStructureActivity(Guid Id, int SortOrder, string Name, string Purpose,
    int DurationMinutes, string? KnowledgeBlockCode, string? KnowledgeBlockName,
    IReadOnlyList<Guid> KnowledgeComponentIds, IReadOnlyList<string> WarningCodes);
public sealed record LessonBalanceArea(string Code, string Label, LessonBalanceStatus Status, string Message);
public sealed record LessonStructureView(Guid LessonPlanId, Guid LessonPlanVersionId, string RowVersion,
    string ParticipantName, string ParticipantType, string Goal, int TargetDurationMinutes,
    IReadOnlyList<LessonStructureKnowledge> KnowledgeComponents,
    IReadOnlyList<LessonStructureActivity> Activities, IReadOnlyList<LessonBalanceArea> Balance,
    int MaterialCandidateCount, bool IsApplied, bool CanApply, string PolicyCode);

public enum LessonStructureFailure
{
    None,
    Invalid,
    NotFound,
    NotDraft,
    AlreadyApplied,
    KnowledgeBlockUnavailable,
    Conflict,
}

public sealed record LessonStructureResult(LessonStructureView? View, LessonStructureFailure Failure);

public interface ILessonStructureService
{
    Task<LessonStructureResult> GetAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken);
    Task<LessonStructureResult> ChangeTargetDurationAsync(Guid teacherId, Guid lessonPlanId,
        int targetDurationMinutes, string rowVersion, CancellationToken cancellationToken);
    Task<LessonStructureResult> ApplyBaselineAsync(Guid teacherId, Guid lessonPlanId,
        string rowVersion, CancellationToken cancellationToken);
}
