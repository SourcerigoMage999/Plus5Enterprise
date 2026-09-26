namespace Plus5.Application.Readiness;

public sealed record StudentKnowledgeComponentDetail(
    Guid KnowledgeComponentId,
    Guid? ParentKnowledgeComponentId,
    string Name,
    int SortOrder,
    string Status,
    decimal? Score,
    string Confidence,
    string Readiness,
    int EvidenceCount,
    decimal EffectiveEvidenceWeight,
    DateTimeOffset CalculatedAtUtc,
    string AlgorithmVersion);

public sealed record StudentKnowledgeAreaDetail(
    Guid KnowledgeAreaId,
    string Name,
    int SortOrder,
    decimal? Score,
    string Confidence,
    string Readiness,
    int EvidenceCount,
    decimal EffectiveEvidenceWeight,
    DateTimeOffset? CalculatedAtUtc,
    string? AlgorithmVersion,
    IReadOnlyList<StudentKnowledgeComponentDetail> Components);

public sealed record StudentKnowledgeModelDetail(
    Guid KnowledgeModelId,
    string Code,
    string Version,
    string Status,
    IReadOnlyList<StudentKnowledgeAreaDetail> Areas);

public sealed record StudentKnowledgeDetailSnapshot(
    Guid StudentId,
    string FirstName,
    string LastName,
    string SchoolGradeName,
    string? SchoolGradeCode,
    string? SchoolName,
    string? ProgramName,
    string? GroupName,
    IReadOnlyList<StudentKnowledgeModelDetail> Models);

public interface IStudentKnowledgeDetailQuery
{
    Task<StudentKnowledgeDetailSnapshot?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken);
}
