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

public sealed class StudentDossierQueryTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnsOwnedProfileGuardianGroupAndRelevantSessions()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Engleski 7", Now.AddDays(-30));
        var group = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Grupa Orion", 6,
            GroupStatus.Active, Now.AddDays(-20));
        var student = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Ana", "Anić", StudentStatus.Active,
            Now.AddDays(-15), program.Id, DeliveryMode.Group, nickname: "Ani",
            schoolName: "OŠ Plus", email: "ana@example.test");
        var guardian = new Guardian(
            Guid.NewGuid(), student.Id, "Iva", "Anić", true, Now.AddDays(-15),
            relationship: "Majka", email: "iva@example.test");
        var membership = new GroupMembership(
            Guid.NewGuid(), teacherId, group.Id, student.Id, Now.AddDays(-14));
        var held = CreateSession(teacherId, DeliveryMode.Group, group.Id, Now.AddDays(-2), "Prošli sat");
        held.Start(Now.AddDays(-2).AddMinutes(1));
        held.Complete(Now.AddDays(-2).AddMinutes(2));
        var next = CreateSession(teacherId, DeliveryMode.Group, group.Id, Now.AddDays(1), "Sljedeći sat");

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, program, group, student, guardian, membership, held, next);
        await dbContext.SaveChangesAsync();

        var result = await new EfStudentDossierQuery(dbContext, new FixedTimeProvider(Now))
            .GetAsync(teacherId, student.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Ani", result.Nickname);
        Assert.Equal("OŠ Plus", result.SchoolName);
        Assert.Equal(program.Name, result.Program?.Name);
        Assert.Equal(group.Name, result.Group?.Name);
        Assert.Equal("Iva", result.PrimaryGuardian?.FirstName);
        Assert.Equal(next.Id, result.NextSession?.Id);
        Assert.Equal(held.Id, result.LastHeldSession?.Id);
    }

    [Fact]
    public async Task MissingForeignAndArchivedStudentsShareTheNotFoundResult()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "8R", "Osmi razred", 8);
        var foreign = new Student(
            Guid.NewGuid(), otherTeacherId, grade.Id, "Tuđa", "Učenica",
            StudentStatus.Active, Now.AddDays(-2));
        var archived = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Arhivirana", "Učenica",
            StudentStatus.Active, Now.AddDays(-2));
        archived.Archive(Now.AddDays(-1));

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, foreign, archived);
        await dbContext.SaveChangesAsync();
        var query = new EfStudentDossierQuery(dbContext, new FixedTimeProvider(Now));

        Assert.Null(await query.GetAsync(teacherId, Guid.NewGuid(), CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, foreign.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, archived.Id, CancellationToken.None));
    }

    private static TeachingSession CreateSession(
        Guid teacherId,
        DeliveryMode deliveryMode,
        Guid contextId,
        DateTimeOffset startsAtUtc,
        string title) => new(
            Guid.NewGuid(), teacherId, deliveryMode, contextId, startsAtUtc,
            startsAtUtc.AddHours(1), "Europe/Zagreb", Now.AddDays(-10), title);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
