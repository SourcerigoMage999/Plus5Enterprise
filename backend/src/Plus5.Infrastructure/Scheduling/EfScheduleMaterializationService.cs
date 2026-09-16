using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plus5.Application.Groups;
using Plus5.Application.Scheduling;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed partial class EfScheduleMaterializationService(
    DbContextOptions<Plus5DbContext> dbOptions,
    TimeProvider clock,
    ILogger<EfScheduleMaterializationService> logger)
    : IScheduleMaterializationService
{
    internal const int SeriesBatchSize = 50;
    private readonly SqlScheduleMaterializationLeaseManager leaseManager = new(dbOptions, clock);

    public async Task<ScheduleMaterializationRunResult> RunAsync(
        string leaseOwnerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwnerId);
        if (leaseOwnerId.Length > ScheduleMaterializationLease.OwnerIdMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseOwnerId));
        }

        var acquired = await ExecuteWithTransientRetryAsync(
            token => leaseManager.TryAcquireAsync(leaseOwnerId, token),
            cancellationToken);
        if (!acquired)
        {
            LogLeaseSkipped(logger);
            return EmptyResult(ScheduleMaterializationRunStatus.LeaseSkipped);
        }

        LogLeaseAcquired(logger, leaseOwnerId);
        var totals = new MaterializationTotals();

        try
        {
            for (var offset = 0; ; offset = checked(offset + SeriesBatchSize))
            {
                if (!await ExecuteWithTransientRetryAsync(
                        token => leaseManager.TryRenewAsync(leaseOwnerId, token),
                        cancellationToken))
                {
                    LogLeaseLost(logger, leaseOwnerId);
                    return totals.ToResult(ScheduleMaterializationRunStatus.LeaseLost);
                }

                var seriesIds = await ExecuteWithTransientRetryAsync(
                    token => GetSeriesBatchAsync(offset, token),
                    cancellationToken);
                if (seriesIds.Count == 0)
                {
                    break;
                }

                foreach (var seriesId in seriesIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await ExecuteWithTransientRetryAsync(
                            token => leaseManager.TryRenewAsync(leaseOwnerId, token),
                            cancellationToken))
                    {
                        LogLeaseLost(logger, leaseOwnerId);
                        return totals.ToResult(ScheduleMaterializationRunStatus.LeaseLost);
                    }

                    var outcome = await ProcessSeriesSafelyAsync(seriesId, cancellationToken);
                    totals.Add(outcome);
                }

                if (seriesIds.Count < SeriesBatchSize)
                {
                    break;
                }
            }

            return totals.ToResult(ScheduleMaterializationRunStatus.Completed);
        }
        finally
        {
            await ReleaseLeaseSafelyAsync(leaseOwnerId);
        }
    }

    private async Task ReleaseLeaseSafelyAsync(string ownerId)
    {
        try
        {
            await ExecuteWithTransientRetryAsync(async cancellationToken =>
            {
                await leaseManager.ReleaseAsync(ownerId, cancellationToken);
                return true;
            }, CancellationToken.None);
        }
        catch (Exception exception)
        {
            LogLeaseReleaseFailed(logger, exception);
        }
    }

    private async Task<IReadOnlyList<Guid>> GetSeriesBatchAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        return await db.RecurringSessionSeries.AsNoTracking()
            .Where(series => series.EndsOn == null && series.SupersededAtUtc == null)
            .Where(series => series.Kind == RecurringSessionSeriesKind.RegularGroupSchedule
                ? db.Groups.Any(group => group.Id == series.GroupId
                    && group.TeacherAccountId == series.TeacherAccountId
                    && group.Status == GroupStatus.Active
                    && group.ArchivedAtUtc == null)
                : db.Students.Any(student => student.Id == series.StudentId
                    && student.TeacherAccountId == series.TeacherAccountId
                    && student.Status != StudentStatus.Inactive
                    && student.ArchivedAtUtc == null))
            .OrderBy(series => series.CreatedAtUtc)
            .ThenBy(series => series.Id)
            .Skip(offset)
            .Take(SeriesBatchSize)
            .Select(series => series.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<SeriesOutcome> ProcessSeriesSafelyAsync(
        Guid seriesId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ScheduleMaterializationRetryPolicy.MaxAttempts; attempt++)
        {
            try
            {
                return await ProcessSeriesAsync(seriesId, cancellationToken);
            }
            catch (Exception exception) when (ScheduleMaterializationRetryPolicy.IsTransient(exception)
                && attempt < ScheduleMaterializationRetryPolicy.MaxAttempts)
            {
                LogTransientRetry(logger, seriesId, attempt, exception);
                await Task.Delay(ScheduleMaterializationRetryPolicy.DelayAfter(attempt), clock, cancellationToken);
            }
            catch (Exception exception) when (ScheduleMaterializationRetryPolicy.IsTransient(exception))
            {
                LogSeriesFailed(logger, seriesId, exception);
                if (ScheduleMaterializationRetryPolicy.IsDatabaseUnavailable(exception))
                {
                    throw;
                }

                return await RecordSeriesFailureSafelyAsync(
                    seriesId,
                    ScheduleMaterializationIssueType.ConcurrencyFailure,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogSeriesFailed(logger, seriesId, exception);
                return await RecordSeriesFailureSafelyAsync(
                    seriesId,
                    ScheduleMaterializationIssueType.MaterializationFailure,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException("The bounded retry loop exited unexpectedly.");
    }

    private async Task<SeriesOutcome> ProcessSeriesAsync(
        Guid seriesId,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var series = await db.RecurringSessionSeries.SingleOrDefaultAsync(
            item => item.Id == seriesId
                && item.EndsOn == null
                && item.SupersededAtUtc == null,
            cancellationToken);
        if (series is null || !await ContextIsActiveAsync(db, series, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return new SeriesOutcome(1, 0, 0, 0, 0, 0);
        }

        var now = clock.GetUtcNow();
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(series.TimeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            var fallbackDate = DateOnly.FromDateTime(now.UtcDateTime);
            var fallbackIssues = await db.ScheduleMaterializationIssues
                .Where(issue => issue.RecurringSessionSeriesId == series.Id
                    && issue.OccurrenceLocalDate == fallbackDate)
                .ToListAsync(cancellationToken);
            var outcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                db,
                series.Id,
                fallbackDate,
                [ScheduleMaterializationIssueType.MaterializationFailure],
                now,
                fallbackIssues);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SeriesOutcome(1, 0, 1, 0, outcome.Recorded, outcome.Resolved);
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var lastDayNumber = Math.Min(
            DateOnly.MaxValue.DayNumber,
            (long)today.DayNumber + GroupScheduleGenerator.HorizonDays - 1);
        var dates = Enumerable.Range(0, checked((int)(lastDayNumber - today.DayNumber + 1)))
            .Select(offset => today.AddDays(offset))
            .Where(date => date >= series.StartsOn && date.DayOfWeek == series.DayOfWeek)
            .ToArray();
        if (dates.Length == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SeriesOutcome(1, 0, 0, 0, 0, 0);
        }

        var existingSessions = await db.Sessions
            .Where(session => session.RecurringSessionSeriesId == series.Id
                && session.SeriesOccurrenceDate.HasValue
                && dates.Contains(session.SeriesOccurrenceDate.Value))
            .ToListAsync(cancellationToken);
        var existingDates = existingSessions
            .Select(session => session.SeriesOccurrenceDate!.Value)
            .ToHashSet();
        var issues = await db.ScheduleMaterializationIssues
            .Where(issue => issue.RecurringSessionSeriesId == series.Id
                && dates.Contains(issue.OccurrenceLocalDate))
            .ToListAsync(cancellationToken);
        var titleTemplate = await db.Sessions.AsNoTracking()
            .Where(session => session.RecurringSessionSeriesId == series.Id
                && !session.IsSeriesException)
            .OrderByDescending(session => session.SeriesOccurrenceDate)
            .Select(session => new SessionTemplate(session.Title, session.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        var skipped = 0;
        var recorded = 0;
        var resolved = 0;
        var validOccurrences = new List<ScheduleMaterializationOccurrence>();
        foreach (var date in dates)
        {
            if (existingDates.Contains(date))
            {
                skipped++;
                var existingOutcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                    db, series.Id, date, [], now, issues);
                recorded += existingOutcome.Recorded;
                resolved += existingOutcome.Resolved;
                continue;
            }

            var localStart = date.ToDateTime(series.LocalStartTime, DateTimeKind.Unspecified);
            var localEnd = date.ToDateTime(series.LocalEndTime, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localStart) || zone.IsInvalidTime(localEnd))
            {
                skipped++;
                var invalidOutcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                    db,
                    series.Id,
                    date,
                    [ScheduleMaterializationIssueType.InvalidLocalTime],
                    now,
                    issues);
                recorded += invalidOutcome.Recorded;
                resolved += invalidOutcome.Resolved;
                continue;
            }

            if (zone.IsAmbiguousTime(localStart) || zone.IsAmbiguousTime(localEnd))
            {
                skipped++;
                var ambiguousOutcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                    db,
                    series.Id,
                    date,
                    [ScheduleMaterializationIssueType.AmbiguousLocalTime],
                    now,
                    issues);
                recorded += ambiguousOutcome.Recorded;
                resolved += ambiguousOutcome.Resolved;
                continue;
            }

            var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone));
            var end = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, zone));
            if (start < now)
            {
                skipped++;
                continue;
            }

            validOccurrences.Add(new ScheduleMaterializationOccurrence(date, start, end));
        }

        var conflicts = await ScheduleMaterializationConflictDetector.FindAsync(
            db,
            series,
            validOccurrences,
            cancellationToken);
        var contextId = series.GroupId ?? series.StudentId!.Value;
        var deliveryMode = series.Kind == RecurringSessionSeriesKind.RegularGroupSchedule
            ? DeliveryMode.Group
            : DeliveryMode.Individual;
        var created = 0;
        var conflictCount = 0;

        foreach (var occurrence in validOccurrences)
        {
            var conflict = conflicts.GetValueOrDefault(occurrence.Date);
            var activeIssues = new List<ScheduleMaterializationIssueType>(2);
            if ((conflict & ScheduleMaterializationConflictKind.Teacher) != 0)
            {
                activeIssues.Add(ScheduleMaterializationIssueType.TeacherConflict);
            }

            if ((conflict & ScheduleMaterializationConflictKind.Location) != 0)
            {
                activeIssues.Add(ScheduleMaterializationIssueType.LocationConflict);
            }

            if (activeIssues.Count > 0)
            {
                skipped++;
                conflictCount++;
                var conflictOutcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                    db,
                    series.Id,
                    occurrence.Date,
                    activeIssues,
                    now,
                    issues);
                recorded += conflictOutcome.Recorded;
                resolved += conflictOutcome.Resolved;
                continue;
            }

            db.Sessions.Add(new Session(
                Guid.NewGuid(),
                series.TeacherAccountId,
                deliveryMode,
                contextId,
                occurrence.Start,
                occurrence.End,
                series.TimeZoneId,
                now,
                titleTemplate?.Title,
                titleTemplate?.Notes,
                series.LocationId,
                series.OnlineMeetingUrl,
                series.Id,
                occurrence.Date));
            created++;
            var successOutcome = ScheduleMaterializationIssueTracker.ApplyOutcome(
                db, series.Id, occurrence.Date, [], now, issues);
            recorded += successOutcome.Recorded;
            resolved += successOutcome.Resolved;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SeriesOutcome(1, created, skipped, conflictCount, recorded, resolved);
    }

    private async Task<SeriesOutcome> RecordSeriesFailureSafelyAsync(
        Guid seriesId,
        ScheduleMaterializationIssueType issueType,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteWithTransientRetryAsync(
                token => RecordSeriesFailureAsync(seriesId, issueType, token),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogIssuePersistenceFailed(logger, seriesId, exception);
            return new SeriesOutcome(1, 0, 0, 0, 0, 0);
        }
    }

    private async Task<SeriesOutcome> RecordSeriesFailureAsync(
        Guid seriesId,
        ScheduleMaterializationIssueType issueType,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(dbOptions);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var series = await db.RecurringSessionSeries.SingleOrDefaultAsync(
            item => item.Id == seriesId && item.EndsOn == null && item.SupersededAtUtc == null,
            cancellationToken);
        if (series is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SeriesOutcome(1, 0, 0, 0, 0, 0);
        }

        var now = clock.GetUtcNow();
        DateOnly today;
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(series.TimeZoneId);
            today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            today = DateOnly.FromDateTime(now.UtcDateTime);
        }

        var last = DateOnly.FromDayNumber((int)Math.Min(
            DateOnly.MaxValue.DayNumber,
            (long)today.DayNumber + GroupScheduleGenerator.HorizonDays - 1));
        var dates = Enumerable.Range(0, last.DayNumber - today.DayNumber + 1)
            .Select(offset => today.AddDays(offset))
            .Where(date => date >= series.StartsOn && date <= last && date.DayOfWeek == series.DayOfWeek)
            .ToArray();
        var existingDates = await db.Sessions.AsNoTracking()
            .Where(session => session.RecurringSessionSeriesId == seriesId
                && session.SeriesOccurrenceDate.HasValue
                && dates.Contains(session.SeriesOccurrenceDate.Value))
            .Select(session => session.SeriesOccurrenceDate!.Value)
            .ToListAsync(cancellationToken);
        var missingDates = dates.Except(existingDates).ToArray();
        var issues = await db.ScheduleMaterializationIssues
            .Where(issue => issue.RecurringSessionSeriesId == seriesId
                && missingDates.Contains(issue.OccurrenceLocalDate))
            .ToListAsync(cancellationToken);
        var recorded = 0;
        foreach (var date in missingDates)
        {
            var issue = issues.SingleOrDefault(item =>
                item.OccurrenceLocalDate == date && item.IssueType == issueType);
            if (issue is null)
            {
                issue = new ScheduleMaterializationIssue(
                    Guid.NewGuid(),
                    seriesId,
                    date,
                    issueType,
                    now);
                db.ScheduleMaterializationIssues.Add(issue);
                issues.Add(issue);
            }
            else
            {
                issue.RecordAgain(now);
            }

            recorded++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SeriesOutcome(1, 0, missingDates.Length, 0, recorded, 0);
    }

    private static Task<bool> ContextIsActiveAsync(
        Plus5DbContext db,
        RecurringSessionSeries series,
        CancellationToken cancellationToken) =>
        series.Kind == RecurringSessionSeriesKind.RegularGroupSchedule
            ? db.Groups.AnyAsync(group => group.Id == series.GroupId
                && group.TeacherAccountId == series.TeacherAccountId
                && group.Status == GroupStatus.Active
                && group.ArchivedAtUtc == null,
                cancellationToken)
            : db.Students.AnyAsync(student => student.Id == series.StudentId
                && student.TeacherAccountId == series.TeacherAccountId
                && student.Status != StudentStatus.Inactive
                && student.ArchivedAtUtc == null,
                cancellationToken);

    private async Task<T> ExecuteWithTransientRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ScheduleMaterializationRetryPolicy.MaxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception exception) when (ScheduleMaterializationRetryPolicy.IsTransient(exception)
                && attempt < ScheduleMaterializationRetryPolicy.MaxAttempts)
            {
                LogInfrastructureRetry(logger, attempt, exception);
                await Task.Delay(ScheduleMaterializationRetryPolicy.DelayAfter(attempt), clock, cancellationToken);
            }
        }

        throw new InvalidOperationException("The bounded retry loop exited unexpectedly.");
    }

    private static ScheduleMaterializationRunResult EmptyResult(
        ScheduleMaterializationRunStatus status) => new(status, 0, 0, 0, 0, 0, 0);

    [LoggerMessage(4600, LogLevel.Information,
        "Schedule materialization lease acquired by {LeaseOwnerId}.")]
    private static partial void LogLeaseAcquired(ILogger logger, string leaseOwnerId);

    [LoggerMessage(4601, LogLevel.Information,
        "Schedule materialization run skipped because another instance owns the lease.")]
    private static partial void LogLeaseSkipped(ILogger logger);

    [LoggerMessage(4602, LogLevel.Warning,
        "Schedule materialization lease was lost by {LeaseOwnerId}; the run is stopping.")]
    private static partial void LogLeaseLost(ILogger logger, string leaseOwnerId);

    [LoggerMessage(4603, LogLevel.Warning,
        "Schedule materialization transient retry {Attempt} for series {SeriesId}.")]
    private static partial void LogTransientRetry(
        ILogger logger,
        Guid seriesId,
        int attempt,
        Exception exception);

    [LoggerMessage(4604, LogLevel.Warning,
        "Schedule materialization infrastructure retry {Attempt}.")]
    private static partial void LogInfrastructureRetry(
        ILogger logger,
        int attempt,
        Exception exception);

    [LoggerMessage(4605, LogLevel.Error,
        "Schedule materialization failed for series {SeriesId}.")]
    private static partial void LogSeriesFailed(
        ILogger logger,
        Guid seriesId,
        Exception exception);

    [LoggerMessage(4606, LogLevel.Error,
        "Schedule materialization issue persistence failed for series {SeriesId}.")]
    private static partial void LogIssuePersistenceFailed(
        ILogger logger,
        Guid seriesId,
        Exception exception);

    [LoggerMessage(4607, LogLevel.Warning,
        "Schedule materialization lease release failed; expiry will recover ownership.")]
    private static partial void LogLeaseReleaseFailed(ILogger logger, Exception exception);

    private sealed record SessionTemplate(string? Title, string? Notes);

    private sealed record SeriesOutcome(
        int SeriesScanned,
        int SessionsCreated,
        int OccurrencesSkipped,
        int ConflictsFound,
        int IssuesRecorded,
        int IssuesResolved);

    private sealed class MaterializationTotals
    {
        private int seriesScanned;
        private int sessionsCreated;
        private int occurrencesSkipped;
        private int conflictsFound;
        private int issuesRecorded;
        private int issuesResolved;

        public void Add(SeriesOutcome outcome)
        {
            seriesScanned = checked(seriesScanned + outcome.SeriesScanned);
            sessionsCreated = checked(sessionsCreated + outcome.SessionsCreated);
            occurrencesSkipped = checked(occurrencesSkipped + outcome.OccurrencesSkipped);
            conflictsFound = checked(conflictsFound + outcome.ConflictsFound);
            issuesRecorded = checked(issuesRecorded + outcome.IssuesRecorded);
            issuesResolved = checked(issuesResolved + outcome.IssuesResolved);
        }

        public ScheduleMaterializationRunResult ToResult(
            ScheduleMaterializationRunStatus status) => new(
            status,
            seriesScanned,
            sessionsCreated,
            occurrencesSkipped,
            conflictsFound,
            issuesRecorded,
            issuesResolved);
    }
}
