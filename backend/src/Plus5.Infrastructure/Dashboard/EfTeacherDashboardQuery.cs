using Microsoft.EntityFrameworkCore;
using Plus5.Application.Dashboard;
using Plus5.Application.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Dashboard;

public sealed class EfTeacherDashboardQuery(
    Plus5DbContext dbContext,
    TimeProvider timeProvider) : ITeacherDashboardQuery
{
    public async Task<TeacherDashboard> GetAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);

        var generatedAtUtc = timeProvider.GetUtcNow();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(generatedAtUtc, zone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var tomorrow = today.AddDays(1);
        var todayStartsAtUtc = LocalMidnightToUtc(today, zone);
        var tomorrowStartsAtUtc = LocalMidnightToUtc(tomorrow, zone);

        var visibleSessions = dbContext.Sessions.AsNoTracking()
            .Where(session => session.TeacherAccountId == teacherAccountId
                && session.Status != SessionStatus.Cancelled);

        var todaySessions = await Project(
                visibleSessions.Where(session => session.StartsAtUtc >= todayStartsAtUtc
                    && session.StartsAtUtc < tomorrowStartsAtUtc),
                teacherAccountId)
            .OrderBy(session => session.StartsAtUtc)
            .ThenBy(session => session.EndsAtUtc)
            .ThenBy(session => session.Id)
            .ToListAsync(cancellationToken);

        var nextSession = await Project(
                visibleSessions.Where(session => session.Status == SessionStatus.Scheduled
                    && session.StartsAtUtc >= generatedAtUtc),
                teacherAccountId)
            .OrderBy(session => session.StartsAtUtc)
            .ThenBy(session => session.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var studentCounts = await dbContext.Students.AsNoTracking()
            .Where(student => student.TeacherAccountId == teacherAccountId
                && student.ArchivedAtUtc == null)
            .GroupBy(_ => 1)
            .Select(students => new TeacherDashboardStudentOverview(
                students.Count(),
                students.Count(student => student.Status == StudentStatus.Active),
                students.Count(student => student.Status == StudentStatus.OnHold),
                students.Count(student => student.Status == StudentStatus.Inactive)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new TeacherDashboardStudentOverview(0, 0, 0, 0);

        return new TeacherDashboard(
            GroupScheduleGenerator.TimeZoneId,
            today,
            generatedAtUtc,
            todaySessions,
            nextSession,
            studentCounts);
    }

    private IQueryable<TeacherDashboardSession> Project(
        IQueryable<Session> sessions,
        Guid teacherAccountId) =>
        sessions.Select(session => new TeacherDashboardSession(
            session.Id,
            (int)session.DeliveryMode,
            session.GroupId,
            session.StudentId,
            session.GroupId.HasValue
                ? dbContext.Groups
                    .Where(group => group.TeacherAccountId == teacherAccountId
                        && group.Id == session.GroupId)
                    .Select(group => group.Name)
                    .Single()
                : dbContext.Students
                    .Where(student => student.TeacherAccountId == teacherAccountId
                        && student.Id == session.StudentId)
                    .Select(student => student.FirstName + " " + student.LastName)
                    .Single(),
            session.GroupId.HasValue
                ? (from sourceGroup in dbContext.Groups
                   join grade in dbContext.SchoolGrades on sourceGroup.SchoolGradeId equals grade.Id
                   where sourceGroup.TeacherAccountId == teacherAccountId
                       && sourceGroup.Id == session.GroupId
                   select grade.Name).Single()
                : (from student in dbContext.Students
                   join grade in dbContext.SchoolGrades on student.SchoolGradeId equals grade.Id
                   where student.TeacherAccountId == teacherAccountId
                       && student.Id == session.StudentId
                   select grade.Name).Single(),
            session.GroupId.HasValue
                ? dbContext.Groups
                    .Where(group => group.TeacherAccountId == teacherAccountId
                        && group.Id == session.GroupId)
                    .Select(group => (Guid?)group.ProgramId)
                    .Single()
                : dbContext.Students
                    .Where(student => student.TeacherAccountId == teacherAccountId
                        && student.Id == session.StudentId)
                    .Select(student => student.ProgramId)
                    .Single(),
            session.GroupId.HasValue
                ? (from sourceGroup in dbContext.Groups
                   join program in dbContext.Programs on sourceGroup.ProgramId equals program.Id
                   where sourceGroup.TeacherAccountId == teacherAccountId
                       && sourceGroup.Id == session.GroupId
                       && program.TeacherAccountId == teacherAccountId
                   select program.Name).Single()
                : (from student in dbContext.Students
                   join program in dbContext.Programs on student.ProgramId equals (Guid?)program.Id
                   where student.TeacherAccountId == teacherAccountId
                       && student.Id == session.StudentId
                       && program.TeacherAccountId == teacherAccountId
                   select program.Name).SingleOrDefault(),
            session.Title,
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.TimeZoneId,
            session.LocationId,
            dbContext.Locations
                .Where(location => location.TeacherAccountId == teacherAccountId
                    && location.Id == session.LocationId)
                .Select(location => location.Name)
                .SingleOrDefault(),
            session.OnlineMeetingUrl != null,
            (int)session.Status,
            session.GroupId.HasValue
                ? dbContext.GroupMemberships.Count(membership =>
                    membership.TeacherAccountId == teacherAccountId
                    && membership.GroupId == session.GroupId
                    && membership.JoinedAtUtc <= session.StartsAtUtc
                    && (membership.LeftAtUtc == null
                        || membership.LeftAtUtc > session.StartsAtUtc))
                : 1));

    private static DateTimeOffset LocalMidnightToUtc(DateOnly date, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }
}
