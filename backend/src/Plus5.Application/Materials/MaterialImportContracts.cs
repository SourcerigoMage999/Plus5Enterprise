namespace Plus5.Application.Materials;

public interface IMaterialImportQuery
{
    Task<MaterialImportOptions> GetOptionsAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken);
}

public interface IMaterialImportService
{
    Task<MaterialImportResult> ImportAsync(
        Guid teacherAccountId,
        MaterialImportCommand command,
        CancellationToken cancellationToken);
}

public sealed record MaterialImportCommand(
    string OriginalFileName,
    string DeclaredMediaType,
    long DeclaredSizeBytes,
    MaterialImportFileFormat Format,
    Func<Stream> OpenReadStream,
    string Title,
    string MaterialTypeCode,
    string? Description,
    string? Subject,
    string? LanguageCode,
    MaterialImportVisibility Visibility,
    Guid? ProgramId,
    Guid? SchoolGradeId,
    Guid? ProficiencyLevelId,
    string? LearningGoal,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<Guid> KnowledgeComponentIds,
    IReadOnlyCollection<Guid> CurriculumOutcomeIds);

public enum MaterialImportVisibility
{
    Private = 1,
    Shared = 2,
}

public enum MaterialImportFileFormat
{
    Pdf = 1,
    Docx = 2,
    Pptx = 3,
    Mp4 = 4,
    Zip = 5,
}

public enum MaterialImportOutcome
{
    Created,
    InvalidInput,
    InvalidFile,
    ReferenceNotFound,
    MalwareDetected,
    ScannerUnavailable,
}

public sealed record MaterialImportResult(
    MaterialImportOutcome Outcome,
    Guid? MaterialId = null,
    string? Detail = null);

public sealed record MaterialImportOptions(
    IReadOnlyList<MaterialImportReference> Programs,
    IReadOnlyList<MaterialImportReference> SchoolGrades,
    IReadOnlyList<MaterialImportReference> ProficiencyLevels,
    IReadOnlyList<MaterialImportKnowledgeComponent> KnowledgeComponents,
    IReadOnlyList<MaterialImportCurriculumOutcome> CurriculumOutcomes,
    IReadOnlyList<string> MaterialTypeCodes,
    IReadOnlyList<string> LanguageCodes);

public sealed record MaterialImportReference(Guid Id, string Name, string? Code);

public sealed record MaterialImportKnowledgeComponent(
    Guid Id,
    string Name,
    string KnowledgeAreaName,
    string KnowledgeModelCode,
    string KnowledgeModelVersion);

public sealed record MaterialImportCurriculumOutcome(
    Guid Id,
    string Title,
    string? OfficialCode,
    string CurriculumCode,
    string CurriculumVersion);
