namespace Plus5.Application.Materials;

public interface IMaterialEditingQuery
{
    Task<MaterialEditWorkspace?> GetAsync(Guid teacherAccountId, Guid materialId, CancellationToken cancellationToken);
    Task<MaterialVersionSnapshot?> GetVersionAsync(Guid teacherAccountId, Guid materialId, Guid versionId, CancellationToken cancellationToken);
}

public interface IMaterialEditingService
{
    Task<MaterialEditResult> SaveDraftAsync(Guid teacherAccountId, Guid materialId, MaterialEditCommand command, CancellationToken cancellationToken);
    Task<MaterialEditResult> PublishDraftAsync(Guid teacherAccountId, Guid materialId, MaterialPublishCommand command, CancellationToken cancellationToken);
    Task<MaterialEditResult> RestoreAsync(Guid teacherAccountId, Guid materialId, Guid versionId, MaterialRestoreCommand command, CancellationToken cancellationToken);
}

public sealed record MaterialEditCommand(
    string ExpectedRowVersion,
    Guid? DraftVersionId,
    string Title,
    string MaterialTypeCode,
    string? Description,
    string? Subject,
    string? LanguageCode,
    Guid? ProgramId,
    Guid? SchoolGradeId,
    Guid? ProficiencyLevelId,
    string? LearningGoal,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<Guid> KnowledgeComponentIds,
    IReadOnlyCollection<Guid> CurriculumOutcomeIds);

public sealed record MaterialPublishCommand(string ExpectedRowVersion, Guid DraftVersionId);
public sealed record MaterialRestoreCommand(string ExpectedRowVersion);

public enum MaterialEditOutcome { Success, InvalidInput, NotFound, Conflict, ReferenceNotFound, DraftAlreadyExists, StorageFailure }
public sealed record MaterialEditResult(MaterialEditOutcome Outcome, Guid? VersionId = null);

public sealed record MaterialEditWorkspace(
    Guid MaterialId,
    string RowVersion,
    string Visibility,
    Guid CurrentVersionId,
    MaterialVersionSnapshot EditableVersion,
    IReadOnlyList<MaterialVersionHistoryItem> History,
    MaterialImportOptions Options);

public sealed record MaterialVersionHistoryItem(
    Guid Id,
    int VersionNumber,
    string Status,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? SupersededAtUtc,
    bool IsCurrent);

public sealed record MaterialVersionSnapshot(
    Guid Id,
    int VersionNumber,
    string Status,
    string Title,
    string? Description,
    string MaterialTypeCode,
    string? Subject,
    string? LanguageCode,
    Guid? ProgramId,
    Guid? SchoolGradeId,
    Guid? ProficiencyLevelId,
    string? LearningGoal,
    MaterialEditFile File,
    IReadOnlyList<string> Tags,
    IReadOnlyList<Guid> KnowledgeComponentIds,
    IReadOnlyList<Guid> CurriculumOutcomeIds,
    int AssessableTaskCount);

public sealed record MaterialEditFile(
    string Format,
    string OriginalFileName,
    string MediaType,
    long SizeBytes);
