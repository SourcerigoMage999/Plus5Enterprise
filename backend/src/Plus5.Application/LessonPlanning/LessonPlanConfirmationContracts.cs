namespace Plus5.Application.LessonPlanning;

public sealed record LessonPlanConfirmationActivity(
    Guid Id,
    int SortOrder,
    string Name,
    string Purpose,
    string WorkMode,
    int DurationMinutes,
    int AssessableTaskCount,
    IReadOnlyList<LessonActivityMaterialAttachment> Materials);

public sealed record LessonPlanConfirmationView(
    Guid LessonPlanId,
    Guid LessonPlanVersionId,
    int VersionNumber,
    string Status,
    string PlanRowVersion,
    string VersionRowVersion,
    string ParticipantName,
    string ParticipantType,
    LessonPlanEditorSession? Session,
    string Goal,
    int TargetDurationMinutes,
    int PlannedDurationMinutes,
    IReadOnlyList<LessonStructureKnowledge> KnowledgeComponents,
    IReadOnlyList<LessonPlanConfirmationActivity> Activities,
    IReadOnlyList<LessonActivityMaterialAttachment> Materials,
    IReadOnlyList<LessonBalanceArea> Balance,
    IReadOnlyList<string> ValidationMessages,
    bool CanConfirm,
    bool CanEdit,
    bool CanStartBoard);

public enum LessonPlanConfirmationFailure
{
    None,
    Invalid,
    NotFound,
    NotDraft,
    ValidationFailed,
    Conflict,
}

public sealed record LessonPlanConfirmationResult(
    LessonPlanConfirmationView? View,
    LessonPlanConfirmationFailure Failure);

public interface ILessonPlanConfirmationService
{
    Task<LessonPlanConfirmationResult> GetAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken);
    Task<LessonPlanConfirmationResult> ConfirmAsync(Guid teacherId, Guid lessonPlanId,
        string planRowVersion, string versionRowVersion, CancellationToken cancellationToken);
    Task<LessonPlanConfirmationResult> CreateEditableDraftAsync(Guid teacherId, Guid lessonPlanId,
        string planRowVersion, CancellationToken cancellationToken);
}
