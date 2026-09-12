using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

public sealed class GroupEditingSqlTests
{
    [LocalSqlFact]
    public async Task EditRulesAndFutureScheduleVersioningAreAtomic()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase37Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options;
        var clock = new FixedClock();
        var now = clock.GetUtcNow();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = new UserAccount(Guid.NewGuid(), "edit@test.local", "EDIT@TEST.LOCAL", now.AddDays(-10));
            var foreignOwner = new UserAccount(Guid.NewGuid(), "foreign-edit@test.local", "FOREIGN-EDIT@TEST.LOCAL", now.AddDays(-10));
            var grade = new SchoolGrade(Guid.NewGuid(), "E7", "Edit grade", 7);
            var nextGrade = new SchoolGrade(Guid.NewGuid(), "E8", "Next grade", 8);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "Original program", now.AddDays(-10));
            var nextProgram = new TeachingProgram(Guid.NewGuid(), owner.Id, "Next program", now.AddDays(-10));
            var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreignOwner.Id, "Foreign", now.AddDays(-10));
            var location = new Location(Guid.NewGuid(), owner.Id, "Room 1", now.AddDays(-10));
            var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Editable", 3, GroupStatus.Active, now.AddDays(-10));
            var student = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Ana", "Edit", StudentStatus.Active, now.AddDays(-10), program.Id, DeliveryMode.Group);
            var secondStudent = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Borna", "Edit", StudentStatus.Active, now.AddDays(-10), program.Id, DeliveryMode.Group);
            var membership = new GroupMembership(Guid.NewGuid(), owner.Id, group.Id, student.Id, now.AddDays(-9));
            var secondMembership = new GroupMembership(Guid.NewGuid(), owner.Id, group.Id, secondStudent.Id, now.AddDays(-9));
            var emptyGroup = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Empty", 3, GroupStatus.Active, now.AddDays(-10));
            var scheduled = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "Scheduled", 3, GroupStatus.Active, now.AddDays(-10));
            var oldSeries = new RecurringSessionSeries(Guid.NewGuid(), owner.Id,
                RecurringSessionSeriesKind.RegularGroupSchedule, scheduled.Id, DayOfWeek.Monday,
                new(2026, 9, 1), null, new(16, 0), new(17, 0), GroupScheduleGenerator.TimeZoneId,
                now.AddDays(-6), location.Id);
            var oldSession = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, scheduled.Id,
                new(2026, 9, 14, 14, 0, 0, TimeSpan.Zero), new(2026, 9, 14, 15, 0, 0, TimeSpan.Zero),
                GroupScheduleGenerator.TimeZoneId, now.AddDays(-6), locationId: location.Id,
                recurringSessionSeriesId: oldSeries.Id, seriesOccurrenceDate: new(2026, 9, 14));
            var blockingSession = new Session(Guid.NewGuid(), owner.Id, DeliveryMode.Group, emptyGroup.Id,
                new(2026, 9, 8, 16, 0, 0, TimeSpan.Zero), new(2026, 9, 8, 17, 0, 0, TimeSpan.Zero),
                GroupScheduleGenerator.TimeZoneId, now.AddDays(-1), locationId: location.Id);
            db.AddRange(owner, foreignOwner, grade, nextGrade, program, nextProgram, foreignProgram,
                location, group, student, secondStudent, membership, secondMembership, emptyGroup, scheduled, oldSeries,
                oldSession, blockingSession);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            async Task<GroupEditResult> Update(Guid id, GroupEditCommand command)
            {
                await using var writer = new Plus5DbContext(options);
                return await new EfGroupEditingService(writer, clock).UpdateAsync(owner.Id, id, command, CancellationToken.None);
            }
            async Task<GroupEditItem> Read(Guid id)
            {
                await using var reader = new Plus5DbContext(options);
                return (await new EfGroupEditingQuery(reader, clock).GetAsync(owner.Id, id, CancellationToken.None))!;
            }
            GroupEditCommand Command(GroupEditItem item)
            {
                var first = item.Slots.Count > 0 ? item.Slots[0] : null;
                return new(item.Name, item.Description, item.ProgramId,
                    item.SchoolGradeId, item.Status, item.Capacity, item.RowVersion,
                    item.Slots.Select(slot => new GroupScheduleSlot(slot.DayOfWeek, slot.Start, slot.End)).ToList(),
                    first?.StartsOn, first?.EndsOn, first?.LocationId);
            }

            var withMember = await Read(group.Id);
            Assert.Equal(2, withMember.MemberCount);
            Assert.Equal(GroupEditFailure.ProgramHasActiveMembers,
                (await Update(group.Id, Command(withMember) with { ProgramId = nextProgram.Id })).Failure);
            Assert.Equal(GroupEditFailure.CapacityBelowMembers,
                (await Update(group.Id, Command(withMember) with { Capacity = 1 })).Failure);
            Assert.Equal(program.Id, (await Read(group.Id)).ProgramId);

            var empty = await Read(emptyGroup.Id);
            var emptyResult = await Update(emptyGroup.Id, Command(empty) with
            {
                Name = "Empty updated",
                ProgramId = nextProgram.Id,
                SchoolGradeId = nextGrade.Id,
                Status = 2,
                Capacity = 7,
            });
            Assert.Equal(GroupEditFailure.None, emptyResult.Failure);
            var changed = await Read(emptyGroup.Id);
            Assert.Equal("Empty updated", changed.Name);
            Assert.Equal(nextProgram.Id, changed.ProgramId);
            Assert.Equal(nextGrade.Id, changed.SchoolGradeId);
            Assert.Equal(2, changed.Status);
            Assert.Equal(7, changed.Capacity);
            Assert.Equal(GroupEditFailure.Conflict, (await Update(emptyGroup.Id, Command(empty))).Failure);
            Assert.Equal(GroupEditFailure.NotFound,
                (await Update(emptyGroup.Id, Command(changed) with { ProgramId = foreignProgram.Id })).Failure);

            var beforeSchedule = await Read(scheduled.Id);
            Assert.Single(beforeSchedule.Slots);
            var conflictResult = await Update(scheduled.Id, Command(beforeSchedule) with
            {
                Slots = [new GroupScheduleSlot(2, new(18, 0), new(19, 0))],
                ScheduleStartsOn = new(2026, 9, 8),
                ScheduleEndsOn = null,
            });
            Assert.Equal(GroupEditFailure.ScheduleConflict, conflictResult.Failure);
            Assert.Null((await db.RecurringSessionSeries.SingleAsync(item => item.Id == oldSeries.Id)).SupersededAtUtc);
            db.ChangeTracker.Clear();

            var scheduleResult = await Update(scheduled.Id, Command(beforeSchedule) with
            {
                Slots = [new GroupScheduleSlot(3, new(18, 0), new(19, 0))],
                ScheduleStartsOn = new(2026, 9, 8),
                ScheduleEndsOn = null,
            });
            Assert.Equal(GroupEditFailure.None, scheduleResult.Failure);
            Assert.Equal(12, scheduleResult.SessionCount);
            db.ChangeTracker.Clear();
            var oldRule = await db.RecurringSessionSeries.SingleAsync(item => item.Id == oldSeries.Id);
            Assert.NotNull(oldRule.SupersededAtUtc);
            Assert.Equal(new DateOnly(2026, 9, 7), oldRule.EndsOn);
            Assert.Equal(SessionStatus.Cancelled, (await db.Sessions.SingleAsync(item => item.Id == oldSession.Id)).Status);
            var successor = await db.RecurringSessionSeries.SingleAsync(item => item.PreviousSeriesId == oldSeries.Id);
            Assert.Equal(DayOfWeek.Wednesday, successor.DayOfWeek);
            Assert.Equal(new DateOnly(2026, 9, 8), successor.StartsOn);
            Assert.Equal(12, await db.Sessions.CountAsync(item => item.RecurringSessionSeriesId == successor.Id));
            var currentSchedule = await Read(scheduled.Id);
            Assert.Single(currentSchedule.Slots);
            Assert.Equal(3, currentSchedule.Slots[0].DayOfWeek);

            var removeResult = await Update(scheduled.Id, Command(currentSchedule) with
            {
                Slots = [],
                ScheduleStartsOn = null,
                ScheduleEndsOn = null,
                LocationId = null,
            });
            Assert.Equal(GroupEditFailure.None, removeResult.Failure);
            Assert.Equal(0, removeResult.SessionCount);
            Assert.Empty((await Read(scheduled.Id)).Slots);
            db.ChangeTracker.Clear();
            Assert.All(await db.Sessions.Where(item => item.RecurringSessionSeriesId == successor.Id).ToListAsync(),
                item => Assert.Equal(SessionStatus.Cancelled, item.Status));
            Assert.Null(await new EfGroupEditingQuery(db, clock).GetAsync(foreignOwner.Id, scheduled.Id, CancellationToken.None));
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
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 7, 8, 0, 0, TimeSpan.Zero);
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
                Skip = "Local disposable SQL credentials required.";
        }
    }
}
