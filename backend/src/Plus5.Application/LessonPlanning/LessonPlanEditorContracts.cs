namespace Plus5.Application.LessonPlanning;

public sealed record LessonPlanEditorSession(
    Guid Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    string? Title);

public sealed record LessonActivityMaterialAttachment(
    Guid MaterialId,
    Guid MaterialVersionId,
    int VersionNumber,
    string Title,
    string MaterialTypeCode,
    string? Subject,
    bool IsCurrentVersion,
    bool IsOwner,
    int MatchingKnowledgeComponentCount);

public sealed record LessonPlanEditorActivity(
    Guid Id,
    int SortOrder,
    string Name,
    string Purpose,
    int DurationMinutes,
    string? KnowledgeBlockCode,
    IReadOnlyList<Guid> KnowledgeComponentIds,
    IReadOnlyList<LessonActivityMaterialAttachment> Materials);

public sealed record LessonPlanEditorView(
    Guid LessonPlanId,
    Guid LessonPlanVersionId,
    string RowVersion,
    string ParticipantName,
    string ParticipantType,
    LessonPlanEditorSession? Session,
    string Goal,
    int TargetDurationMinutes,
    IReadOnlyList<LessonStructureKnowledge> KnowledgeComponents,
    IReadOnlyList<LessonPlanEditorActivity> Activities,
    IReadOnlyList<LessonBalanceArea> Balance,
    IReadOnlyList<LessonActivityMaterialAttachment> AttachedMaterials);

public sealed record LessonMaterialCandidate(
    Guid MaterialId,
    Guid MaterialVersionId,
    int VersionNumber,
    string Title,
    string MaterialTypeCode,
    string? Subject,
    bool IsOwner,
    int MatchingKnowledgeComponentCount,
    bool IsAttached);

public sealed record LessonMaterialCandidatePage(
    IReadOnlyList<LessonMaterialCandidate> Items,
    int Page,
    int PageSize,
    long TotalCount);

public sealed record LessonActivityCandidate(
    Guid KnowledgeBlockVersionId,
    string Code,
    string Name,
    int VersionNumber,
    int MinDurationMinutes,
    int OptimalDurationMinutes,
    int MaxDurationMinutes,
    string ParticipantApplicability,
    string AssessmentCapability,
    int ExistingActivityCount);

public enum LessonPlanEditorFailure
{
    None,
    Invalid,
    NotFound,
    NotDraft,
    ActivityNotFound,
    MaterialUnavailable,
    AlreadyAttached,
    AttachmentNotFound,
    KnowledgeBlockUnavailable,
    Conflict,
}

public sealed record LessonPlanEditorResult(
    LessonPlanEditorView? View,
    LessonPlanEditorFailure Failure);

public sealed record LessonMaterialCandidateResult(
    LessonMaterialCandidatePage? Page,
    LessonPlanEditorFailure Failure);

public sealed record LessonActivityCandidateResult(
    IReadOnlyList<LessonActivityCandidate>? Items,
    LessonPlanEditorFailure Failure);

public interface ILessonPlanEditorService
{
    Task<LessonPlanEditorResult> GetAsync(
        Guid teacherId,
        Guid lessonPlanId,
        CancellationToken cancellationToken);

    Task<LessonMaterialCandidateResult> GetMaterialCandidatesAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<LessonActivityCandidateResult> GetActivityCandidatesAsync(
        Guid teacherId,
        Guid lessonPlanId,
        string? search,
        int? maximumDurationMinutes,
        CancellationToken cancellationToken);

    Task<LessonPlanEditorResult> AddActivityAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid knowledgeBlockVersionId,
        string rowVersion,
        CancellationToken cancellationToken);

    Task<LessonPlanEditorResult> AttachMaterialAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        string rowVersion,
        CancellationToken cancellationToken);

    Task<LessonPlanEditorResult> DetachMaterialAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        string rowVersion,
        CancellationToken cancellationToken);
}
