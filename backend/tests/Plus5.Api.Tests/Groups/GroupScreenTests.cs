using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;
using TeachingSession = Plus5.Domain.Scheduling.Session;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupScreenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueriesAreOwnerScopedFilteredPagedAndExcludeArchivedGroups()
    {
        await using var db = CreateDb();
        var (owner, group, student) = await Seed(db);
        var archived = new Group(Guid.NewGuid(), owner, group.ProgramId, group.SchoolGradeId, "Archived", 4, GroupStatus.Inactive, Now.AddDays(-1));
        archived.Archive(0, Now);
        db.Groups.Add(archived);
        await db.SaveChangesAsync();
        var query = new EfGroupQuery(db, new FixedClock());
        var page = await query.GetPageAsync(owner, new(1, 1, "Orion", group.ProgramId, 1), CancellationToken.None);
        Assert.Equal(group.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
        Assert.Empty((await query.GetPageAsync(owner, new(int.MaxValue, 100), CancellationToken.None)).Items);
        Assert.Null(await query.GetAsync(Guid.NewGuid(), group.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(owner, archived.Id, CancellationToken.None));
        Assert.Null(await query.GetStudentsAsync(Guid.NewGuid(), group.Id, new(1, 8), false, CancellationToken.None));
        Assert.Null(await query.GetSessionsAsync(Guid.NewGuid(), group.Id, new(1, 8), CancellationToken.None));
        Assert.Empty((await query.GetPageAsync(owner, new(1, 8, Status: 2), CancellationToken.None)).Items);
        Assert.Equal(student.Id, Assert.Single((await query.GetStudentsAsync(owner, group.Id, new(1, 8), true, CancellationToken.None))!.Items).Id);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.GetPageAsync(owner, new(0, 101), CancellationToken.None));
    }

    [Fact]
    public async Task SummaryCountsMembershipsAndActualNonCancelledSessionsInZagrebWeek()
    {
        await using var db = CreateDb();
        var (owner, group, student) = await Seed(db);
        db.GroupMemberships.Add(new(Guid.NewGuid(), owner, group.Id, student.Id, Now.AddDays(-1)));
        db.RecurringSessionSeries.Add(new(Guid.NewGuid(), owner, RecurringSessionSeriesKind.RegularGroupSchedule, group.Id,
            DayOfWeek.Wednesday, new(2026, 9, 1), new(2026, 12, 1), new(16, 0), new(17, 30), "Europe/Zagreb", Now.AddDays(-1)));
        var cancelled = Session(owner, group.Id, Now.AddDays(1));
        cancelled.Cancel(Now);
        db.Sessions.AddRange(Session(owner, group.Id, Now.AddHours(1)), cancelled, Session(owner, group.Id, Now.AddDays(8)),
            Session(owner, group.Id, new(2026, 8, 30, 22, 0, 0, TimeSpan.Zero)), // Monday midnight Zagreb.
            Session(owner, group.Id, new(2026, 8, 30, 21, 59, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();
        var query = new EfGroupQuery(db, new FixedClock());
        var summary = await query.GetOverviewAsync(owner, CancellationToken.None);
        Assert.Equal(1, summary.TotalGroups);
        Assert.Equal(1, summary.ActiveGroups);
        Assert.Equal(1, summary.Students);
        Assert.Equal(1, summary.AvailableSeats);
        Assert.Equal(2, summary.SessionsThisWeek);
        Assert.Equal(new DateOnly(2026, 8, 31), summary.WeekStartsOn);
        Assert.Equal(1, (await query.GetAsync(owner, group.Id, CancellationToken.None))!.MemberCount);
        Assert.Equal(new TimeOnly(16, 0), Assert.Single((await query.GetAsync(owner, group.Id, CancellationToken.None))!.Slots).Start);
        var sessions = await query.GetSessionsAsync(owner, group.Id, new(1, 1), CancellationToken.None);
        Assert.Equal(2, sessions!.TotalCount);
        Assert.Single(sessions.Items);
        Assert.Empty((await query.GetStudentsAsync(owner, group.Id, new(1, 8), true, CancellationToken.None))!.Items);
    }

    [Fact]
    public async Task CandidatesPrioritizeMatchingGradeAndProgramWithoutEnforcingGradeMatch()
    {
        await using var db = CreateDb();
        var (owner, group, student) = await Seed(db);
        var otherGrade = new SchoolGrade(Guid.NewGuid(), "8R", "Osmi razred", 8);
        var other = new Student(Guid.NewGuid(), owner, otherGrade.Id, "A", "A", StudentStatus.Active, Now.AddDays(-1));
        db.AddRange(otherGrade, other);
        await db.SaveChangesAsync();
        var query = new EfGroupQuery(db, new FixedClock());
        var candidates = await query.GetStudentsAsync(owner, group.Id, new(1, 8), true, CancellationToken.None);
        Assert.Equal(student.Id, candidates!.Items[0].Id);
        Assert.True(candidates.Items[0].Recommended);
        Assert.False(candidates.Items[1].Recommended);
        Assert.Equal(GroupMembershipResult.Saved, await new EfGroupMembershipService(db, new FixedClock()).ChangeAsync(owner, group.Id, other.Id,
            new(true, group.RowVersion, other.RowVersion), CancellationToken.None));
        Assert.Equal(otherGrade.Id, other.SchoolGradeId);
        Assert.Equal(group.ProgramId, other.ProgramId);
    }

    [Fact]
    public async Task JoinAndLeavePreserveHistoryAndAdministrativeData()
    {
        await using var db = CreateDb();
        var (owner, group, student) = await Seed(db);
        var service = new EfGroupMembershipService(db, new FixedClock());
        Assert.Equal(GroupMembershipResult.Saved, await service.ChangeAsync(owner, group.Id, student.Id,
            new(true, group.RowVersion, student.RowVersion), CancellationToken.None));
        Assert.Equal(DeliveryMode.Group, student.DeliveryMode);
        Assert.Equal(GroupMembershipResult.MembershipChanged, await service.ChangeAsync(owner, group.Id, student.Id,
            new(true, group.RowVersion, student.RowVersion), CancellationToken.None));
        Assert.Equal(GroupMembershipResult.Saved, await service.ChangeAsync(owner, group.Id, student.Id,
            new(false, group.RowVersion, student.RowVersion), CancellationToken.None));
        Assert.Equal(DeliveryMode.Individual, student.DeliveryMode);
        Assert.Equal(group.ProgramId, student.ProgramId);
        Assert.Null(student.ArchivedAtUtc);
        Assert.NotNull((await db.GroupMemberships.SingleAsync()).LeftAtUtc);
        Assert.Equal("Ana", student.FirstName);
    }

    [Fact]
    public async Task WritesRejectForeignOwnerStaleVersionsCapacityAndInactiveGroups()
    {
        await using var db = CreateDb();
        var (owner, group, student) = await Seed(db);
        var service = new EfGroupMembershipService(db, new FixedClock());
        Assert.Equal(GroupMembershipResult.NotFound, await service.ChangeAsync(Guid.NewGuid(), group.Id, student.Id, new(true, [], []), CancellationToken.None));
        Assert.Equal(GroupMembershipResult.Conflict, await service.ChangeAsync(owner, group.Id, student.Id, new(true, [1], student.RowVersion), CancellationToken.None));
        Assert.Equal(GroupMembershipResult.Conflict, await service.ChangeAsync(owner, group.Id, student.Id, new(true, group.RowVersion, [1]), CancellationToken.None));
        var paused = new Group(Guid.NewGuid(), owner, group.ProgramId, group.SchoolGradeId, "Paused", 1, GroupStatus.OnHold, Now.AddDays(-1));
        db.Groups.Add(paused);
        group.ChangeCapacity(1, 0, Now);
        db.GroupMemberships.Add(new(Guid.NewGuid(), owner, group.Id, Guid.NewGuid(), Now.AddDays(-1)));
        await db.SaveChangesAsync();
        Assert.Equal(GroupMembershipResult.Full, await service.ChangeAsync(owner, group.Id, student.Id, new(true, group.RowVersion, student.RowVersion), CancellationToken.None));
        Assert.Equal(GroupMembershipResult.Unavailable, await service.ChangeAsync(owner, paused.Id, student.Id, new(true, paused.RowVersion, student.RowVersion), CancellationToken.None));
        Assert.Equal(DeliveryMode.Individual, student.DeliveryMode);
    }

    private static TeachingSession Session(Guid owner, Guid group, DateTimeOffset start) => new(Guid.NewGuid(), owner, DeliveryMode.Group, group, start, start.AddHours(1), "Europe/Zagreb", Now.AddDays(-10));
    private static Plus5DbContext CreateDb() => new(new DbContextOptionsBuilder<Plus5DbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(Guid Owner, Group Group, Student Student)> Seed(Plus5DbContext db)
    {
        var owner = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var program = new TeachingProgram(Guid.NewGuid(), owner, "Matematika 7", Now.AddDays(-1));
        var group = new Group(Guid.NewGuid(), owner, program.Id, grade.Id, "Orion", 2, GroupStatus.Active, Now.AddDays(-1));
        var student = new Student(Guid.NewGuid(), owner, grade.Id, "Ana", "Anić", StudentStatus.Active, Now.AddDays(-1), program.Id, DeliveryMode.Individual);
        var foreignOwner = Guid.NewGuid();
        var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreignOwner, "Foreign program", Now.AddDays(-1));
        db.AddRange(grade, program, group, student, foreignProgram,
            new Group(Guid.NewGuid(), foreignOwner, foreignProgram.Id, grade.Id, "Foreign", 99, GroupStatus.Active, Now.AddDays(-1)));
        await db.SaveChangesAsync();
        return (owner, group, student);
    }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
}
