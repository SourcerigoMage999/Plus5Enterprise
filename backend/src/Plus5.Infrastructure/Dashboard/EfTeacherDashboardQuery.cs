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

        var todaySessions = await LoadSessionsAsync(
            visibleSessions.Where(session => session.StartsAtUtc >= todayStartsAtUtc
                && session.StartsAtUtc < tomorrowStartsAtUtc)
            .OrderBy(session => session.StartsAtUtc)
            .ThenBy(session => session.EndsAtUtc)
            .ThenBy(session => session.Id),
            teacherAccountId,
            cancellationToken);

        var nextSessions = await LoadSessionsAsync(
            visibleSessions.Where(session => session.Status == SessionStatus.Scheduled
                && session.StartsAtUtc >= generatedAtUtc)
            .OrderBy(session => session.StartsAtUtc)
            .ThenBy(session => session.Id)
            .Take(1),
            teacherAccountId,
            cancellationToken);
        var nextSession = nextSessions.SingleOrDefault();

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

    private async Task<List<TeacherDashboardSession>> LoadSessionsAsync(
        IQueryable<Session> sessions,
        Guid teacherAccountId,
        CancellationToken cancellationToken)
    {
        var rows = await sessions
            .Select(session => new
            {
                session.Id,
                session.DeliveryMode,
                session.GroupId,
                session.StudentId,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.TimeZoneId,
                session.LocationId,
                Online = session.OnlineMeetingUrl != null,
                session.Status,
            })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var groupIds = rows
            .Where(row => row.GroupId.HasValue)
            .Select(row => row.GroupId!.Value)
            .Distinct()
            .ToList();
        var studentIds = rows
            .Where(row => row.StudentId.HasValue)
            .Select(row => row.StudentId!.Value)
            .Distinct()
            .ToList();
        var locationIds = rows
            .Where(row => row.LocationId.HasValue)
            .Select(row => row.LocationId!.Value)
            .Distinct()
            .ToList();

        var groups = await dbContext.Groups.AsNoTracking()
            .Where(group => group.TeacherAccountId == teacherAccountId
                && groupIds.Contains(group.Id))
            .Select(group => new
            {
                group.Id,
                group.Name,
                group.SchoolGradeId,
                ProgramId = (Guid?)group.ProgramId,
            })
            .ToDictionaryAsync(group => group.Id, cancellationToken);
        var students = await dbContext.Students.AsNoTracking()
            .Where(student => student.TeacherAccountId == teacherAccountId
                && studentIds.Contains(student.Id))
            .Select(student => new
            {
                student.Id,
                Name = student.FirstName + " " + student.LastName,
                student.SchoolGradeId,
                student.ProgramId,
            })
            .ToDictionaryAsync(student => student.Id, cancellationToken);

        var gradeIds = groups.Values.Select(group => group.SchoolGradeId)
            .Concat(students.Values.Select(student => student.SchoolGradeId))
            .Distinct()
            .ToList();
        var programIds = groups.Values.Where(group => group.ProgramId.HasValue)
            .Select(group => group.ProgramId!.Value)
            .Concat(students.Values.Where(student => student.ProgramId.HasValue)
                .Select(student => student.ProgramId!.Value))
            .Distinct()
            .ToList();

        var grades = await dbContext.SchoolGrades.AsNoTracking()
            .Where(grade => gradeIds.Contains(grade.Id))
            .ToDictionaryAsync(grade => grade.Id, grade => grade.Name, cancellationToken);
        var programs = await dbContext.Programs.AsNoTracking()
            .Where(program => program.TeacherAccountId == teacherAccountId
                && programIds.Contains(program.Id))
            .ToDictionaryAsync(program => program.Id, program => program.Name, cancellationToken);
        var locations = await dbContext.Locations.AsNoTracking()
            .Where(location => location.TeacherAccountId == teacherAccountId
                && locationIds.Contains(location.Id))
            .ToDictionaryAsync(location => location.Id, location => location.Name, cancellationToken);
        var memberships = await dbContext.GroupMemberships.AsNoTracking()
            .Where(membership => membership.TeacherAccountId == teacherAccountId
                && groupIds.Contains(membership.GroupId))
            .Select(membership => new
            {
                membership.GroupId,
                membership.JoinedAtUtc,
                membership.LeftAtUtc,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var sourceGroup = row.GroupId.HasValue ? groups[row.GroupId.Value] : null;
            var student = row.StudentId.HasValue ? students[row.StudentId.Value] : null;
            var gradeId = sourceGroup?.SchoolGradeId ?? student!.SchoolGradeId;
            var programId = sourceGroup?.ProgramId ?? student?.ProgramId;
            return new TeacherDashboardSession(
                row.Id,
                (int)row.DeliveryMode,
                row.GroupId,
                row.StudentId,
                sourceGroup?.Name ?? student!.Name,
                grades[gradeId],
                programId,
                programId.HasValue && programs.TryGetValue(programId.Value, out var programName)
                    ? programName
                    : null,
                row.Title,
                row.StartsAtUtc,
                row.EndsAtUtc,
                row.TimeZoneId,
                row.LocationId,
                row.LocationId.HasValue && locations.TryGetValue(row.LocationId.Value, out var locationName)
                    ? locationName
                    : null,
                row.Online,
                (int)row.Status,
                sourceGroup is null
                    ? 1
                    : memberships.Count(membership => membership.GroupId == sourceGroup.Id
                        && membership.JoinedAtUtc <= row.StartsAtUtc
                        && (membership.LeftAtUtc == null || membership.LeftAtUtc > row.StartsAtUtc)));
        }).ToList();
    }

    private static DateTimeOffset LocalMidnightToUtc(DateOnly date, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }
}
