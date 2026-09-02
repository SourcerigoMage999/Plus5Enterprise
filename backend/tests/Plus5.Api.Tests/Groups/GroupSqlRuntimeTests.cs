using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupSqlRuntimeTests
{
    [SqlRuntimeFact]
    public async Task RealSqlProjectionMembershipVersionsAndLastSeatRemainConsistent()
    {
        // Opt-in integration gate: credentials must point to a local disposable-test SQL Server.
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433");
        var databaseName = "Plus5_Phase35Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options;
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            var owner = new UserAccount(Guid.NewGuid(), "sql-test@example.test", "SQL-TEST@EXAMPLE.TEST", now);
            var grade = new SchoolGrade(Guid.NewGuid(), "SQL7", "SQL test grade", 7);
            var program = new TeachingProgram(Guid.NewGuid(), owner.Id, "SQL test program", now);
            var group = new Group(Guid.NewGuid(), owner.Id, program.Id, grade.Id, "SQL test group", 1, GroupStatus.Active, now);
            var one = new Student(Guid.NewGuid(), owner.Id, grade.Id, "One", "Test", StudentStatus.Active, now);
            var two = new Student(Guid.NewGuid(), owner.Id, grade.Id, "Two", "Test", StudentStatus.Active, now);
            db.AddRange(owner, grade, program, group, one, two);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.RecurringSessionSeries.Add(new(Guid.NewGuid(), owner.Id,
                Plus5.Domain.Scheduling.RecurringSessionSeriesKind.RegularGroupSchedule, group.Id,
                DayOfWeek.Wednesday, today.AddDays(-1), today.AddDays(30), new(16, 0), new(17, 30), "Europe/Zagreb", now));
            db.Sessions.Add(new(Guid.NewGuid(), owner.Id, DeliveryMode.Group, group.Id, now.AddHours(2), now.AddHours(3), "Europe/Zagreb", now));
            await db.SaveChangesAsync();
            var query = new EfGroupQuery(db, TimeProvider.System);
            Assert.Single((await query.GetPageAsync(owner.Id, new(1, 8), CancellationToken.None)).Items);
            Assert.Equal(1, (await query.GetOverviewAsync(owner.Id, CancellationToken.None)).AvailableSeats);
            Assert.Equal(2, (await query.GetStudentsAsync(owner.Id, group.Id, new(1, 8), true, CancellationToken.None))!.TotalCount);
            Assert.Single((await query.GetSessionsAsync(owner.Id, group.Id, new(1, 8), CancellationToken.None))!.Items);
            Assert.Single((await query.GetAsync(owner.Id, group.Id, CancellationToken.None))!.Slots);

            var oldVersion = group.RowVersion.ToArray();
            async Task<GroupMembershipResult> Join(Guid studentId, byte[] version)
            {
                await using var writer = new Plus5DbContext(options);
                return await new EfGroupMembershipService(writer, TimeProvider.System).ChangeAsync(owner.Id, group.Id, studentId,
                    new(true, oldVersion, version), CancellationToken.None);
            }
            var results = await Task.WhenAll(Join(one.Id, one.RowVersion), Join(two.Id, two.RowVersion));
            Assert.Single(results, result => result == GroupMembershipResult.Saved);
            Assert.All(results.Where(result => result != GroupMembershipResult.Saved), result =>
                Assert.True(result is GroupMembershipResult.Conflict or GroupMembershipResult.Full));
            db.ChangeTracker.Clear();
            var detail = (await query.GetAsync(owner.Id, group.Id, CancellationToken.None))!;
            Assert.Equal(1, detail.MemberCount);
            Assert.False(oldVersion.SequenceEqual(detail.RowVersion));
            Assert.Equal(0, (await query.GetOverviewAsync(owner.Id, CancellationToken.None)).AvailableSeats);
            var member = Assert.Single((await query.GetStudentsAsync(owner.Id, group.Id, new(1, 8), false, CancellationToken.None))!.Items);
            var service = new EfGroupMembershipService(db, TimeProvider.System);
            Assert.Equal(GroupMembershipResult.Conflict, await service.ChangeAsync(owner.Id, group.Id, member.Id,
                new(false, oldVersion, member.RowVersion), CancellationToken.None));
            Assert.Equal(GroupMembershipResult.Saved, await service.ChangeAsync(owner.Id, group.Id, member.Id,
                new(false, detail.RowVersion, member.RowVersion), CancellationToken.None));
            db.ChangeTracker.Clear();
            Assert.NotNull((await db.GroupMemberships.SingleAsync()).LeftAtUtc);
            Assert.Equal(DeliveryMode.Individual, (await db.Students.SingleAsync(student => student.Id == member.Id)).DeliveryMode);
            Assert.Equal(1, (await query.GetOverviewAsync(owner.Id, CancellationToken.None)).AvailableSeats);
        }
        finally
        {
            // Only the randomly named database created by this test is ever deleted. Never the application database.
            await using var cleanup = new Plus5DbContext(options);
            Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private sealed class SqlRuntimeFactAttribute : FactAttribute
    {
        public SqlRuntimeFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
                Skip = "Set PLUS5_TEST_SQL_CONNECTION_STRING to a local SQL Server with disposable-database permissions to run the runtime gate.";
        }
    }
}
