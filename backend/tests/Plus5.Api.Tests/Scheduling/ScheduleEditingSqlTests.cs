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

public sealed class ScheduleEditingSqlTests
{
    [LocalSqlFact]
    public async Task EditsOccurrenceVersionsFutureSeriesAndCancelsWithoutCrossOwnerOrLostUpdates()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase44Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString).Options;
        var clock = new FixedClock();
        var now = clock.GetUtcNow();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = new UserAccount(Guid.NewGuid(), "edit-session@test.local", "EDIT-SESSION@TEST.LOCAL", now.AddDays(-1));
            var foreignOwner = new UserAccount(Guid.NewGuid(), "foreign-edit@test.local", "FOREIGN-EDIT@TEST.LOCAL", now.AddDays(-1));
            var grade = new SchoolGrade(Guid.NewGuid(), "G7", "Grade 7", 7);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Grammar", now.AddDays(-1));
            var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Grammar Oh", 8, GroupStatus.Active, now.AddDays(-1));
            var secondGroup = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Grammar B", 8, GroupStatus.Active, now.AddDays(-1));
            var firstLocation = new Location(Guid.NewGuid(), owner.Id, "Room 1", now.AddDays(-1));
            var secondLocation = new Location(Guid.NewGuid(), owner.Id, "Room 2", now.AddDays(-1));
            var series = new RecurringSessionSeries(Guid.NewGuid(), owner.Id,
                RecurringSessionSeriesKind.RegularGroupSchedule, group.Id, DayOfWeek.Tuesday,
                new(2026, 9, 15), null, new(16, 0), new(17, 0), "Europe/Zagreb", now.AddDays(-1), firstLocation.Id);
            db.AddRange(owner, foreignOwner, grade, program, group, secondGroup, firstLocation, secondLocation, series);
            var dates = new[] { new DateOnly(2026, 9, 15), new(2026, 9, 22), new(2026, 9, 29) };
            var sessions = dates.Select(date => new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group,
                group.Id, Utc(date, 16), Utc(date, 17), "Europe/Zagreb", now.AddDays(-1),
                "Redovni sat", "Plan", firstLocation.Id, recurringSessionSeriesId: series.Id,
                seriesOccurrenceDate: date)).ToArray();
            var blocking = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, secondGroup.Id,
                Utc(dates[1], 18), Utc(dates[1], 19), "Europe/Zagreb", now.AddDays(-1));
            db.AddRange(sessions);
            db.Add(blocking);
            await db.SaveChangesAsync();

            var query = new EfScheduleEditingQuery(db, new EfScheduleSessionDetailQuery(db));
            var edit = await query.GetAsync(owner.Id, sessions[0].Id, CancellationToken.None);
            Assert.NotNull(edit);
            Assert.True(edit.CanEditFutureSeries);
            Assert.Equal(firstLocation.Id, edit.Detail.LocationId);
            Assert.Equal(8, edit.RowVersion.Length);

            async Task<(ScheduleEditResult Result, ScheduleEditPreviewResult Preview)> Update(
                Guid sessionId,
                ScheduleEditCommand command)
            {
                await using var writer = new Plus5DbContext(options);
                var service = new EfScheduleEditingService(writer, clock);
                var preview = await service.PreviewAsync(owner.Id, sessionId, command, CancellationToken.None);
                var result = await service.UpdateAsync(owner.Id, sessionId, command, CancellationToken.None);
                return (result, preview);
            }

            var occurrenceCommand = new ScheduleEditCommand("  Iznimka  ", "  Nova napomena  ",
                dates[0], new(17, 0), new(18, 0), secondLocation.Id, null, 1, edit.RowVersion);
            var occurrenceResult = await Update(sessions[0].Id, occurrenceCommand);
            Assert.Equal(ScheduleEditFailure.None, occurrenceResult.Preview.Failure);
            Assert.False(occurrenceResult.Preview.HasConflict);
            Assert.Equal(ScheduleEditFailure.None, occurrenceResult.Result.Failure);
            db.ChangeTracker.Clear();
            var exception = await db.Sessions.SingleAsync(item => item.Id == sessions[0].Id);
            Assert.True(exception.IsSeriesException);
            Assert.Equal("Iznimka", exception.Title);
            Assert.Equal("Nova napomena", exception.Notes);
            Assert.Equal(secondLocation.Id, exception.LocationId);
            Assert.Equal(Utc(dates[0], 17), exception.StartsAtUtc);
            Assert.Equal(Utc(dates[1], 16), (await db.Sessions.SingleAsync(item => item.Id == sessions[1].Id)).StartsAtUtc);

            var second = await query.GetAsync(owner.Id, sessions[1].Id, CancellationToken.None);
            Assert.NotNull(second);
            var conflictCommand = new ScheduleEditCommand(second.Detail.Title, second.Detail.Notes,
                dates[1], new(18, 0), new(19, 0), firstLocation.Id, null, 2, second.RowVersion);
            var conflictResult = await Update(sessions[1].Id, conflictCommand);
            Assert.True(conflictResult.Preview.HasConflict);
            Assert.Equal(ScheduleEditFailure.ScheduleConflict, conflictResult.Result.Failure);

            var futureCommand = conflictCommand with { StartsAt = new(19, 0), EndsAt = new(20, 0) };
            var futureResult = await Update(sessions[1].Id, futureCommand);
            Assert.Equal(ScheduleEditFailure.None, futureResult.Preview.Failure);
            Assert.Equal(ScheduleEditFailure.None, futureResult.Result.Failure);
            Assert.Equal(12, futureResult.Result.SessionCount);
            db.ChangeTracker.Clear();
            var oldSeries = await db.RecurringSessionSeries.SingleAsync(item => item.Id == series.Id);
            Assert.NotNull(oldSeries.SupersededAtUtc);
            Assert.Equal(new DateOnly(2026, 9, 21), oldSeries.EndsOn);
            var successor = await db.RecurringSessionSeries.SingleAsync(item => item.PreviousSeriesId == series.Id);
            Assert.Equal(new TimeOnly(19, 0), successor.LocalStartTime);
            Assert.Equal(firstLocation.Id, successor.LocationId);
            Assert.All(await db.Sessions.Where(item => item.Id == sessions[1].Id || item.Id == sessions[2].Id).ToListAsync(),
                item => Assert.Equal(SessionStatus.Cancelled, item.Status));
            var replacement = await db.Sessions.SingleAsync(item => item.Id == futureResult.Result.SessionId);
            Assert.Equal(Utc(dates[1], 19), replacement.StartsAtUtc);
            Assert.Equal("Redovni sat", replacement.Title);

            await using (var foreignDb = new Plus5DbContext(options))
            {
                var foreignService = new EfScheduleEditingService(foreignDb, clock);
                Assert.Equal(ScheduleEditFailure.NotFound,
                    (await foreignService.UpdateAsync(foreignOwner.Id, replacement.Id,
                        futureCommand with { RowVersion = replacement.RowVersion }, CancellationToken.None)).Failure);
            }

            await using (var cancelDb = new Plus5DbContext(options))
            {
                var cancelService = new EfScheduleEditingService(cancelDb, clock);
                Assert.Equal(ScheduleEditFailure.ConcurrencyConflict,
                    await cancelService.CancelAsync(owner.Id, replacement.Id, second.RowVersion, CancellationToken.None));
            }

            await using (var cancelDb = new Plus5DbContext(options))
            {
                var cancelService = new EfScheduleEditingService(cancelDb, clock);
                Assert.Equal(ScheduleEditFailure.None,
                    await cancelService.CancelAsync(owner.Id, replacement.Id, replacement.RowVersion, CancellationToken.None));
            }

            db.ChangeTracker.Clear();
            Assert.Equal(SessionStatus.Cancelled,
                (await db.Sessions.SingleAsync(item => item.Id == replacement.Id)).Status);
            Assert.True(await db.Sessions.AnyAsync(item => item.Id == replacement.Id));
        }
        finally
        {
            await using var cleanup = new Plus5DbContext(options);
            Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static DateTimeOffset Utc(DateOnly date, int localHour) =>
        new(date.Year, date.Month, date.Day, localHour - 2, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
            {
                Skip = "Local disposable SQL credentials required.";
            }
        }
    }
}
