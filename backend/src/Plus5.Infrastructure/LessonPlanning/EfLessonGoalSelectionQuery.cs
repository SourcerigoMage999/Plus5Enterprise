using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonGoalSelectionQuery(Plus5DbContext db, TimeProvider clock)
    : ILessonGoalSelectionQuery
{
    public async Task<LessonGoalPage<LessonGoalParticipant>> GetParticipantsAsync(
        Guid teacherId,
        LessonParticipantKind kind,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var term = search?.Trim();
        if (kind == LessonParticipantKind.Student)
        {
            var query =
                from student in db.Students.AsNoTracking()
                join grade in db.SchoolGrades.AsNoTracking() on student.SchoolGradeId equals grade.Id
                join program in db.Programs.AsNoTracking() on student.ProgramId equals program.Id into programs
                from program in programs.DefaultIfEmpty()
                where student.TeacherAccountId == teacherId
                    && student.ArchivedAtUtc == null
                    && student.Status != StudentStatus.Inactive
                    && (term == null || (student.FirstName + " " + student.LastName).Contains(term))
                orderby student.LastName, student.FirstName, student.Id
                select new LessonGoalParticipant(student.Id, student.FirstName + " " + student.LastName,
                    grade.Name, program == null ? null : program.Name, null);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new(items, page, pageSize, total);
        }

        var groups =
            from lessonGroup in db.Groups.AsNoTracking()
            join grade in db.SchoolGrades.AsNoTracking() on lessonGroup.SchoolGradeId equals grade.Id
            join program in db.Programs.AsNoTracking() on lessonGroup.ProgramId equals program.Id
            where lessonGroup.TeacherAccountId == teacherId
                && lessonGroup.ArchivedAtUtc == null
                && lessonGroup.Status != GroupStatus.Inactive
                && (term == null || lessonGroup.Name.Contains(term))
            let memberCount = db.GroupMemberships.Count(m => m.GroupId == lessonGroup.Id && m.LeftAtUtc == null)
            orderby lessonGroup.Name, lessonGroup.Id
            select new LessonGoalParticipant(lessonGroup.Id, lessonGroup.Name, grade.Name, program.Name, memberCount);
        var groupTotal = await groups.CountAsync(cancellationToken);
        var groupItems = await groups.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new(groupItems, page, pageSize, groupTotal);
    }

    public async Task<IReadOnlyList<LessonGoalSession>> GetSessionsAsync(
        Guid teacherId,
        LessonParticipantKind kind,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return await db.Sessions.AsNoTracking()
            .Where(item => item.TeacherAccountId == teacherId
                && item.Status == SessionStatus.Scheduled
                && item.StartsAtUtc >= now
                && (kind == LessonParticipantKind.Student
                    ? item.StudentId == participantId && item.GroupId == null
                    : item.GroupId == participantId && item.StudentId == null))
            .OrderBy(item => item.StartsAtUtc)
            .Take(50)
            .Select(item => new LessonGoalSession(item.Id, item.StartsAtUtc, item.EndsAtUtc,
                item.TimeZoneId, item.Title))
            .ToListAsync(cancellationToken);
    }

    public async Task<LessonGoalPage<LessonKnowledgeComponentOption>> GetKnowledgeComponentsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var term = search?.Trim();
        var query =
            from component in db.KnowledgeComponents.AsNoTracking()
            join area in db.KnowledgeAreas.AsNoTracking() on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking() on component.KnowledgeModelId equals model.Id
            where component.Status == KnowledgeComponentStatus.Active
                && model.Status == KnowledgeModelStatus.Published
                && (term == null || component.Name.Contains(term) || area.Name.Contains(term))
            orderby model.Code, model.Version, area.SortOrder, component.SortOrder, component.Name
            select new LessonKnowledgeComponentOption(component.Id, component.Name, area.Name,
                model.Code, model.Version);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }
}
