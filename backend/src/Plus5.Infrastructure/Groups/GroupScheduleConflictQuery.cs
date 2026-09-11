using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

internal static class GroupScheduleConflictQuery
{
    // Check canonical rules too: an older open-ended series may not yet have materialized this window.
    // Existing occurrences (including cancelled/rescheduled exceptions) are authoritative over their rule.
    public static async Task<bool> HasUnmaterializedConflictAsync(Plus5DbContext db, Guid owner, Guid? locationId,
        IReadOnlyList<GroupOccurrence> occurrences, CancellationToken cancellationToken)
    {
        if (occurrences.Count == 0) return false;
        var first = occurrences.Min(o => o.Date);
        var last = occurrences.Max(o => o.Date);
        var lower = DateOnly.FromDayNumber(Math.Max(0, first.DayNumber - 2));
        var upper = DateOnly.FromDayNumber(Math.Min(DateOnly.MaxValue.DayNumber, last.DayNumber + 2));
        var query = db.RecurringSessionSeries.AsNoTracking().Where(s => (s.TeacherAccountId == owner
                || (locationId.HasValue && s.LocationId == locationId)) && s.StartsOn <= upper && (s.EndsOn == null || s.EndsOn >= lower))
            .OrderBy(s => s.Id).Select(s => new { s.Id, s.StartsOn, s.EndsOn, s.DayOfWeek, s.LocalStartTime, s.LocalEndTime, s.TimeZoneId });
        for (var offset = 0; ; offset = checked(offset + 100))
        {
            var rules = await query.Skip(offset).Take(100).ToListAsync(cancellationToken);
            foreach (var rule in rules)
            {
                TimeZoneInfo zone;
                try { zone = TimeZoneInfo.FindSystemTimeZoneById(rule.TimeZoneId); }
                catch (TimeZoneNotFoundException) { return true; }
                catch (InvalidTimeZoneException) { return true; }
                foreach (var candidate in occurrences)
                {
                    var startDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(candidate.Start, zone).DateTime);
                    var endDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(candidate.End, zone).DateTime);
                    for (var day = startDate.DayNumber; day <= endDate.DayNumber; day++)
                    {
                        var date = DateOnly.FromDayNumber(day);
                        if (date.DayOfWeek != rule.DayOfWeek || date < rule.StartsOn || (rule.EndsOn.HasValue && date > rule.EndsOn)) continue;
                        var start = date.ToDateTime(rule.LocalStartTime);
                        var end = date.ToDateTime(rule.LocalEndTime);
                        var invalid = zone.IsInvalidTime(start) || zone.IsAmbiguousTime(start) || zone.IsInvalidTime(end) || zone.IsAmbiguousTime(end);
                        if (!invalid && !(new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone)) < candidate.End
                            && new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end, zone)) > candidate.Start)) continue;
                        if (!await db.Sessions.AnyAsync(s => s.RecurringSessionSeriesId == rule.Id && s.SeriesOccurrenceDate == date, cancellationToken)) return true;
                    }
                }
            }
            if (rules.Count < 100) return false;
        }
    }
}
