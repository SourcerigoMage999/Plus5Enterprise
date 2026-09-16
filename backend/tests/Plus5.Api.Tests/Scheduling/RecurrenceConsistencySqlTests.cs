using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Scheduling;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Scheduling;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Scheduling;

public sealed class RecurrenceConsistencySqlTests
{
    [LocalSqlFact]
    public async Task FutureChangePreservesOccurrenceExceptionsAndNonScheduledHistory()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new FixedClock();
        var now = clock.GetUtcNow();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = new UserAccount(Guid.NewGuid(), "recurrence@test.local",
                "RECURRENCE@TEST.LOCAL", now.AddDays(-20));
            var grade = new SchoolGrade(Guid.NewGuid(), "RC7", "Recurrence grade", 7);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Recurrence",
                now.AddDays(-20));
            var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id,
                "Recurrence group", 8, GroupStatus.Active, now.AddDays(-20));
            var firstLocation = new Location(Guid.NewGuid(), owner.Id, "Room 1", now.AddDays(-20));
            var secondLocation = new Location(Guid.NewGuid(), owner.Id, "Room 2", now.AddDays(-20));
            var series = new RecurringSessionSeries(Guid.NewGuid(), owner.Id,
                RecurringSessionSeriesKind.RegularGroupSchedule, group.Id, DayOfWeek.Tuesday,
                new(2026, 9, 8), null, new(16, 0), new(17, 0), "Europe/Zagreb",
                now.AddDays(-20), firstLocation.Id);
            var dates = new[]
            {
                new DateOnly(2026, 9, 8),
                new DateOnly(2026, 9, 15),
                new DateOnly(2026, 9, 22),
                new DateOnly(2026, 9, 29),
                new DateOnly(2026, 10, 6),
                new DateOnly(2026, 10, 13),
                new DateOnly(2026, 10, 20),
            };
            var sessions = dates.Select(date => CreateSession(owner.Id, group.Id, series.Id,
                date, new(16, 0), new(17, 0), now.AddDays(-20), firstLocation.Id)).ToArray();
            db.AddRange(owner, grade, program, group, firstLocation, secondLocation, series);
            db.AddRange(sessions);
            await db.SaveChangesAsync();

            var exceptionRead = await new EfScheduleEditingQuery(db,
                    new EfScheduleSessionDetailQuery(db))
                .GetAsync(owner.Id, sessions[2].Id, CancellationToken.None);
            Assert.NotNull(exceptionRead);
            await using (var writer = new Plus5DbContext(options))
            {
                var oneOccurrence = new ScheduleEditCommand("Iznimka", "Poseban termin",
                    dates[2], new(18, 0), new(19, 0), secondLocation.Id, null,
                    (int)ScheduleEditScope.OneOccurrence, exceptionRead.RowVersion);
                var result = await new EfScheduleEditingService(writer, clock)
                    .UpdateAsync(owner.Id, sessions[2].Id, oneOccurrence, CancellationToken.None);
                Assert.Equal(ScheduleEditFailure.None, result.Failure);
                Assert.Equal(1, result.SessionCount);
            }

            db.ChangeTracker.Clear();
            var exception = await db.Sessions.SingleAsync(item => item.Id == sessions[2].Id);
            Assert.True(exception.IsSeriesException);
            Assert.Equal(LocalUtc(dates[2], new(18, 0)), exception.StartsAtUtc);
            Assert.Null((await db.RecurringSessionSeries.SingleAsync(item => item.Id == series.Id))
                .SupersededAtUtc);
            Assert.Equal(LocalUtc(dates[1], new(16, 0)),
                (await db.Sessions.SingleAsync(item => item.Id == sessions[1].Id)).StartsAtUtc);

            var inProgress = await db.Sessions.SingleAsync(item => item.Id == sessions[3].Id);
            inProgress.Start(now.AddDays(-2));
            var held = await db.Sessions.SingleAsync(item => item.Id == sessions[4].Id);
            held.Start(now.AddDays(-2));
            held.Complete(now.AddDays(-1));
            var cancelled = await db.Sessions.SingleAsync(item => item.Id == sessions[5].Id);
            cancelled.Cancel(now.AddDays(-1));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var selectedRead = await new EfScheduleEditingQuery(db,
                    new EfScheduleSessionDetailQuery(db))
                .GetAsync(owner.Id, sessions[1].Id, CancellationToken.None);
            Assert.NotNull(selectedRead);
            var futureCommand = new ScheduleEditCommand(selectedRead.Detail.Title,
                selectedRead.Detail.Notes, dates[1], new(17, 0), new(18, 0),
                secondLocation.Id, null, (int)ScheduleEditScope.FutureSeries,
                selectedRead.RowVersion);

            ScheduleEditResult futureResult;
            await using (var writer = new Plus5DbContext(options))
            {
                futureResult = await new EfScheduleEditingService(writer, clock)
                    .UpdateAsync(owner.Id, sessions[1].Id, futureCommand, CancellationToken.None);
            }

            Assert.Equal(ScheduleEditFailure.None, futureResult.Failure);
            Assert.Equal(8, futureResult.SessionCount);
            db.ChangeTracker.Clear();

            var oldSeries = await db.RecurringSessionSeries.SingleAsync(item => item.Id == series.Id);
            Assert.Equal(new DateOnly(2026, 9, 14), oldSeries.EndsOn);
            Assert.NotNull(oldSeries.SupersededAtUtc);
            var successor = await db.RecurringSessionSeries.SingleAsync(
                item => item.PreviousSeriesId == series.Id);
            Assert.Equal(dates[1], successor.StartsOn);
            Assert.Equal(new TimeOnly(17, 0), successor.LocalStartTime);
            Assert.Equal(secondLocation.Id, successor.LocationId);

            Assert.Equal(SessionStatus.Scheduled,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[0].Id)).Status);
            Assert.Equal(SessionStatus.Cancelled,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[1].Id)).Status);
            Assert.Equal(SessionStatus.Scheduled,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[2].Id)).Status);
            Assert.Equal(SessionStatus.InProgress,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[3].Id)).Status);
            Assert.Equal(SessionStatus.Held,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[4].Id)).Status);
            Assert.Equal(SessionStatus.Cancelled,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[5].Id)).Status);
            Assert.Equal(SessionStatus.Cancelled,
                (await db.Sessions.SingleAsync(item => item.Id == sessions[6].Id)).Status);

            var successorOccurrences = await db.Sessions.AsNoTracking()
                .Where(item => item.RecurringSessionSeriesId == successor.Id)
                .OrderBy(item => item.SeriesOccurrenceDate)
                .ToListAsync();
            Assert.Equal(futureResult.SessionCount, successorOccurrences.Count);
            Assert.Equal(successorOccurrences.Count,
                successorOccurrences.Select(item => item.SeriesOccurrenceDate).Distinct().Count());
            Assert.DoesNotContain(successorOccurrences,
                item => dates.Skip(2).Take(4).Contains(item.SeriesOccurrenceDate!.Value));
            Assert.Contains(successorOccurrences, item => item.SeriesOccurrenceDate == dates[1]);
            Assert.Contains(successorOccurrences, item => item.SeriesOccurrenceDate == dates[6]);
            Assert.Equal(LocalUtc(dates[1], new(17, 0)),
                successorOccurrences.Single(item => item.SeriesOccurrenceDate == dates[1]).StartsAtUtc);

            await using var staleWriter = new Plus5DbContext(options);
            var staleResult = await new EfScheduleEditingService(staleWriter, clock)
                .UpdateAsync(owner.Id, sessions[1].Id, futureCommand, CancellationToken.None);
            Assert.Equal(ScheduleEditFailure.Unavailable, staleResult.Failure);
            Assert.Single(await db.RecurringSessionSeries.AsNoTracking()
                .Where(item => item.PreviousSeriesId == series.Id).ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task ConcurrentFutureChangesCreateExactlyOneSuccessor()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new FixedClock();
        var now = clock.GetUtcNow();

        try
        {
            Guid ownerId;
            Guid seriesId;
            Guid sessionId;
            byte[] rowVersion;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var owner = new UserAccount(Guid.NewGuid(), "race-series@test.local",
                    "RACE-SERIES@TEST.LOCAL", now.AddDays(-10));
                var grade = new SchoolGrade(Guid.NewGuid(), "RR7", "Race grade", 7);
                var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Race",
                    now.AddDays(-10));
                var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id,
                    "Race group", 6, GroupStatus.Active, now.AddDays(-10));
                var series = new RecurringSessionSeries(Guid.NewGuid(), owner.Id,
                    RecurringSessionSeriesKind.RegularGroupSchedule, group.Id, DayOfWeek.Tuesday,
                    new(2026, 9, 15), null, new(16, 0), new(17, 0), "Europe/Zagreb",
                    now.AddDays(-10));
                var session = CreateSession(owner.Id, group.Id, series.Id,
                    new(2026, 9, 15), new(16, 0), new(17, 0), now.AddDays(-10));
                db.AddRange(owner, grade, program, group, series, session);
                await db.SaveChangesAsync();
                ownerId = owner.Id;
                seriesId = series.Id;
                sessionId = session.Id;
                rowVersion = session.RowVersion.ToArray();
            }

            var command = new ScheduleEditCommand(null, null, new(2026, 9, 15),
                new(17, 0), new(18, 0), null, null,
                (int)ScheduleEditScope.FutureSeries, rowVersion);
            async Task<ScheduleEditResult> Update()
            {
                await using var writer = new Plus5DbContext(options);
                return await new EfScheduleEditingService(writer, clock)
                    .UpdateAsync(ownerId, sessionId, command, CancellationToken.None);
            }

            var results = await Task.WhenAll(Update(), Update());
            Assert.Single(results, item => item.Failure == ScheduleEditFailure.None);
            Assert.All(results.Where(item => item.Failure != ScheduleEditFailure.None), item =>
                Assert.Contains(item.Failure, new[]
                {
                    ScheduleEditFailure.ConcurrencyConflict,
                    ScheduleEditFailure.Unavailable,
                }));

            await using var assertionDb = new Plus5DbContext(options);
            var successors = await assertionDb.RecurringSessionSeries.AsNoTracking()
                .Where(item => item.PreviousSeriesId == seriesId)
                .ToListAsync();
            Assert.Single(successors);
            Assert.Single(await assertionDb.RecurringSessionSeries.AsNoTracking()
                .Where(item => item.Id == seriesId && item.SupersededAtUtc != null)
                .ToListAsync());
            Assert.Equal(12, await assertionDb.Sessions.AsNoTracking()
                .CountAsync(item => item.RecurringSessionSeriesId == successors[0].Id));
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static Session CreateSession(
        Guid owner,
        Guid group,
        Guid series,
        DateOnly date,
        TimeOnly startsAt,
        TimeOnly endsAt,
        DateTimeOffset createdAt,
        Guid? location = null) => new(
            Guid.NewGuid(),
            owner,
            DeliveryMode.Group,
            group,
            LocalUtc(date, startsAt),
            LocalUtc(date, endsAt),
            "Europe/Zagreb",
            createdAt,
            locationId: location,
            recurringSessionSeriesId: series,
            seriesOccurrenceDate: date);

    private static DateTimeOffset LocalUtc(DateOnly date, TimeOnly time)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase45Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString).Options;
        return (options, databaseName);
    }

    private static async Task DeleteDatabase(
        DbContextOptions<Plus5DbContext> options,
        string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
            {
                Skip = "Local disposable SQL credentials required.";
            }
        }
    }
}
