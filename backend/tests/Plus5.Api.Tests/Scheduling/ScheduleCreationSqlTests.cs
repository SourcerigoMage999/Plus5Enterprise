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

public sealed class ScheduleCreationSqlTests
{
    [LocalSqlFact]
    public async Task CreatesOwnedOneOffAndRecurringSessionsAndRejectsInvalidOrConflictingWrites()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase43Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString).Options;
        var clock = new FixedClock();
        var now = clock.GetUtcNow();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = new UserAccount(Guid.NewGuid(), "create-session@test.local", "CREATE-SESSION@TEST.LOCAL", now.AddDays(-1));
            var foreignOwner = new UserAccount(Guid.NewGuid(), "foreign-session@test.local", "FOREIGN-SESSION@TEST.LOCAL", now.AddDays(-1));
            var grade = new SchoolGrade(Guid.NewGuid(), "G7", "Grade 7", 7);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Grammar", now.AddDays(-1));
            var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreignOwner.Id, "Foreign", now.AddDays(-1));
            var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Grammar 7", 6, GroupStatus.Active, now.AddDays(-1));
            var secondGroup = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Grammar 7 B", 6, GroupStatus.Active, now.AddDays(-1));
            var inactiveGroup = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Inactive", 6, GroupStatus.Inactive, now.AddDays(-1));
            var foreignGroup = new Group(Guid.NewGuid(), foreignOwner.Id, foreignProgram.Id, grade.Id, "Foreign", 6, GroupStatus.Active, now.AddDays(-1));
            var student = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Ana", "Test", StudentStatus.Active, now.AddDays(-1), program.Id, DeliveryMode.Individual);
            var inactiveStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Iva", "Inactive", StudentStatus.Inactive, now.AddDays(-1), program.Id, DeliveryMode.Individual);
            var location = new Location(Guid.NewGuid(), owner.Id, "Room 1", now.AddDays(-1));
            var foreignLocation = new Location(Guid.NewGuid(), foreignOwner.Id, "Foreign room", now.AddDays(-1));
            db.AddRange(owner, foreignOwner, grade, program, foreignProgram, group, secondGroup,
                inactiveGroup, foreignGroup, student, inactiveStudent, location, foreignLocation);
            await db.SaveChangesAsync();

            async Task<ScheduleCreateResult> Create(ScheduleCreateCommand command)
            {
                await using var writer = new Plus5DbContext(options);
                return await new EfScheduleCreationService(writer, clock)
                    .CreateAsync(owner.Id, command, CancellationToken.None);
            }

            var oneOff = new ScheduleCreateCommand(2, group.Id, "  Dodatni sat  ", "  Priprema prema planu.  ", new(2026, 9, 14),
                new(16, 0), new(17, 0), false, null, location.Id, null);
            var created = await Create(oneOff);
            Assert.Equal(ScheduleCreateFailure.None, created.Failure);
            Assert.Equal(1, created.SessionCount);
            var saved = await db.Sessions.AsNoTracking().SingleAsync(session => session.Id == created.SessionId);
            Assert.Equal("Dodatni sat", saved.Title);
            Assert.Equal("Priprema prema planu.", saved.Notes);
            Assert.Equal(group.Id, saved.GroupId);
            Assert.Equal(new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.Zero), saved.StartsAtUtc);
            Assert.Equal(location.Id, saved.LocationId);
            Assert.Null(saved.RecurringSessionSeriesId);

            Assert.Equal(ScheduleCreateFailure.ScheduleConflict,
                (await Create(oneOff with { ContextId = secondGroup.Id, StartsAt = new(16, 30), EndsAt = new(17, 30) })).Failure);
            Assert.Equal(ScheduleCreateFailure.None,
                (await Create(oneOff with { ContextId = secondGroup.Id, StartsAt = new(17, 0), EndsAt = new(18, 0), LocationId = null })).Failure);
            Assert.Equal(ScheduleCreateFailure.NotFound,
                (await Create(oneOff with { ContextId = foreignGroup.Id, Date = new(2026, 9, 15) })).Failure);
            Assert.Equal(ScheduleCreateFailure.NotFound,
                (await Create(oneOff with { LocationId = foreignLocation.Id, Date = new(2026, 9, 15) })).Failure);
            Assert.Equal(ScheduleCreateFailure.Unavailable,
                (await Create(oneOff with { ContextId = inactiveGroup.Id, Date = new(2026, 9, 15), LocationId = null })).Failure);
            Assert.Equal(ScheduleCreateFailure.Unavailable,
                (await Create(oneOff with { DeliveryMode = 1, ContextId = inactiveStudent.Id, Date = new(2026, 9, 15), LocationId = null })).Failure);
            Assert.Equal(ScheduleCreateFailure.Invalid,
                (await Create(oneOff with { DeliveryMode = 2, RepeatWeekly = true, LocationId = null })).Failure);
            Assert.Equal(ScheduleCreateFailure.Invalid,
                (await Create(oneOff with { Date = new(2026, 9, 11), LocationId = null })).Failure);
            Assert.Equal(ScheduleCreateFailure.InvalidLocalTime,
                (await Create(oneOff with { Date = new(2026, 10, 25), StartsAt = new(2, 30), EndsAt = new(3, 30), LocationId = null })).Failure);

            var recurring = new ScheduleCreateCommand(1, student.Id, null, null, new(2026, 9, 15),
                new(10, 0), new(11, 0), true, null, null, "https://meet.example.test/ana");
            var seriesResult = await Create(recurring);
            Assert.Equal(ScheduleCreateFailure.None, seriesResult.Failure);
            Assert.Equal(12, seriesResult.SessionCount);
            var series = await db.RecurringSessionSeries.AsNoTracking()
                .SingleAsync(item => item.StudentId == student.Id);
            Assert.Equal(RecurringSessionSeriesKind.IndividualRecurrence, series.Kind);
            Assert.Null(series.EndsOn);
            Assert.Equal("https://meet.example.test/ana", series.OnlineMeetingUrl);
            Assert.Equal(12, await db.Sessions.CountAsync(session => session.RecurringSessionSeriesId == series.Id));
            Assert.Equal(seriesResult.SessionId, await db.Sessions.Where(session => session.RecurringSessionSeriesId == series.Id)
                .OrderBy(session => session.StartsAtUtc).Select(session => session.Id).FirstAsync());

            var beforeRace = await db.Sessions.CountAsync();
            var race = oneOff with { Date = new(2026, 9, 16), StartsAt = new(13, 0), EndsAt = new(14, 0), LocationId = null };
            var raced = await Task.WhenAll(
                Create(race with { ContextId = group.Id }),
                Create(race with { ContextId = secondGroup.Id }));
            Assert.Single(raced, result => result.Failure == ScheduleCreateFailure.None);
            Assert.All(raced.Where(result => result.Failure != ScheduleCreateFailure.None), result =>
                Assert.True(result.Failure is ScheduleCreateFailure.ScheduleConflict or ScheduleCreateFailure.ConcurrencyConflict));
            Assert.Equal(beforeRace + 1, await db.Sessions.CountAsync());
        }
        finally
        {
            await using var cleanup = new Plus5DbContext(options);
            Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 12, 8, 0, 0, TimeSpan.Zero);
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
