using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
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

public sealed class ScheduleMaterializationSqlTests
{
    [LocalSqlFact]
    public async Task MigrationUpgradePreservesExistingSeriesAndIsIdempotent()
    {
        var (options, databaseName) = CreateDatabase();
        var now = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

        try
        {
            Guid seriesId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.GetService<IMigrator>()
                    .MigrateAsync("20260911143606_AllowOpenEndedGroupSeries");
                var seed = CreateGroupSeries(now, DayOfWeek.Tuesday, new(16, 0), new(17, 0));
                seriesId = seed.Series.Id;
                db.AddRange(seed.Owner, seed.Grade, seed.Program, seed.Group, seed.Series);
                await db.SaveChangesAsync();

                await db.Database.MigrateAsync();
                await db.Database.MigrateAsync();
                db.ChangeTracker.Clear();

                Assert.True(await db.RecurringSessionSeries.AsNoTracking()
                    .AnyAsync(series => series.Id == seriesId));
                Assert.Empty(await db.ScheduleMaterializationIssues.AsNoTracking().ToListAsync());
                Assert.Empty(await db.ScheduleMaterializationLeases.AsNoTracking().ToListAsync());
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task ReplenishmentPreservesExistingOccurrencesAndIsIdempotent()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new MutableClock(new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero));

        try
        {
            Guid seriesId;
            Guid exceptionId;
            Guid cancelledId;
            Guid inProgressId;
            Guid heldId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var seed = CreateGroupSeries(clock.GetUtcNow(), DayOfWeek.Tuesday, new(16, 0), new(17, 0));
                seriesId = seed.Series.Id;
                var exception = CreateSession(seed, new(2026, 9, 22), clock.GetUtcNow().AddDays(-1));
                exception.UpdateDetails(
                    LocalUtc(new(2026, 9, 23), new(18, 0)),
                    LocalUtc(new(2026, 9, 23), new(19, 0)),
                    "Iznimka",
                    null,
                    null,
                    null,
                    clock.GetUtcNow());
                var cancelled = CreateSession(seed, new(2026, 9, 29), clock.GetUtcNow().AddDays(-1));
                cancelled.Cancel(clock.GetUtcNow());
                var inProgress = CreateSession(seed, new(2026, 10, 6), clock.GetUtcNow().AddDays(-1));
                inProgress.Start(clock.GetUtcNow());
                var held = CreateSession(seed, new(2026, 10, 13), clock.GetUtcNow().AddDays(-1));
                held.Start(clock.GetUtcNow());
                held.Complete(clock.GetUtcNow());
                exceptionId = exception.Id;
                cancelledId = cancelled.Id;
                inProgressId = inProgress.Id;
                heldId = held.Id;
                db.AddRange(seed.Owner, seed.Grade, seed.Program, seed.Group, seed.Series);
                db.AddRange(exception, cancelled, inProgress, held);
                await db.SaveChangesAsync();
            }

            var first = await CreateService(options, clock).RunAsync("worker-one", CancellationToken.None);
            var second = await CreateService(options, clock).RunAsync("worker-one", CancellationToken.None);

            Assert.Equal(ScheduleMaterializationRunStatus.Completed, first.Status);
            Assert.Equal(8, first.SessionsCreated);
            Assert.Equal(ScheduleMaterializationRunStatus.Completed, second.Status);
            Assert.Equal(0, second.SessionsCreated);

            await using var assertionDb = new Plus5DbContext(options);
            var sessions = await assertionDb.Sessions.AsNoTracking()
                .Where(session => session.RecurringSessionSeriesId == seriesId)
                .OrderBy(session => session.SeriesOccurrenceDate)
                .ToListAsync();
            Assert.Equal(12, sessions.Count);
            Assert.Equal(sessions.Count,
                sessions.Select(session => session.SeriesOccurrenceDate).Distinct().Count());
            Assert.True(sessions.Single(session => session.Id == exceptionId).IsSeriesException);
            Assert.Equal(SessionStatus.Cancelled,
                sessions.Single(session => session.Id == cancelledId).Status);
            Assert.Equal(SessionStatus.InProgress,
                sessions.Single(session => session.Id == inProgressId).Status);
            Assert.Equal(SessionStatus.Held, sessions.Single(session => session.Id == heldId).Status);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task ConflictIssueIsUpsertedAndResolvedAfterSuccessfulMaterialization()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new MutableClock(new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero));

        try
        {
            Guid seriesId;
            Guid conflictId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var seed = CreateGroupSeries(clock.GetUtcNow(), DayOfWeek.Tuesday, new(16, 0), new(17, 0));
                seriesId = seed.Series.Id;
                var conflict = new Session(
                    Guid.NewGuid(),
                    seed.Owner.Id,
                    DeliveryMode.Group,
                    seed.Group.Id,
                    LocalUtc(new(2026, 9, 22), new(16, 30)),
                    LocalUtc(new(2026, 9, 22), new(17, 30)),
                    "Europe/Zagreb",
                    clock.GetUtcNow());
                conflictId = conflict.Id;
                db.AddRange(seed.Owner, seed.Grade, seed.Program, seed.Group, seed.Series, conflict);
                await db.SaveChangesAsync();
            }

            var service = CreateService(options, clock);
            var first = await service.RunAsync("worker-one", CancellationToken.None);
            var second = await service.RunAsync("worker-one", CancellationToken.None);

            Assert.Equal(1, first.ConflictsFound);
            Assert.Equal(1, second.ConflictsFound);
            await using (var issueDb = new Plus5DbContext(options))
            {
                var issue = await issueDb.ScheduleMaterializationIssues.SingleAsync(item =>
                    item.RecurringSessionSeriesId == seriesId
                    && item.OccurrenceLocalDate == new DateOnly(2026, 9, 22)
                    && item.IssueType == ScheduleMaterializationIssueType.TeacherConflict);
                Assert.Equal(2, issue.AttemptCount);
                Assert.Null(issue.ResolvedAtUtc);

                var conflict = await issueDb.Sessions.SingleAsync(item => item.Id == conflictId);
                conflict.Cancel(clock.GetUtcNow());
                await issueDb.SaveChangesAsync();
            }

            clock.Advance(TimeSpan.FromHours(6));
            var recovered = await service.RunAsync("worker-one", CancellationToken.None);

            Assert.Equal(1, recovered.SessionsCreated);
            Assert.Equal(1, recovered.IssuesResolved);
            await using var assertionDb = new Plus5DbContext(options);
            Assert.Single(await assertionDb.Sessions.AsNoTracking().Where(session =>
                session.RecurringSessionSeriesId == seriesId
                && session.SeriesOccurrenceDate == new DateOnly(2026, 9, 22)).ToListAsync());
            var resolved = await assertionDb.ScheduleMaterializationIssues.AsNoTracking()
                .SingleAsync(item => item.RecurringSessionSeriesId == seriesId
                    && item.OccurrenceLocalDate == new DateOnly(2026, 9, 22)
                    && item.IssueType == ScheduleMaterializationIssueType.TeacherConflict);
            Assert.NotNull(resolved.ResolvedAtUtc);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlLeaseSkipsSecondOwnerAndAllowsExpiredTakeover()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new MutableClock(new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero));

        try
        {
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            var first = CreateService(options, clock);
            var second = CreateService(options, clock);

            var firstLease = new SqlScheduleMaterializationLeaseManager(options, clock);
            Assert.True(await firstLease.TryAcquireAsync("worker-one", CancellationToken.None));
            var skipped = await second.RunAsync("worker-two", CancellationToken.None);
            Assert.Equal(ScheduleMaterializationRunStatus.LeaseSkipped, skipped.Status);

            clock.Advance(SqlScheduleMaterializationLeaseManager.LeaseDuration.Add(TimeSpan.FromSeconds(1)));
            var takeover = await second.RunAsync("worker-two", CancellationToken.None);
            Assert.Equal(ScheduleMaterializationRunStatus.Completed, takeover.Status);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task ConcurrentWorkersUseSingleLeaseAndCreateOneOccurrenceSet()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new MutableClock(new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero));

        try
        {
            Guid seriesId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var seed = CreateGroupSeries(clock.GetUtcNow(), DayOfWeek.Tuesday, new(16, 0), new(17, 0));
                seriesId = seed.Series.Id;
                db.AddRange(seed.Owner, seed.Grade, seed.Program, seed.Group, seed.Series);
                await db.SaveChangesAsync();
            }

            await using var blocker = new Plus5DbContext(options);
            await using var blockerTransaction = await blocker.Database.BeginTransactionAsync();
            await blocker.RecurringSessionSeries
                .FromSqlInterpolated($"SELECT * FROM [RecurringSessionSeries] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {seriesId}")
                .SingleAsync();

            var firstService = CreateService(options, clock);
            var firstRun = firstService.RunAsync("worker-one", CancellationToken.None);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!await LeaseIsOwnedByAsync(options, "worker-one", timeout.Token))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
            }

            var second = await CreateService(options, clock)
                .RunAsync("worker-two", CancellationToken.None);
            Assert.Equal(ScheduleMaterializationRunStatus.LeaseSkipped, second.Status);

            await blockerTransaction.RollbackAsync();
            var first = await firstRun;
            Assert.Equal(ScheduleMaterializationRunStatus.Completed, first.Status);

            await using var assertionDb = new Plus5DbContext(options);
            var sessions = await assertionDb.Sessions.AsNoTracking()
                .Where(session => session.RecurringSessionSeriesId == seriesId)
                .ToListAsync();
            Assert.Equal(12, sessions.Count);
            Assert.Equal(sessions.Count,
                sessions.Select(session => session.SeriesOccurrenceDate).Distinct().Count());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task InvalidDstOccurrenceCreatesDurableIssueWithoutBlockingOtherDates()
    {
        var (options, databaseName) = CreateDatabase();
        var clock = new MutableClock(new(2026, 3, 23, 8, 0, 0, TimeSpan.Zero));

        try
        {
            Guid seriesId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var seed = CreateGroupSeries(clock.GetUtcNow(), DayOfWeek.Sunday, new(2, 30), new(3, 30));
                seriesId = seed.Series.Id;
                db.AddRange(seed.Owner, seed.Grade, seed.Program, seed.Group, seed.Series);
                await db.SaveChangesAsync();
            }

            var result = await CreateService(options, clock)
                .RunAsync("worker-one", CancellationToken.None);

            Assert.Equal(ScheduleMaterializationRunStatus.Completed, result.Status);
            Assert.True(result.SessionsCreated > 0);
            await using var assertionDb = new Plus5DbContext(options);
            var issue = await assertionDb.ScheduleMaterializationIssues.AsNoTracking()
                .SingleAsync(item => item.RecurringSessionSeriesId == seriesId
                    && item.OccurrenceLocalDate == new DateOnly(2026, 3, 29));
            Assert.Equal(ScheduleMaterializationIssueType.InvalidLocalTime, issue.IssueType);
            Assert.Null(issue.ResolvedAtUtc);
            Assert.DoesNotContain(await assertionDb.Sessions.AsNoTracking()
                    .Where(session => session.RecurringSessionSeriesId == seriesId).ToListAsync(),
                session => session.SeriesOccurrenceDate == new DateOnly(2026, 3, 29));
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static EfScheduleMaterializationService CreateService(
        DbContextOptions<Plus5DbContext> options,
        TimeProvider clock) => new(options, clock,
        NullLogger<EfScheduleMaterializationService>.Instance);

    private static async Task<bool> LeaseIsOwnedByAsync(
        DbContextOptions<Plus5DbContext> options,
        string ownerId,
        CancellationToken cancellationToken)
    {
        await using var db = new Plus5DbContext(options);
        return await db.ScheduleMaterializationLeases.AsNoTracking()
            .AnyAsync(lease => lease.OwnerId == ownerId, cancellationToken);
    }

    private static SeriesSeed CreateGroupSeries(
        DateTimeOffset now,
        DayOfWeek dayOfWeek,
        TimeOnly startsAt,
        TimeOnly endsAt)
    {
        var emailLocalPart = Guid.NewGuid().ToString("N");
        var email = $"{emailLocalPart}@test.local";
        var owner = new UserAccount(Guid.NewGuid(), email,
            email.ToUpperInvariant(), now.AddDays(-30));
        var grade = new SchoolGrade(Guid.NewGuid(), $"G{Guid.NewGuid():N}"[..12],
            "Materialization grade", 7);
        var program = new TeachingProgram(Guid.NewGuid(), owner.Id,
            $"Program {Guid.NewGuid():N}"[..20], now.AddDays(-30));
        var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id,
            $"Group {Guid.NewGuid():N}"[..18], 10, GroupStatus.Active, now.AddDays(-30));
        var series = new RecurringSessionSeries(
            Guid.NewGuid(),
            owner.Id,
            RecurringSessionSeriesKind.RegularGroupSchedule,
            group.Id,
            dayOfWeek,
            DateOnly.FromDateTime(now.AddDays(-30).UtcDateTime),
            null,
            startsAt,
            endsAt,
            "Europe/Zagreb",
            now.AddDays(-30));
        return new(owner, grade, program, group, series);
    }

    private static Session CreateSession(
        SeriesSeed seed,
        DateOnly date,
        DateTimeOffset createdAt) => new(
        Guid.NewGuid(),
        seed.Owner.Id,
        DeliveryMode.Group,
        seed.Group.Id,
        LocalUtc(date, seed.Series.LocalStartTime),
        LocalUtc(date, seed.Series.LocalEndTime),
        seed.Series.TimeZoneId,
        createdAt,
        recurringSessionSeriesId: seed.Series.Id,
        seriesOccurrenceDate: date);

    private static DateTimeOffset LocalUtc(DateOnly date, TimeOnly time)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");
        return new(TimeZoneInfo.ConvertTimeToUtc(
            date.ToDateTime(time, DateTimeKind.Unspecified),
            zone));
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase46Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
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

    private sealed record SeriesSeed(
        UserAccount Owner,
        SchoolGrade Grade,
        TeachingProgram Program,
        Group Group,
        RecurringSessionSeries Series);

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan value) => now = now.Add(value);
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
