using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupCreationSqlTests
{
    [LocalSqlFact]
    public async Task UpgradeAtomicCreatesRollbackAndConcurrentWrites()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase36Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options;
        var clock = new FixedClock();
        var now = clock.GetUtcNow();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync("20260901224924_AddStudentEditingConcurrency");
            var owner = new UserAccount(Guid.NewGuid(), "create@test.local", "CREATE@TEST.LOCAL", now.AddDays(-1));
            var foreignOwner = new UserAccount(Guid.NewGuid(), "foreign@test.local", "FOREIGN@TEST.LOCAL", now.AddDays(-1));
            var grade = new SchoolGrade(Guid.NewGuid(), "C7", "Grade 7", 7);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Program", now.AddDays(-1));
            var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreignOwner.Id, "Foreign", now.AddDays(-1));
            var student = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Ana", "Test", StudentStatus.Active, now.AddDays(-1));
            var second = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Borna", "Test", StudentStatus.Active, now.AddDays(-1));
            var location = new Location(Guid.NewGuid(), owner.Id, "Room", now.AddDays(-1));
            var legacy = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Legacy", 1, GroupStatus.Active, now.AddDays(-1));
            var legacyEnd = new DateOnly(2026, 1, 31);
            var legacySeries = new RecurringSessionSeries(Guid.NewGuid(), owner.Id, RecurringSessionSeriesKind.RegularGroupSchedule,
                legacy.Id, DayOfWeek.Monday, new(2026, 1, 1), legacyEnd, new(10, 0), new(11, 0), "Europe/Zagreb", now.AddDays(-1));
            db.AddRange(owner, foreignOwner, grade, program, foreignProgram, student, second, location, legacy, legacySeries);
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(legacyEnd, (await db.RecurringSessionSeries.SingleAsync()).EndsOn);

            GroupCreateCommand Command(string name) => new(name, program.Id, grade.Id, 6, null, [], [], null, null, null);
            async Task<GroupCreateResult> Create(GroupCreateCommand command)
            {
                await using var writer = new Plus5DbContext(options);
                return await new EfGroupCreationService(writer, clock).CreateAsync(owner.Id, command, CancellationToken.None);
            }
            var empty = await Create(Command("Empty"));
            Assert.Equal(GroupCreateFailure.None, empty.Failure);
            Assert.Equal(GroupStatus.Active, (await db.Groups.SingleAsync(g => g.Id == empty.Id)).Status);
            Assert.False(await db.GroupMemberships.AnyAsync(m => m.GroupId == empty.Id));
            Assert.False(await db.RecurringSessionSeries.AnyAsync(s => s.GroupId == empty.Id));
            Assert.Equal(GroupCreateFailure.DuplicateName, (await Create(Command(" empty "))).Failure);
            Assert.Equal(GroupCreateFailure.NotFound, (await Create(Command("Foreign") with { ProgramId = foreignProgram.Id })).Failure);
            Assert.Equal(GroupCreateFailure.Invalid, (await Create(Command("Capacity") with { Capacity = 0 })).Failure);
            var member = new InitialGroupMember(student.Id, student.RowVersion);
            Assert.Equal(GroupCreateFailure.Invalid, (await Create(Command("Duplicate member") with { Members = [member, member] })).Failure);
            Assert.Equal(GroupCreateFailure.Conflict, (await Create(Command("Stale") with { Members = [member with { RowVersion = new byte[8] }] })).Failure);
            var schedule = Command("Scheduled") with { Members = [member], Slots = [new(1, new(16, 0), new(17, 0))], StartsOn = new(2026, 9, 7), LocationId = location.Id };
            var created = await Create(schedule);
            Assert.Equal(GroupCreateFailure.None, created.Failure);
            Assert.Equal(12, created.SessionCount);
            Assert.Null((await db.RecurringSessionSeries.SingleAsync(s => s.GroupId == created.Id)).EndsOn);
            Assert.Equal(12, await db.Sessions.CountAsync(s => s.GroupId == created.Id));
            Assert.Equal(1, await db.GroupMemberships.CountAsync(m => m.GroupId == created.Id));
            var changedStudent = await db.Students.AsNoTracking().SingleAsync(s => s.Id == student.Id);
            Assert.Equal(DeliveryMode.Group, changedStudent.DeliveryMode);
            Assert.Equal(program.Id, changedStudent.ProgramId);
            Assert.False(student.RowVersion.SequenceEqual(changedStudent.RowVersion));
            Assert.Single((await new EfGroupQuery(db, clock).GetAsync(owner.Id, created.Id!.Value, CancellationToken.None))!.Slots);
            var before = await db.Groups.CountAsync();
            var conflict = await Create(schedule with { Name = "Conflict", Members = [new(second.Id, second.RowVersion)] });
            Assert.Equal(GroupCreateFailure.ScheduleConflict, conflict.Failure);
            Assert.Equal(before, await db.Groups.CountAsync());
            Assert.Null((await db.Students.AsNoTracking().SingleAsync(s => s.Id == second.Id)).DeliveryMode);
            Assert.False(await db.GroupMemberships.AnyAsync(m => m.StudentId == second.Id));
            Assert.Equal(GroupCreateFailure.MembershipChanged, (await Create(Command("Already joined") with { Members = [member] })).Failure);
            var touching = await Create(schedule with { Name = "Touching", Members = [], Slots = [new(1, new(17, 0), new(18, 0))], EndsOn = new(2026, 9, 7) });
            Assert.Equal(GroupCreateFailure.None, touching.Failure);
            Assert.Equal(1, touching.SessionCount);
            Assert.Equal(GroupCreateFailure.InvalidLocalTime, (await Create(schedule with { Name = "DST", Members = [], StartsOn = new(2026, 10, 25), EndsOn = new(2026, 10, 25), Slots = [new(0, new(2, 30), new(3, 30))] })).Failure);

            var raceSchedule = schedule with { Members = [], Slots = [new(2, new(16, 0), new(17, 0))] };
            var raced = await Task.WhenAll(Create(raceSchedule with { Name = "Race A" }), Create(raceSchedule with { Name = "Race B" }));
            Assert.Single(raced, result => result.Failure == GroupCreateFailure.None);
            Assert.All(raced.Where(r => r.Failure != GroupCreateFailure.None), result => Assert.True(result.Failure is GroupCreateFailure.Conflict or GroupCreateFailure.ScheduleConflict));
            var membershipRace = await Task.WhenAll(Create(Command("Join A") with { Members = [new(second.Id, second.RowVersion)] }), Create(Command("Join B") with { Members = [new(second.Id, second.RowVersion)] }));
            Assert.Single(membershipRace, result => result.Failure == GroupCreateFailure.None);
            Assert.Equal(1, await db.GroupMemberships.CountAsync(m => m.StudentId == second.Id && m.LeftAtUtc == null));
            var futureRule = new RecurringSessionSeries(Guid.NewGuid(), owner.Id, RecurringSessionSeriesKind.RegularGroupSchedule,
                legacy.Id, DayOfWeek.Monday, new(2028, 1, 1), null, new(16, 0), new(17, 0), "Europe/Zagreb", now);
            db.RecurringSessionSeries.Add(futureRule);
            await db.SaveChangesAsync();
            var futureConflict = await Create(schedule with { Name = "Unmaterialized conflict", Members = [], StartsOn = new(2028, 1, 1), LocationId = null });
            Assert.Equal(GroupCreateFailure.ScheduleConflict, futureConflict.Failure);
            var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20260901224924_AddStudentEditingConcurrency"));
            Assert.Equal(51000, downgrade.Number);
        }
        finally
        {
            await using var cleanup = new Plus5DbContext(options);
            Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 9, 7, 0, 0, 0, TimeSpan.Zero); }
    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"))) Skip = "Local disposable SQL credentials required.";
        }
    }
}
