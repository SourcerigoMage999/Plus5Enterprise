using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupEditingQuery(Plus5DbContext db, TimeProvider clock) : IGroupEditingQuery
{
    public Task<GroupEditItem?> GetAsync(Guid owner, Guid groupId, CancellationToken cancellationToken)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);
        return db.Groups.AsNoTracking()
            .Where(item => item.TeacherAccountId == owner && item.Id == groupId && item.ArchivedAtUtc == null)
            .Select(item => new GroupEditItem(item.Id, item.Name, item.Description, item.ProgramId,
                db.Programs.Where(program => program.TeacherAccountId == owner && program.Id == item.ProgramId)
                    .Select(program => program.Name).Single(), item.SchoolGradeId,
                db.SchoolGrades.Where(grade => grade.Id == item.SchoolGradeId).Select(grade => grade.Name).Single(),
                (int)item.Status, item.Capacity,
                db.GroupMemberships.Count(member => member.TeacherAccountId == owner
                    && member.GroupId == item.Id && member.LeftAtUtc == null), item.RowVersion,
                db.RecurringSessionSeries.AsNoTracking()
                    .Where(series => series.TeacherAccountId == owner && series.GroupId == item.Id
                        && series.SupersededAtUtc == null && (series.EndsOn == null || series.EndsOn >= today))
                    .OrderBy(series => series.DayOfWeek).ThenBy(series => series.LocalStartTime).ThenBy(series => series.Id)
                    .Take(14)
                    .Select(series => new GroupEditSlot(series.Id, (int)series.DayOfWeek,
                        series.LocalStartTime, series.LocalEndTime, series.StartsOn, series.EndsOn,
                        series.LocationId, db.Locations.Where(location => location.TeacherAccountId == owner
                            && location.Id == series.LocationId).Select(location => location.Name).FirstOrDefault()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
