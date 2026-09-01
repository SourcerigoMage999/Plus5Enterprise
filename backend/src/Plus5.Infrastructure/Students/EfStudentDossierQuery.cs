using Microsoft.EntityFrameworkCore;
using Plus5.Application.Students;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Students;

public sealed class EfStudentDossierQuery(
    Plus5DbContext dbContext,
    TimeProvider timeProvider) : IStudentDossierQuery
{
    public async Task<StudentDossier?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentId, Guid.Empty);

        var profile = await (
            from student in dbContext.Students.AsNoTracking()
            where student.Id == studentId
                && student.TeacherAccountId == teacherAccountId
                && student.ArchivedAtUtc == null
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
            let primaryGuardian = dbContext.Guardians
                .Where(guardian => guardian.StudentId == student.Id && guardian.IsPrimary)
                .Select(guardian => new StudentDossierGuardian(
                    guardian.Id,
                    guardian.FirstName,
                    guardian.LastName,
                    guardian.Relationship,
                    guardian.Email,
                    guardian.Phone))
                .SingleOrDefault()
            select new StudentDossier(
                student.Id,
                student.FirstName,
                student.LastName,
                student.Nickname,
                student.DateOfBirth,
                student.SchoolName,
                student.Gender,
                student.Email,
                student.Phone,
                (StudentListStatus)student.Status,
                new StudentDossierReference(schoolGrade.Id, schoolGrade.Name, schoolGrade.Code),
                program == null
                    ? null
                    : new StudentDossierReference(program.Id, program.Name, null),
                student.DeliveryMode.HasValue
                    ? (StudentListDeliveryMode?)student.DeliveryMode.GetValueOrDefault()
                    : null,
                activeGroupId.HasValue && activeGroupName != null
                    ? new StudentDossierReference(activeGroupId.Value, activeGroupName, null)
                    : null,
                primaryGuardian,
                null,
                null))
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var nextSession = await dbContext.Sessions
            .AsNoTracking()
            .Where(session => session.TeacherAccountId == teacherAccountId
                && session.Status == SessionStatus.Scheduled
                && session.StartsAtUtc >= now
                && (session.StudentId == studentId
                    || (profile.Group != null && session.GroupId == profile.Group.Id)))
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new StudentDossierSession(
                session.Id,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.TimeZoneId,
                (StudentListDeliveryMode)session.DeliveryMode,
                session.GroupId.HasValue
                    ? new StudentDossierReference(
                        session.GroupId.Value,
                        dbContext.Groups
                            .Where(group => group.TeacherAccountId == teacherAccountId
                                && group.Id == session.GroupId)
                            .Select(group => group.Name)
                            .Single(),
                        null)
                    : null))
            .FirstOrDefaultAsync(cancellationToken);

        var lastHeldSession = await dbContext.Sessions
            .AsNoTracking()
            .Where(session => session.TeacherAccountId == teacherAccountId
                && session.Status == SessionStatus.Held
                && (session.StudentId == studentId
                    || (session.GroupId.HasValue && dbContext.GroupMemberships.Any(membership =>
                        membership.TeacherAccountId == teacherAccountId
                        && membership.StudentId == studentId
                        && membership.GroupId == session.GroupId
                        && membership.JoinedAtUtc <= session.StartsAtUtc
                        && (membership.LeftAtUtc == null || membership.LeftAtUtc >= session.EndsAtUtc)))))
            .OrderByDescending(session => session.EndsAtUtc)
            .Select(session => new StudentDossierSession(
                session.Id,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.TimeZoneId,
                (StudentListDeliveryMode)session.DeliveryMode,
                session.GroupId.HasValue
                    ? new StudentDossierReference(
                        session.GroupId.Value,
                        dbContext.Groups
                            .Where(group => group.TeacherAccountId == teacherAccountId
                                && group.Id == session.GroupId)
                            .Select(group => group.Name)
                            .Single(),
                        null)
                    : null))
            .FirstOrDefaultAsync(cancellationToken);

        return profile with
        {
            NextSession = nextSession,
            LastHeldSession = lastHeldSession,
        };
    }

}
