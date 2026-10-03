namespace Plus5.Application.Materials;

public enum MaterialDetailKnowledgeModelStatus
{
    Published = 1,
    Retired = 2,
}

public sealed record MaterialDetailReference(Guid Id, string Name, string? Code);

public sealed record MaterialDetailFile(
    MaterialLibraryFileFormat Format,
    string OriginalFileName,
    string MediaType,
    long SizeBytes);

public sealed record MaterialDetailKnowledgeComponent(
    Guid Id,
    string Name,
    string KnowledgeAreaName,
    string KnowledgeModelCode,
    string KnowledgeModelVersion,
    MaterialDetailKnowledgeModelStatus KnowledgeModelStatus);

public sealed record MaterialDetailCurriculumOutcome(
    Guid Id,
    string? OfficialCode,
    string Title,
    string CurriculumCode,
    string CurriculumName,
    string CurriculumVersion);

public sealed record MaterialDetail(
    Guid Id,
    Guid VersionId,
    int VersionNumber,
    string Title,
    string? Description,
    string MaterialTypeCode,
    string? Subject,
    string? LanguageCode,
    MaterialDetailReference? Program,
    MaterialDetailReference? SchoolGrade,
    MaterialDetailReference? ProficiencyLevel,
    string? LearningGoal,
    MaterialDetailFile File,
    DateTimeOffset AddedAtUtc,
    bool IsOwner,
    MaterialLibraryShareAccess? ShareAccess,
    IReadOnlyList<string> Tags,
    IReadOnlyList<MaterialDetailKnowledgeComponent> KnowledgeComponents,
    IReadOnlyList<MaterialDetailCurriculumOutcome> CurriculumOutcomes);

public interface IMaterialDetailQuery
{
    Task<MaterialDetail?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken);
}
