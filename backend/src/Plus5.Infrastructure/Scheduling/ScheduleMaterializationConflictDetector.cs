using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Scheduling;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

internal static class ScheduleMaterializationConflictDetector
{
    private const int RuleBatchSize = 100;

    public static async Task<Dictionary<DateOnly, ScheduleMaterializationConflictKind>> FindAsync(
        Plus5DbContext db,
        RecurringSessionSeries series,
        IReadOnlyList<ScheduleMaterializationOccurrence> occurrences,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<DateOnly, ScheduleMaterializationConflictKind>();
        if (occurrences.Count == 0)
        {
            return result;
        }

        var lowerUtc = occurrences.Min(item => item.Start);
        var upperUtc = occurrences.Max(item => item.End);
        var actualSessions = await db.Sessions.AsNoTracking()
            .Where(session => session.Status != SessionStatus.Cancelled
                && session.StartsAtUtc < upperUtc
                && session.EndsAtUtc > lowerUtc
                && (session.TeacherAccountId == series.TeacherAccountId
                    || series.LocationId.HasValue && session.LocationId == series.LocationId))
            .Select(session => new ActualSession(
                session.TeacherAccountId,
                session.LocationId,
                session.StartsAtUtc,
                session.EndsAtUtc))
            .ToListAsync(cancellationToken);

        foreach (var occurrence in occurrences)
        {
            foreach (var session in actualSessions.Where(item =>
                         item.Start < occurrence.End && item.End > occurrence.Start))
            {
                if (session.TeacherAccountId == series.TeacherAccountId)
                {
                    AddConflict(result, occurrence.Date, ScheduleMaterializationConflictKind.Teacher);
                }

                if (series.LocationId.HasValue && session.LocationId == series.LocationId)
                {
                    AddConflict(result, occurrence.Date, ScheduleMaterializationConflictKind.Location);
                }
            }
        }

        var lowerDate = DateOnly.FromDayNumber(Math.Max(0, occurrences.Min(item => item.Date).DayNumber - 2));
        var upperDate = DateOnly.FromDayNumber(Math.Min(
            DateOnly.MaxValue.DayNumber,
            occurrences.Max(item => item.Date).DayNumber + 2));

        for (var offset = 0; ; offset = checked(offset + RuleBatchSize))
        {
            var rules = await db.RecurringSessionSeries.AsNoTracking()
                .Where(rule => rule.Id != series.Id
                    && rule.SupersededAtUtc == null
                    && rule.StartsOn <= upperDate
                    && (rule.EndsOn == null || rule.EndsOn >= lowerDate)
                    && (rule.TeacherAccountId == series.TeacherAccountId
                        || series.LocationId.HasValue && rule.LocationId == series.LocationId))
                .OrderBy(rule => rule.Id)
                .Skip(offset)
                .Take(RuleBatchSize)
                .Select(rule => new CompetingRule(
                    rule.Id,
                    rule.TeacherAccountId,
                    rule.LocationId,
                    rule.StartsOn,
                    rule.EndsOn,
                    rule.DayOfWeek,
                    rule.LocalStartTime,
                    rule.LocalEndTime,
                    rule.TimeZoneId))
                .ToListAsync(cancellationToken);
            if (rules.Count == 0)
            {
                break;
            }

            var ruleIds = rules.Select(rule => rule.Id).ToArray();
            var materialized = (await db.Sessions.AsNoTracking()
                    .Where(session => session.RecurringSessionSeriesId.HasValue
                        && ruleIds.Contains(session.RecurringSessionSeriesId.Value)
                        && session.SeriesOccurrenceDate.HasValue
                        && session.SeriesOccurrenceDate.Value >= lowerDate
                        && session.SeriesOccurrenceDate.Value <= upperDate)
                    .Select(session => new
                    {
                        SeriesId = session.RecurringSessionSeriesId!.Value,
                        Date = session.SeriesOccurrenceDate!.Value,
                    })
                    .ToListAsync(cancellationToken))
                .Select(item => (item.SeriesId, item.Date))
                .ToHashSet();

            foreach (var rule in rules)
            {
                EvaluateRuleConflicts(series, occurrences, rule, materialized, result);
            }

            if (rules.Count < RuleBatchSize)
            {
                break;
            }
        }

        return result;
    }

    private static void EvaluateRuleConflicts(
        RecurringSessionSeries series,
        IReadOnlyList<ScheduleMaterializationOccurrence> occurrences,
        CompetingRule rule,
        HashSet<(Guid SeriesId, DateOnly Date)> materialized,
        Dictionary<DateOnly, ScheduleMaterializationConflictKind> result)
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(rule.TimeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            foreach (var occurrence in occurrences)
            {
                AddRelevantConflicts(series, rule, occurrence.Date, result);
            }

            return;
        }

        foreach (var occurrence in occurrences)
        {
            var startDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurrence.Start, zone).DateTime);
            var endDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurrence.End, zone).DateTime);
            for (var day = startDate.DayNumber; day <= endDate.DayNumber; day++)
            {
                var date = DateOnly.FromDayNumber(day);
                if (date.DayOfWeek != rule.DayOfWeek
                    || date < rule.StartsOn
                    || rule.EndsOn.HasValue && date > rule.EndsOn.Value
                    || materialized.Contains((rule.Id, date)))
                {
                    continue;
                }

                var localStart = date.ToDateTime(rule.LocalStartTime, DateTimeKind.Unspecified);
                var localEnd = date.ToDateTime(rule.LocalEndTime, DateTimeKind.Unspecified);
                var invalid = zone.IsInvalidTime(localStart)
                    || zone.IsAmbiguousTime(localStart)
                    || zone.IsInvalidTime(localEnd)
                    || zone.IsAmbiguousTime(localEnd);
                var overlaps = !invalid
                    && new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone)) < occurrence.End
                    && new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, zone)) > occurrence.Start;
                if (invalid || overlaps)
                {
                    AddRelevantConflicts(series, rule, occurrence.Date, result);
                }
            }
        }
    }

    private static void AddRelevantConflicts(
        RecurringSessionSeries series,
        CompetingRule rule,
        DateOnly occurrenceDate,
        Dictionary<DateOnly, ScheduleMaterializationConflictKind> result)
    {
        if (rule.TeacherAccountId == series.TeacherAccountId)
        {
            AddConflict(result, occurrenceDate, ScheduleMaterializationConflictKind.Teacher);
        }

        if (series.LocationId.HasValue && rule.LocationId == series.LocationId)
        {
            AddConflict(result, occurrenceDate, ScheduleMaterializationConflictKind.Location);
        }
    }

    private static void AddConflict(
        Dictionary<DateOnly, ScheduleMaterializationConflictKind> result,
        DateOnly date,
        ScheduleMaterializationConflictKind conflict) =>
        result[date] = result.GetValueOrDefault(date) | conflict;

    private sealed record ActualSession(
        Guid TeacherAccountId,
        Guid? LocationId,
        DateTimeOffset Start,
        DateTimeOffset End);

    private sealed record CompetingRule(
        Guid Id,
        Guid TeacherAccountId,
        Guid? LocationId,
        DateOnly StartsOn,
        DateOnly? EndsOn,
        DayOfWeek DayOfWeek,
        TimeOnly LocalStartTime,
        TimeOnly LocalEndTime,
        string TimeZoneId);
}

[Flags]
internal enum ScheduleMaterializationConflictKind
{
    None = 0,
    Teacher = 1,
    Location = 2,
}

internal sealed record ScheduleMaterializationOccurrence(
    DateOnly Date,
    DateTimeOffset Start,
    DateTimeOffset End);
