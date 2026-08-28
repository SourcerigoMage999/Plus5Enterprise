using Microsoft.EntityFrameworkCore;
using Plus5.Application.Students;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Students;

public sealed class EfStudentListQuery(Plus5DbContext dbContext) : IStudentListQuery
{
    public async Task<StudentListPage> GetPageAsync(
        Guid teacherAccountId,
        StudentListCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentOutOfRangeException.ThrowIfLessThan(criteria.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(criteria.PageSize, 1);

        var search = criteria.Search?.Trim();
        var students = dbContext.Students
            .AsNoTracking()
            .Where(student => student.TeacherAccountId == teacherAccountId
                && student.ArchivedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            students = students.Where(student =>
                student.FirstName.Contains(search)
                || student.LastName.Contains(search)
                || (student.FirstName + " " + student.LastName).Contains(search)
                || (student.Nickname != null && student.Nickname.Contains(search)));
        }

        if (criteria.ProgramId.HasValue)
        {
            students = students.Where(student => student.ProgramId == criteria.ProgramId);
        }

        if (criteria.DeliveryMode.HasValue)
        {
            var deliveryMode = (DeliveryMode)criteria.DeliveryMode.Value;
            students = students.Where(student => student.DeliveryMode == deliveryMode);
        }

        if (criteria.Status.HasValue)
        {
            var status = (StudentStatus)criteria.Status.Value;
            students = students.Where(student => student.Status == status);
        }

        if (criteria.SchoolGradeId.HasValue)
        {
            students = students.Where(student => student.SchoolGradeId == criteria.SchoolGradeId);
        }

        var totalCount = await students.LongCountAsync(cancellationToken);
        var skip = ((long)criteria.Page - 1) * criteria.PageSize;

        if (skip > int.MaxValue)
        {
            return new StudentListPage([], criteria.Page, criteria.PageSize, totalCount);
        }

        var items = await (
            from student in students
            join schoolGrade in dbContext.SchoolGrades.AsNoTracking()
                on student.SchoolGradeId equals schoolGrade.Id
            join programCandidate in dbContext.Programs.AsNoTracking()
                    .Where(program => program.TeacherAccountId == teacherAccountId)
                on student.ProgramId equals (Guid?)programCandidate.Id into programs
            from program in programs.DefaultIfEmpty()
            let activeGroupId = dbContext.GroupMemberships
                .Where(membership => membership.TeacherAccountId == teacherAccountId
                    && membership.StudentId == student.Id
                    && membership.LeftAtUtc == null)
                .Select(membership => (Guid?)membership.GroupId)
                .SingleOrDefault()
            let activeGroupName = dbContext.Groups
                .Where(studentGroup => studentGroup.TeacherAccountId == teacherAccountId
                    && studentGroup.Id == activeGroupId)
                .Select(studentGroup => studentGroup.Name)
                .SingleOrDefault()
            let lastSessionAtUtc = dbContext.Sessions
                .Where(session => session.TeacherAccountId == teacherAccountId
                    && session.Status == SessionStatus.Held
                    && ((student.DeliveryMode == DeliveryMode.Individual
                            && session.StudentId == student.Id)
                        || (student.DeliveryMode == DeliveryMode.Group
                            && activeGroupId.HasValue
                            && session.GroupId == activeGroupId)))
                .OrderByDescending(session => session.EndsAtUtc)
                .Select(session => (DateTimeOffset?)session.EndsAtUtc)
                .FirstOrDefault()
            orderby student.LastName, student.FirstName, student.Id
            select new StudentListItem(
                student.Id,
                student.FirstName,
                student.LastName,
                student.Nickname,
                schoolGrade.Id,
                schoolGrade.Code,
                schoolGrade.Name,
                student.ProgramId,
                program == null ? null : program.Name,
                student.DeliveryMode.HasValue
                    ? (StudentListDeliveryMode?)student.DeliveryMode.GetValueOrDefault()
                    : null,
                activeGroupId,
                activeGroupName,
                (StudentListStatus)student.Status,
                lastSessionAtUtc))
            .Skip((int)skip)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new StudentListPage(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<StudentListOverview> GetOverviewAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);

        var students = dbContext.Students
            .AsNoTracking()
            .Where(student => student.TeacherAccountId == teacherAccountId
                && student.ArchivedAtUtc == null);

        var statusCounts = await students
            .GroupBy(student => student.Status)
            .Select(group => new { Status = group.Key, Count = group.LongCount() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var programCounts = await (
            from student in students
            where student.ProgramId.HasValue
            join program in dbContext.Programs.AsNoTracking()
                    .Where(program => program.TeacherAccountId == teacherAccountId)
                on student.ProgramId equals (Guid?)program.Id
            group student by new { program.Id, program.Name } into programGroup
            orderby programGroup.Count() descending, programGroup.Key.Name
            select new StudentProgramCount(
                programGroup.Key.Id,
                programGroup.Key.Name,
                programGroup.LongCount()))
            .ToListAsync(cancellationToken);

        var programOptions = await (
            from student in students
            where student.ProgramId.HasValue
            join program in dbContext.Programs.AsNoTracking()
                    .Where(program => program.TeacherAccountId == teacherAccountId)
                on student.ProgramId equals (Guid?)program.Id
            select new { program.Id, program.Name })
            .Distinct()
            .OrderBy(program => program.Name)
            .ToListAsync(cancellationToken);
        var programs = programOptions
            .Select(program => new StudentFilterOption(program.Id, string.Empty, program.Name))
            .ToList();

        var schoolGradeOptions = await (
            from student in students
            join schoolGrade in dbContext.SchoolGrades.AsNoTracking()
                on student.SchoolGradeId equals schoolGrade.Id
            select new
            {
                schoolGrade.Id,
                schoolGrade.Code,
                schoolGrade.Name,
                schoolGrade.SortOrder,
            })
            .Distinct()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var schoolGrades = schoolGradeOptions
            .Select(schoolGrade => new StudentFilterOption(
                schoolGrade.Id,
                schoolGrade.Code,
                schoolGrade.Name))
            .ToList();

        return new StudentListOverview(
            statusCounts.Values.Sum(),
            statusCounts.GetValueOrDefault(StudentStatus.Active),
            statusCounts.GetValueOrDefault(StudentStatus.OnHold),
            statusCounts.GetValueOrDefault(StudentStatus.Inactive),
            await students.LongCountAsync(student => student.ProgramId == null, cancellationToken),
            programCounts,
            programs,
            schoolGrades);
    }
}
