namespace Plus5.Application.LessonPlanning;

public sealed record LessonActivityDetail(
    Guid LessonPlanId,
    Guid LessonPlanVersionId,
    string RowVersion,
    string ParticipantName,
    string ParticipantType,
    LessonPlanEditorSession? Session,
    string Goal,
    int TargetDurationMinutes,
    Guid ActivityId,
    int SortOrder,
    string Name,
    string Purpose,
    int DurationMinutes,
    string WorkMode,
    string? Description,
    string? PurposeDescription,
    string? TeacherInstructions,
    string? StudentInstructions,
    string? Notes,
    bool ShowTeacherInstructionsOnBoard,
    bool ShowStudentInstructionsOnBoard,
    bool ShowProgressOnBoard,
    Guid? KnowledgeBlockVersionId,
    string? KnowledgeBlockCode,
    string? KnowledgeBlockName,
    IReadOnlyList<LessonStructureKnowledge> AvailableKnowledgeComponents,
    IReadOnlyList<Guid> SelectedKnowledgeComponentIds,
    IReadOnlyList<LessonActivityMaterialAttachment> Materials,
    LessonBuilderAiStatus AiStatus);

public sealed record LessonBuilderAiStatus(
    bool Enabled,
    bool PaidExternalService,
    bool AutomaticCalls,
    bool InAppBilling);

public sealed record UpdateLessonActivityCommand(
    string RowVersion,
    string Name,
    string Purpose,
    int DurationMinutes,
    string WorkMode,
    string? Description,
    string? PurposeDescription,
    string? TeacherInstructions,
    string? StudentInstructions,
    string? Notes,
    bool ShowTeacherInstructionsOnBoard,
    bool ShowStudentInstructionsOnBoard,
    bool ShowProgressOnBoard);

public sealed record LessonActivityRowVersionCommand(string RowVersion);
public sealed record ReorderLessonActivitiesCommand(string RowVersion, IReadOnlyList<Guid> ActivityIds);
public sealed record UpdateLessonActivityKnowledgeCommand(string RowVersion, IReadOnlyList<Guid> KnowledgeComponentIds);

public enum LessonActivityEditingFailure
{
    None,
    Invalid,
    NotFound,
    NotDraft,
    Conflict,
}

public sealed record LessonActivityDetailResult(
    LessonActivityDetail? Detail,
    LessonActivityEditingFailure Failure);

public sealed record LessonActivityMutationResult(
    LessonPlanEditorView? View,
    LessonActivityEditingFailure Failure);

public interface ILessonActivityEditingService
{
    Task<LessonActivityDetailResult> GetAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken);
    Task<LessonActivityDetailResult> UpdateAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, UpdateLessonActivityCommand command, CancellationToken cancellationToken);
    Task<LessonActivityMutationResult> DuplicateAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, string rowVersion, CancellationToken cancellationToken);
    Task<LessonActivityMutationResult> DeleteAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, string rowVersion, CancellationToken cancellationToken);
    Task<LessonActivityMutationResult> ReorderAsync(Guid teacherId, Guid lessonPlanId, ReorderLessonActivitiesCommand command, CancellationToken cancellationToken);
    Task<LessonActivityDetailResult> UpdateKnowledgeAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, UpdateLessonActivityKnowledgeCommand command, CancellationToken cancellationToken);
}

public sealed record LessonActivityAiProviderRequest(string ContextJson, string OutputJsonSchema);

public enum LessonActivityAiProviderOutcome
{
    Success,
    Disabled,
    Unavailable,
    InvalidOutput,
}

public sealed record LessonActivityAiProviderResult(
    LessonActivityAiProviderOutcome Outcome,
    string? ContentJson = null,
    string? Provider = null,
    string? Model = null,
    string? PromptTemplateVersion = null,
    int? InputTokens = null,
    int? OutputTokens = null);

public interface ILessonBuilderAiAssistant
{
    Task<LessonActivityAiProviderResult> GenerateActivitySuggestionAsync(
        LessonActivityAiProviderRequest request,
        CancellationToken cancellationToken);
}

public sealed record LessonBuilderAiContext(string ContextJson);

public interface ILessonBuilderAiContextBuilder
{
    Task<LessonBuilderAiContext?> BuildAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken);
}

public sealed record CreateLessonActivityAiSuggestionCommand(string RowVersion, Guid? ReplacesSuggestionId);
public sealed record ResolveLessonActivityAiSuggestionCommand(string RowVersion, string Decision, string? EditedContentJson);

public sealed record LessonActivityAiSuggestionView(
    Guid Id,
    string Status,
    string? ContentJson,
    string Provider,
    string Model,
    string PromptTemplateVersion,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    Guid? ReplacesSuggestionId);

public enum LessonActivityAiSuggestionOutcome
{
    Success,
    Disabled,
    InvalidInput,
    InvalidOutput,
    NotFound,
    Conflict,
    Expired,
    Unavailable,
    DailyLimitReached,
}

public sealed record LessonActivityAiSuggestionResult(
    LessonActivityAiSuggestionOutcome Outcome,
    LessonActivityAiSuggestionView? Suggestion = null,
    LessonActivityDetail? Detail = null);

public interface ILessonActivityAiSuggestionService
{
    Task<LessonActivityAiSuggestionResult> CreateAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, CreateLessonActivityAiSuggestionCommand command, CancellationToken cancellationToken);
    Task<LessonActivityAiSuggestionView?> GetAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, Guid suggestionId, CancellationToken cancellationToken);
    Task<LessonActivityAiSuggestionResult> ResolveAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, Guid suggestionId, ResolveLessonActivityAiSuggestionCommand command, CancellationToken cancellationToken);
}
