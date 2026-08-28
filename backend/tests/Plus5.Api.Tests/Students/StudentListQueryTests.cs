using Microsoft.EntityFrameworkCore;
using Plus5.Application.Students;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Students;
using TeachingProgram = Plus5.Domain.Teaching.Program;
using TeachingSession = Plus5.Domain.Scheduling.Session;

namespace Plus5.Api.Tests.Students;

public sealed class StudentListQueryTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PageIsOwnerScopedSearchableAndUsesOnlyHeldSessions()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Matematika 7", CreatedAtUtc);
        var group = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Grupa Orion", 6,
            GroupStatus.Active, CreatedAtUtc);
        var ana = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Ana", "Anić", StudentStatus.Active,
            CreatedAtUtc, program.Id, DeliveryMode.Individual, nickname: "Ani");
        var borna = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Borna", "Barić", StudentStatus.OnHold,
            CreatedAtUtc, program.Id, DeliveryMode.Group);
        var foreignStudent = new Student(
            Guid.NewGuid(), otherTeacherId, grade.Id, "Ana", "Tuđa", StudentStatus.Active,
            CreatedAtUtc);

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, program, group, ana, borna, foreignStudent);
        dbContext.GroupMemberships.Add(new GroupMembership(
            Guid.NewGuid(), teacherId, group.Id, borna.Id, CreatedAtUtc));

        var heldAt = CreatedAtUtc.AddDays(1).AddHours(2);
        var heldSession = CreateSession(teacherId, DeliveryMode.Individual, ana.Id, heldAt);
        heldSession.Start(heldAt.AddMinutes(1));
        heldSession.Complete(heldAt.AddMinutes(2));
        var scheduledSession = CreateSession(
            teacherId, DeliveryMode.Individual, ana.Id, heldAt.AddDays(1));
        dbContext.Sessions.AddRange(heldSession, scheduledSession);
        await dbContext.SaveChangesAsync();

        var query = new EfStudentListQuery(dbContext);
        var page = await query.GetPageAsync(
            teacherId,
            new StudentListCriteria(1, 25, "Ani", program.Id, null, null, grade.Id),
            CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(ana.Id, item.Id);
        Assert.Equal(heldSession.EndsAtUtc, item.LastSessionAtUtc);
        Assert.Equal(program.Name, item.ProgramName);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task GroupContextAndOverviewExcludeArchivedAndForeignStudents()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "8R", "Osmi razred", 8);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Fizika 8", CreatedAtUtc);
        var group = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Grupa Tesla", 6,
            GroupStatus.Active, CreatedAtUtc);
        var active = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Dora", "Delić", StudentStatus.Active,
            CreatedAtUtc, program.Id, DeliveryMode.Group);
        var withoutProgram = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Ema", "Erić", StudentStatus.Inactive,
            CreatedAtUtc);
        var archived = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Filip", "Ferić", StudentStatus.Active,
            CreatedAtUtc);
        archived.Archive(CreatedAtUtc.AddMinutes(1));
        var foreignStudent = new Student(
            Guid.NewGuid(), otherTeacherId, grade.Id, "Goran", "Galić", StudentStatus.Active,
            CreatedAtUtc);

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, program, group, active, withoutProgram, archived, foreignStudent);
        dbContext.GroupMemberships.Add(new GroupMembership(
            Guid.NewGuid(), teacherId, group.Id, active.Id, CreatedAtUtc));
        var heldAt = CreatedAtUtc.AddDays(2);
        var groupSession = CreateSession(teacherId, DeliveryMode.Group, group.Id, heldAt);
        groupSession.Start(heldAt.AddMinutes(1));
        groupSession.Complete(heldAt.AddMinutes(2));
        dbContext.Sessions.Add(groupSession);
        await dbContext.SaveChangesAsync();

        var query = new EfStudentListQuery(dbContext);
        var page = await query.GetPageAsync(
            teacherId,
            new StudentListCriteria(1, 25, null, null, null, null, null),
            CancellationToken.None);
        var overview = await query.GetOverviewAsync(teacherId, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        var groupItem = Assert.Single(page.Items, item => item.Id == active.Id);
        Assert.Equal(group.Id, groupItem.GroupId);
        Assert.Equal(group.Name, groupItem.GroupName);
        Assert.Equal(groupSession.EndsAtUtc, groupItem.LastSessionAtUtc);
        Assert.Equal(2, overview.TotalCount);
        Assert.Equal(1, overview.ActiveCount);
        Assert.Equal(1, overview.InactiveCount);
        Assert.Equal(1, overview.WithoutProgramCount);
        Assert.Equal(1, Assert.Single(overview.ProgramCounts).StudentCount);
        Assert.Equal(program.Id, Assert.Single(overview.Programs).Id);
        Assert.Equal(grade.Id, Assert.Single(overview.SchoolGrades).Id);
    }

    private static TeachingSession CreateSession(
        Guid teacherId,
        DeliveryMode deliveryMode,
        Guid contextId,
        DateTimeOffset startsAtUtc) => new(
            Guid.NewGuid(), teacherId, deliveryMode, contextId, startsAtUtc,
            startsAtUtc.AddHours(1), "Europe/Zagreb", CreatedAtUtc);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
