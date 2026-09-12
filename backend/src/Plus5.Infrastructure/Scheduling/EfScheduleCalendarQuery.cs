using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Application.Scheduling;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed class EfScheduleCalendarQuery(Plus5DbContext db, TimeProvider clock) : IScheduleCalendarQuery
{
    public async Task<ScheduleCalendar> GetAsync(
        Guid owner,
        ScheduleCalendarCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(owner, Guid.Empty);
        ArgumentNullException.ThrowIfNull(criteria);

        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var fromUtc = LocalMidnightToUtc(criteria.From, zone);
        var toUtc = LocalMidnightToUtc(criteria.To, zone);
        var range = db.Sessions.AsNoTracking()
            .Where(session => session.TeacherAccountId == owner
                && session.Status != SessionStatus.Cancelled
                && session.StartsAtUtc >= fromUtc
                && session.StartsAtUtc < toUtc);

        var allItems = await Project(range
                .OrderBy(session => session.StartsAtUtc)
                .ThenBy(session => session.EndsAtUtc)
                .ThenBy(session => session.Id), owner)
            .ToListAsync(cancellationToken);

        var filtered = allItems
            .Where(item => !criteria.GroupId.HasValue || item.GroupId == criteria.GroupId)
            .Where(item => !criteria.ProgramId.HasValue || item.ProgramId == criteria.ProgramId)
            .Where(item => !criteria.LocationId.HasValue || item.LocationId == criteria.LocationId)
            .ToList();
        var visibleIds = filtered.Select(item => item.Id).ToArray();

        var groupParticipants = visibleIds.Length == 0
            ? []
            : await (
                from session in db.Sessions.AsNoTracking()
                from membership in db.GroupMemberships.AsNoTracking()
                where visibleIds.Contains(session.Id)
                    && session.TeacherAccountId == owner
                    && session.GroupId.HasValue
                    && membership.TeacherAccountId == owner
                    && membership.GroupId == session.GroupId
                    && membership.JoinedAtUtc <= session.StartsAtUtc
                    && (membership.LeftAtUtc == null || membership.LeftAtUtc > session.StartsAtUtc)
                select new ParticipantOccurrence(session.Id, membership.StudentId))
                .ToListAsync(cancellationToken);

        var uniqueStudents = groupParticipants.Select(item => item.StudentId)
            .Concat(filtered.Where(item => item.StudentId.HasValue).Select(item => item.StudentId!.Value))
            .Distinct()
            .Count();
        var groupSessions = filtered.Count(item => item.DeliveryMode == (int)DeliveryMode.Group);
        var individualSessions = filtered.Count(item => item.DeliveryMode == (int)DeliveryMode.Individual);
        var summary = new ScheduleCalendarSummary(
            groupSessions,
            individualSessions,
            filtered.Count,
            uniqueStudents,
            groupParticipants.Count + individualSessions,
            filtered.Where(item => item.Capacity.HasValue)
                .Sum(item => Math.Max(0, item.Capacity!.Value - item.MemberCount)));

        var reminders = await Project(db.Sessions.AsNoTracking()
                .Where(session => session.TeacherAccountId == owner
                    && session.Status != SessionStatus.Cancelled
                    && session.StartsAtUtc >= clock.GetUtcNow())
                .OrderBy(session => session.StartsAtUtc)
                .ThenBy(session => session.Id)
                .Take(2), owner)
            .ToListAsync(cancellationToken);

        return new ScheduleCalendar(
            GroupScheduleGenerator.TimeZoneId,
            criteria.From,
            criteria.To,
            filtered,
            reminders,
            summary,
            Options(allItems.Where(item => item.GroupId.HasValue)
                .Select(item => new ScheduleCalendarOption(item.GroupId!.Value, item.ContextName))),
            Options(allItems.Where(item => item.ProgramId.HasValue && item.ProgramName is not null)
                .Select(item => new ScheduleCalendarOption(item.ProgramId!.Value, item.ProgramName!))),
            Options(allItems.Where(item => item.LocationId.HasValue && item.LocationName is not null)
                .Select(item => new ScheduleCalendarOption(item.LocationId!.Value, item.LocationName!))));
    }

    private IQueryable<ScheduleCalendarItem> Project(IQueryable<Session> sessions, Guid owner) =>
        sessions.Select(session => new ScheduleCalendarItem(
            session.Id,
            (int)session.DeliveryMode,
            session.GroupId,
            session.StudentId,
            session.GroupId.HasValue
                ? db.Groups.Where(group => group.TeacherAccountId == owner && group.Id == session.GroupId)
                    .Select(group => group.Name).Single()
                : db.Students.Where(student => student.TeacherAccountId == owner && student.Id == session.StudentId)
                    .Select(student => student.FirstName + " " + student.LastName).Single(),
            session.GroupId.HasValue
                ? db.Groups.Where(group => group.TeacherAccountId == owner && group.Id == session.GroupId)
                    .Select(group => (Guid?)group.ProgramId).Single()
                : db.Students.Where(student => student.TeacherAccountId == owner && student.Id == session.StudentId)
                    .Select(student => student.ProgramId).Single(),
            session.GroupId.HasValue
                ? (from sourceGroup in db.Groups
                   join program in db.Programs on sourceGroup.ProgramId equals program.Id
                   where sourceGroup.TeacherAccountId == owner && sourceGroup.Id == session.GroupId
                       && program.TeacherAccountId == owner
                   select program.Name).Single()
                : (from student in db.Students
                   join program in db.Programs on student.ProgramId equals (Guid?)program.Id
                   where student.TeacherAccountId == owner && student.Id == session.StudentId
                       && program.TeacherAccountId == owner
                   select program.Name).SingleOrDefault(),
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.TimeZoneId,
            session.LocationId,
            db.Locations.Where(location => location.TeacherAccountId == owner && location.Id == session.LocationId)
                .Select(location => location.Name).SingleOrDefault(),
            session.OnlineMeetingUrl != null,
            (int)session.Status,
            session.GroupId.HasValue
                ? db.GroupMemberships.Count(membership => membership.TeacherAccountId == owner
                    && membership.GroupId == session.GroupId
                    && membership.JoinedAtUtc <= session.StartsAtUtc
                    && (membership.LeftAtUtc == null || membership.LeftAtUtc > session.StartsAtUtc))
                : 0,
            session.GroupId.HasValue
                ? db.Groups.Where(group => group.TeacherAccountId == owner && group.Id == session.GroupId)
                    .Select(group => (int?)group.Capacity).Single()
                : null));

    private static DateTimeOffset LocalMidnightToUtc(DateOnly date, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }

    private static List<ScheduleCalendarOption> Options(IEnumerable<ScheduleCalendarOption> source) =>
        source.GroupBy(item => item.Id)
            .Select(group => group.First())
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Id)
            .ToList();

    private sealed record ParticipantOccurrence(Guid SessionId, Guid StudentId);
}
