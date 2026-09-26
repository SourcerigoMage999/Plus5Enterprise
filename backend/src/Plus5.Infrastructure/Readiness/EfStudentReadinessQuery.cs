using Microsoft.EntityFrameworkCore;
using Plus5.Application.Readiness;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Readiness;

public sealed class EfStudentReadinessQuery(Plus5DbContext db) : IStudentReadinessQuery
{
    public async Task<StudentReadinessSnapshot?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentId, Guid.Empty);

        var student = await (
            from candidate in db.Students.AsNoTracking()
            where candidate.Id == studentId
                && candidate.TeacherAccountId == teacherAccountId
                && candidate.ArchivedAtUtc == null
            join grade in db.SchoolGrades.AsNoTracking()
                on candidate.SchoolGradeId equals grade.Id
            select new
            {
                candidate.Id,
                candidate.FirstName,
                candidate.LastName,
                SchoolGradeName = grade.Name,
                SchoolGradeCode = grade.Code,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            return null;
        }

        var areaRows = await (
            from estimate in db.KnowledgeAreaReadinessEstimates.AsNoTracking()
            where estimate.StudentId == studentId
            join area in db.KnowledgeAreas.AsNoTracking()
                on estimate.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on area.KnowledgeModelId equals model.Id
            orderby model.Code, model.Version, area.SortOrder, area.Id
            select new
            {
                KnowledgeAreaId = area.Id,
                KnowledgeModelCode = model.Code,
                KnowledgeModelVersion = model.Version,
                area.Name,
                area.SortOrder,
                estimate.Score,
                estimate.Confidence,
                estimate.Readiness,
                estimate.EvidenceCount,
                estimate.EffectiveEvidenceWeight,
                estimate.CalculatedAtUtc,
                estimate.AlgorithmVersion,
            })
            .ToListAsync(cancellationToken);
        var areas = areaRows.Select(row => new StudentReadinessArea(
            row.KnowledgeAreaId,
            row.KnowledgeModelCode,
            row.KnowledgeModelVersion,
            row.Name,
            row.SortOrder,
            row.Score,
            row.Confidence.ToString(),
            row.Readiness.ToString(),
            row.EvidenceCount,
            row.EffectiveEvidenceWeight,
            row.CalculatedAtUtc,
            row.AlgorithmVersion))
            .ToArray();

        return new StudentReadinessSnapshot(
            student.Id,
            student.FirstName,
            student.LastName,
            student.SchoolGradeName,
            student.SchoolGradeCode,
            areas);
    }
}
