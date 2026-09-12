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

public sealed class ScheduleCalendarSqlTests
{
    [LocalSqlFact]
    public async Task CalendarIsOwnerScopedBoundedFilterableAndUsesExplicitAttendanceMetrics()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase41Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var clock = new FixedClock();
        var now = clock.GetUtcNow();

        try
        {
            Guid ownerId;
            Guid groupId;
            Guid programId;
            Guid locationId;
            Guid groupSessionId;
            Guid individualSessionId;

            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var owner = new UserAccount(Guid.NewGuid(), "calendar@test.local", "CALENDAR@TEST.LOCAL", now.AddDays(-20));
                var foreignOwner = new UserAccount(Guid.NewGuid(), "foreign-calendar@test.local", "FOREIGN-CALENDAR@TEST.LOCAL", now.AddDays(-20));
                var grade = new SchoolGrade(Guid.NewGuid(), "K7", "Calendar grade", 7);
                var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Engleski B1", now.AddDays(-20));
                var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreignOwner.Id, "Foreign program", now.AddDays(-20));
                var location = new Location(Guid.NewGuid(), owner.Id, "Učionica 2", now.AddDays(-20));
                var foreignLocation = new Location(Guid.NewGuid(), foreignOwner.Id, "Foreign room", now.AddDays(-20));
                var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "B1 Teens", 3, GroupStatus.Active, now.AddDays(-20));
                var foreignGroup = new Group(Guid.NewGuid(), foreignOwner.Id, foreignProgram.Id, grade.Id, "Foreign group", 4, GroupStatus.Active, now.AddDays(-20));
                var groupStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Ana", "Grupić", StudentStatus.Active, now.AddDays(-20), program.Id, DeliveryMode.Group);
                var individualStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Borna", "Solo", StudentStatus.Active, now.AddDays(-20), program.Id, DeliveryMode.Individual);
                var formerStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Cvita", "Former", StudentStatus.Active, now.AddDays(-20), program.Id, DeliveryMode.Group);
                var futureStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Dino", "Future", StudentStatus.Active, now.AddDays(-20), program.Id, DeliveryMode.Group);
                var membership = new GroupMembership(Guid.NewGuid(), owner.Id, group.Id, groupStudent.Id, now.AddDays(-10));
                var formerMembership = new GroupMembership(Guid.NewGuid(), owner.Id, group.Id, formerStudent.Id, now.AddDays(-10));
                formerMembership.End(now);
                var futureMembership = new GroupMembership(Guid.NewGuid(), owner.Id, group.Id, futureStudent.Id, now.AddDays(2));
                var groupSession = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, group.Id,
                    new(2026, 9, 14, 14, 0, 0, TimeSpan.Zero), new(2026, 9, 14, 15, 0, 0, TimeSpan.Zero),
                    "Europe/Zagreb", now.AddDays(-1), locationId: location.Id);
                var individualSession = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Individual, individualStudent.Id,
                    new(2026, 9, 15, 9, 0, 0, TimeSpan.Zero), new(2026, 9, 15, 9, 45, 0, TimeSpan.Zero),
                    "Europe/Zagreb", now.AddDays(-1), onlineMeetingUrl: "https://meet.example.test/borna");
                var cancelled = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, group.Id,
                    new(2026, 9, 16, 14, 0, 0, TimeSpan.Zero), new(2026, 9, 16, 15, 0, 0, TimeSpan.Zero),
                    "Europe/Zagreb", now.AddDays(-1), locationId: location.Id);
                cancelled.Cancel(now);
                var outsideRange = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, group.Id,
                    new(2026, 10, 1, 14, 0, 0, TimeSpan.Zero), new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero),
                    "Europe/Zagreb", now.AddDays(-1), locationId: location.Id);
                var foreignSession = new Session(Guid.NewGuid(), foreignOwner.Id, DeliveryMode.Group, foreignGroup.Id,
                    new(2026, 9, 14, 14, 0, 0, TimeSpan.Zero), new(2026, 9, 14, 15, 0, 0, TimeSpan.Zero),
                    "Europe/Zagreb", now.AddDays(-1), locationId: foreignLocation.Id);

                db.AddRange(owner, foreignOwner, grade, program, foreignProgram, location, foreignLocation,
                    group, foreignGroup, groupStudent, individualStudent, formerStudent, futureStudent,
                    membership, formerMembership, futureMembership, groupSession,
                    individualSession, cancelled, outsideRange, foreignSession);
                await db.SaveChangesAsync();
                ownerId = owner.Id;
                groupId = group.Id;
                programId = program.Id;
                locationId = location.Id;
                groupSessionId = groupSession.Id;
                individualSessionId = individualSession.Id;
            }

            await using (var db = new Plus5DbContext(options))
            {
                var query = new EfScheduleCalendarQuery(db, clock);
                var calendar = await query.GetAsync(ownerId,
                    new(new(2026, 9, 14), new(2026, 9, 21)), CancellationToken.None);

                Assert.Equal([groupSessionId, individualSessionId], calendar.Items.Select(item => item.Id));
                var groupItem = calendar.Items[0];
                Assert.Equal("B1 Teens", groupItem.ContextName);
                Assert.Equal("Engleski B1", groupItem.ProgramName);
                Assert.Equal("Učionica 2", groupItem.LocationName);
                Assert.Equal(1, groupItem.MemberCount);
                Assert.Equal(3, groupItem.Capacity);
                Assert.Equal(1, calendar.Summary.GroupSessions);
                Assert.Equal(1, calendar.Summary.IndividualSessions);
                Assert.Equal(2, calendar.Summary.TotalSessions);
                Assert.Equal(2, calendar.Summary.UniqueStudents);
                Assert.Equal(2, calendar.Summary.PlannedAttendances);
                Assert.Equal(2, calendar.Summary.AvailableSeats);
                Assert.Equal(groupId, Assert.Single(calendar.Groups).Id);
                Assert.Equal(programId, Assert.Single(calendar.Programs).Id);
                Assert.Equal(locationId, Assert.Single(calendar.Locations).Id);
                Assert.Equal([groupSessionId, individualSessionId], calendar.Reminders.Select(item => item.Id));

                var filtered = await query.GetAsync(ownerId,
                    new(new(2026, 9, 14), new(2026, 9, 21), GroupId: groupId), CancellationToken.None);
                Assert.Single(filtered.Items);
                Assert.Equal(groupSessionId, filtered.Items[0].Id);
                Assert.Equal(1, filtered.Summary.UniqueStudents);
                Assert.Equal(1, filtered.Summary.PlannedAttendances);
                Assert.Single(filtered.Groups);
            }
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
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);
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
