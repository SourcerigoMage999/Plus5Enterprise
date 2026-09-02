using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupQuery(Plus5DbContext db, TimeProvider clock) : IGroupQuery
{
    private IQueryable<Group> Owned(Guid owner) => db.Groups.AsNoTracking()
        .Where(group => group.TeacherAccountId == owner && group.ArchivedAtUtc == null);

    public async Task<GroupPage<GroupItem>> GetPageAsync(Guid owner, GroupCriteria criteria, CancellationToken cancellationToken)
    {
        Validate(criteria);
        var groups = Owned(owner);
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            groups = groups.Where(group => group.Name.Contains(search));
        }
        if (criteria.ProgramId.HasValue) groups = groups.Where(group => group.ProgramId == criteria.ProgramId);
        if (criteria.Status.HasValue) groups = groups.Where(group => (int)group.Status == criteria.Status);
        var total = await groups.LongCountAsync(cancellationToken);
        var skip = ((long)criteria.Page - 1) * criteria.PageSize;
        var items = skip > int.MaxValue ? [] : await Project(groups.OrderBy(group => group.Name).ThenBy(group => group.Id)
            .Skip((int)skip).Take(criteria.PageSize), owner).ToListAsync(cancellationToken);
        return new(items, criteria.Page, criteria.PageSize, total);
    }

    public Task<GroupItem?> GetAsync(Guid owner, Guid groupId, CancellationToken cancellationToken) =>
        Project(Owned(owner).Where(group => group.Id == groupId), owner).SingleOrDefaultAsync(cancellationToken);

    private IQueryable<GroupItem> Project(IQueryable<Group> groups, Guid owner)
    {
        var today = LocalToday();
        return from entry in groups
               join program in db.Programs.AsNoTracking().Where(program => program.TeacherAccountId == owner) on entry.ProgramId equals program.Id
               join grade in db.SchoolGrades.AsNoTracking() on entry.SchoolGradeId equals grade.Id
               select new GroupItem(entry.Id, entry.Name, program.Id, program.Name, grade.Id, grade.Name,
                   (int)entry.Status, entry.Capacity,
                   db.GroupMemberships.Count(member => member.TeacherAccountId == owner && member.GroupId == entry.Id && member.LeftAtUtc == null),
                   entry.RowVersion,
                   db.RecurringSessionSeries.AsNoTracking()
                       .Where(series => series.TeacherAccountId == owner && series.GroupId == entry.Id
                           && series.StartsOn <= today && series.EndsOn >= today)
                       .OrderBy(series => series.DayOfWeek).ThenBy(series => series.LocalStartTime).ThenBy(series => series.Id)
                       .Take(14)
                       .Select(series => new GroupSlot((int)series.DayOfWeek, series.LocalStartTime, series.LocalEndTime,
                           series.TimeZoneId, db.Locations.Where(location => location.TeacherAccountId == owner && location.Id == series.LocationId)
                               .Select(location => location.Name).FirstOrDefault(), series.OnlineMeetingUrl != null)).ToList());
    }

    public async Task<GroupOverview> GetOverviewAsync(Guid owner, CancellationToken cancellationToken)
    {
        var groups = Owned(owner);
        var active = groups.Where(group => group.Status == GroupStatus.Active);
        var today = LocalToday();
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");
        var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(monday.ToDateTime(TimeOnly.MinValue), zone));
        var end = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(monday.AddDays(7).ToDateTime(TimeOnly.MinValue), zone));
        var memberCount = await db.GroupMemberships.LongCountAsync(member => member.TeacherAccountId == owner
            && member.LeftAtUtc == null && groups.Any(group => group.Id == member.GroupId), cancellationToken);
        var seats = await active.Select(group => (long?)group.Capacity - db.GroupMemberships.LongCount(member =>
            member.TeacherAccountId == owner && member.GroupId == group.Id && member.LeftAtUtc == null)).SumAsync(cancellationToken) ?? 0;
        var sessions = await db.Sessions.LongCountAsync(session => session.TeacherAccountId == owner
            && session.StartsAtUtc >= start && session.StartsAtUtc < end && session.Status != SessionStatus.Cancelled
            && groups.Any(group => group.Id == session.GroupId), cancellationToken);
        return new(await groups.LongCountAsync(cancellationToken), await active.LongCountAsync(cancellationToken), memberCount, seats, sessions, monday);
    }

    public async Task<GroupPage<GroupStudent>?> GetStudentsAsync(Guid owner, Guid groupId, GroupCriteria criteria, bool candidates, CancellationToken cancellationToken)
    {
        Validate(criteria);
        var group = await Owned(owner).Select(group => new { group.Id, group.ProgramId, group.SchoolGradeId })
            .SingleOrDefaultAsync(group => group.Id == groupId, cancellationToken);
        if (group is null) return null;
        var students = db.Students.AsNoTracking().Where(student => student.TeacherAccountId == owner && student.ArchivedAtUtc == null);
        students = candidates
            ? students.Where(student => !db.GroupMemberships.Any(member => member.TeacherAccountId == owner && member.StudentId == student.Id && member.LeftAtUtc == null))
            : students.Where(student => db.GroupMemberships.Any(member => member.TeacherAccountId == owner && member.GroupId == groupId && member.StudentId == student.Id && member.LeftAtUtc == null));
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            students = students.Where(student => (student.FirstName + " " + student.LastName).Contains(search));
        }
        var total = await students.LongCountAsync(cancellationToken);
        var skip = ((long)criteria.Page - 1) * criteria.PageSize;
        var items = skip > int.MaxValue ? [] : await students
            .OrderByDescending(student => candidates && student.ProgramId == group.ProgramId && student.SchoolGradeId == group.SchoolGradeId)
            .ThenBy(student => student.LastName).ThenBy(student => student.FirstName).ThenBy(student => student.Id)
            .Skip((int)skip).Take(criteria.PageSize)
            .Select(student => new GroupStudent(student.Id, student.FirstName, student.LastName,
                db.SchoolGrades.Where(grade => grade.Id == student.SchoolGradeId).Select(grade => grade.Name).Single(),
                student.ProgramId == group.ProgramId && student.SchoolGradeId == group.SchoolGradeId, student.RowVersion))
            .ToListAsync(cancellationToken);
        return new(items, criteria.Page, criteria.PageSize, total);
    }

    public async Task<GroupPage<GroupSession>?> GetSessionsAsync(Guid owner, Guid groupId, GroupCriteria criteria, CancellationToken cancellationToken)
    {
        Validate(criteria);
        if (!await Owned(owner).AnyAsync(group => group.Id == groupId, cancellationToken)) return null;
        var now = clock.GetUtcNow();
        var sessions = db.Sessions.AsNoTracking().Where(session => session.TeacherAccountId == owner && session.GroupId == groupId
            && session.EndsAtUtc >= now && session.Status != SessionStatus.Cancelled);
        var total = await sessions.LongCountAsync(cancellationToken);
        var skip = ((long)criteria.Page - 1) * criteria.PageSize;
        var items = skip > int.MaxValue ? [] : await sessions.OrderBy(session => session.StartsAtUtc).ThenBy(session => session.Id)
            .Skip((int)skip).Take(criteria.PageSize).Select(session => new GroupSession(session.Id, session.StartsAtUtc,
                session.EndsAtUtc, session.TimeZoneId, db.Locations.Where(location => location.TeacherAccountId == owner && location.Id == session.LocationId)
                    .Select(location => location.Name).FirstOrDefault(), session.OnlineMeetingUrl != null, (int)session.Status)).ToListAsync(cancellationToken);
        return new(items, criteria.Page, criteria.PageSize, total);
    }

    private DateOnly LocalToday() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb")).DateTime);

    private static void Validate(GroupCriteria criteria)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(criteria.Page, 1);
        if (criteria.PageSize is < 1 or > 100 || criteria.Search?.Length > 100 || criteria.Status is < 1 or > 3 || criteria.ProgramId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(criteria));
    }
}
