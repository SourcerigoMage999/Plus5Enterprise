namespace Plus5.Application.Readiness;

public sealed record StudentReadinessArea(
    Guid KnowledgeAreaId,
    string KnowledgeModelCode,
    string KnowledgeModelVersion,
    string Name,
    int SortOrder,
    decimal? Score,
    string Confidence,
    string Readiness,
    int EvidenceCount,
    decimal EffectiveEvidenceWeight,
    DateTimeOffset CalculatedAtUtc,
    string AlgorithmVersion);

public sealed record StudentReadinessSnapshot(
    Guid StudentId,
    string FirstName,
    string LastName,
    string SchoolGradeName,
    string? SchoolGradeCode,
    IReadOnlyList<StudentReadinessArea> Areas);

public interface IStudentReadinessQuery
{
    Task<StudentReadinessSnapshot?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken);
}
