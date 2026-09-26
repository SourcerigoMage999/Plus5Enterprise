using Microsoft.EntityFrameworkCore;
using Plus5.Application.Readiness;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Readiness;

public sealed class EfStudentKnowledgeDetailQuery(Plus5DbContext db) : IStudentKnowledgeDetailQuery
{
    public async Task<StudentKnowledgeDetailSnapshot?> GetAsync(
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
            let programName = db.Programs.AsNoTracking()
                .Where(program => program.Id == candidate.ProgramId
                    && program.TeacherAccountId == teacherAccountId)
                .Select(program => program.Name)
                .SingleOrDefault()
            let activeGroupId = db.GroupMemberships.AsNoTracking()
                .Where(membership => membership.TeacherAccountId == teacherAccountId
                    && membership.StudentId == candidate.Id
                    && membership.LeftAtUtc == null)
                .Select(membership => (Guid?)membership.GroupId)
                .SingleOrDefault()
            let groupName = db.Groups.AsNoTracking()
                .Where(studentGroup => studentGroup.Id == activeGroupId
                    && studentGroup.TeacherAccountId == teacherAccountId)
                .Select(studentGroup => studentGroup.Name)
                .SingleOrDefault()
            select new
            {
                candidate.Id,
                candidate.FirstName,
                candidate.LastName,
                candidate.SchoolName,
                SchoolGradeName = grade.Name,
                SchoolGradeCode = grade.Code,
                ProgramName = programName,
                GroupName = groupName,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            return null;
        }

        var componentRows = await (
            from estimate in db.MasteryEstimates.AsNoTracking()
            where estimate.StudentId == studentId
            join component in db.KnowledgeComponents.AsNoTracking()
                on estimate.KnowledgeComponentId equals component.Id
            join area in db.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            orderby model.Code, model.Version, area.SortOrder, area.Id,
                component.SortOrder, component.Id
            select new
            {
                KnowledgeModelId = model.Id,
                model.Code,
                model.Version,
                ModelStatus = model.Status,
                KnowledgeAreaId = area.Id,
                AreaName = area.Name,
                AreaSortOrder = area.SortOrder,
                KnowledgeComponentId = component.Id,
                ParentKnowledgeComponentId = component.ParentComponentId,
                ComponentName = component.Name,
                ComponentSortOrder = component.SortOrder,
                ComponentStatus = component.Status,
                estimate.Score,
                estimate.Confidence,
                estimate.Readiness,
                estimate.EvidenceCount,
                estimate.EffectiveEvidenceWeight,
                estimate.CalculatedAtUtc,
                estimate.AlgorithmVersion,
            })
            .ToListAsync(cancellationToken);

        var areaRows = await (
            from estimate in db.KnowledgeAreaReadinessEstimates.AsNoTracking()
            where estimate.StudentId == studentId
            select new
            {
                estimate.KnowledgeAreaId,
                estimate.Score,
                estimate.Confidence,
                estimate.Readiness,
                estimate.EvidenceCount,
                estimate.EffectiveEvidenceWeight,
                estimate.CalculatedAtUtc,
                estimate.AlgorithmVersion,
            })
            .ToListAsync(cancellationToken);
        var areaEstimates = areaRows.ToDictionary(row => row.KnowledgeAreaId);

        var models = componentRows
            .GroupBy(row => new
            {
                row.KnowledgeModelId,
                row.Code,
                row.Version,
                row.ModelStatus,
            })
            .Select(modelGroup => new StudentKnowledgeModelDetail(
                modelGroup.Key.KnowledgeModelId,
                modelGroup.Key.Code,
                modelGroup.Key.Version,
                modelGroup.Key.ModelStatus.ToString(),
                modelGroup
                    .GroupBy(row => new
                    {
                        row.KnowledgeAreaId,
                        row.AreaName,
                        row.AreaSortOrder,
                    })
                    .OrderBy(areaGroup => areaGroup.Key.AreaSortOrder)
                    .ThenBy(areaGroup => areaGroup.Key.KnowledgeAreaId)
                    .Select(areaGroup =>
                    {
                        var hasAreaEstimate = areaEstimates.TryGetValue(
                            areaGroup.Key.KnowledgeAreaId,
                            out var areaEstimate);
                        return new StudentKnowledgeAreaDetail(
                            areaGroup.Key.KnowledgeAreaId,
                            areaGroup.Key.AreaName,
                            areaGroup.Key.AreaSortOrder,
                            hasAreaEstimate ? areaEstimate!.Score : null,
                            hasAreaEstimate ? areaEstimate!.Confidence.ToString() : "NoData",
                            hasAreaEstimate ? areaEstimate!.Readiness.ToString() : "InsufficientData",
                            hasAreaEstimate ? areaEstimate!.EvidenceCount : 0,
                            hasAreaEstimate ? areaEstimate!.EffectiveEvidenceWeight : 0m,
                            hasAreaEstimate ? areaEstimate!.CalculatedAtUtc : null,
                            hasAreaEstimate ? areaEstimate!.AlgorithmVersion : null,
                            areaGroup
                                .Select(row => new StudentKnowledgeComponentDetail(
                                    row.KnowledgeComponentId,
                                    row.ParentKnowledgeComponentId,
                                    row.ComponentName,
                                    row.ComponentSortOrder,
                                    row.ComponentStatus.ToString(),
                                    row.Score,
                                    row.Confidence.ToString(),
                                    row.Readiness.ToString(),
                                    row.EvidenceCount,
                                    row.EffectiveEvidenceWeight,
                                    row.CalculatedAtUtc,
                                    row.AlgorithmVersion))
                                .ToArray());
                    })
                    .ToArray()))
            .ToArray();

        return new StudentKnowledgeDetailSnapshot(
            student.Id,
            student.FirstName,
            student.LastName,
            student.SchoolGradeName,
            student.SchoolGradeCode,
            student.SchoolName,
            student.ProgramName,
            student.GroupName,
            models);
    }
}
