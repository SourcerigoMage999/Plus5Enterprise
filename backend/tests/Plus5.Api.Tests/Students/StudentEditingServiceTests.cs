using Microsoft.EntityFrameworkCore;
using Plus5.Application.Students;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Students;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Students;

public sealed class StudentEditingServiceTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = CreatedAt.AddHours(2);

    [Fact]
    public async Task EditReadIsOwnerScopedAndIncludesGuardiansAndMembership()
    {
        var fixture = await CreateFixtureAsync(withMembership: true);
        await using var dbContext = fixture.Context;
        var service = CreateService(dbContext);

        var owned = await service.GetAsync(fixture.TeacherId, fixture.Student.Id, CancellationToken.None);
        var foreign = await service.GetAsync(Guid.NewGuid(), fixture.Student.Id, CancellationToken.None);

        Assert.NotNull(owned);
        Assert.Equal(fixture.Group.Id, owned.GroupId);
        Assert.True(Assert.Single(owned.Guardians).IsPrimary);
        Assert.Null(foreign);
    }

    [Fact]
    public async Task UpdateChangesProfileMovesMembershipAndAddsGuardianAtomically()
    {
        var fixture = await CreateFixtureAsync(withMembership: false);
        await using var dbContext = fixture.Context;
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            fixture.TeacherId,
            fixture.Student.Id,
            Command(fixture) with
            {
                FirstName = "  Anamarija ",
                DeliveryMode = StudentCreationDeliveryMode.Group,
                GroupId = fixture.Group.Id,
                Guardians =
                [
                    new(fixture.Guardian.Id, "Ivana", "Kovač", "majka", "ivana@example.test", null, false),
                    new(null, "Marko", "Kovač", "otac", null, "+38599111", true),
                ],
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Anamarija", fixture.Student.FirstName);
        Assert.Equal(DeliveryMode.Group, fixture.Student.DeliveryMode);
        Assert.Equal(fixture.Group.Id, (await dbContext.GroupMemberships.SingleAsync()).GroupId);
        Assert.Equal(2, await dbContext.Guardians.CountAsync());
        Assert.Equal("Marko", (await dbContext.Guardians.SingleAsync(guardian => guardian.IsPrimary)).FirstName);
    }

    [Fact]
    public async Task UpdateRejectsRemovingAnExistingGuardian()
    {
        var fixture = await CreateFixtureAsync(withMembership: false);
        await using var dbContext = fixture.Context;
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            fixture.TeacherId,
            fixture.Student.Id,
            Command(fixture) with { Guardians = [] },
            CancellationToken.None);

        Assert.Equal(StudentEditFailure.GuardianSetMismatch, result.Failure);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ArchiveEndsMembershipAndHidesStudentFromEditRead()
    {
        var fixture = await CreateFixtureAsync(withMembership: true);
        await using var dbContext = fixture.Context;
        var service = CreateService(dbContext);

        var result = await service.ArchiveAsync(
            fixture.TeacherId,
            fixture.Student.Id,
            fixture.Student.RowVersion,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(fixture.Student.ArchivedAtUtc);
        Assert.NotNull((await dbContext.GroupMemberships.SingleAsync()).LeftAtUtc);
        Assert.Null(await service.GetAsync(fixture.TeacherId, fixture.Student.Id, CancellationToken.None));
    }

    private static StudentEditCommand Command(Fixture fixture) => new(
        fixture.Student.RowVersion,
        "Ana",
        "Kovač",
        fixture.Grade.Id,
        null,
        null,
        null,
        null,
        null,
        null,
        fixture.Program.Id,
        StudentCreationDeliveryMode.Individual,
        null,
        StudentCreationStatus.Active,
        [new(fixture.Guardian.Id, "Ivana", "Kovač", "majka", null, null, true)]);

    private static EfStudentEditingService CreateService(Plus5DbContext context) =>
        new(context, new FixedTimeProvider(UpdatedAt));

    private static async Task<Fixture> CreateFixtureAsync(bool withMembership)
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var context = new Plus5DbContext(options);
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Engleski 7", CreatedAt);
        var group = new Group(Guid.NewGuid(), teacherId, program.Id, grade.Id, "Orion", 5, GroupStatus.Active, CreatedAt);
        var student = new Student(Guid.NewGuid(), teacherId, grade.Id, "Ana", "Kovač", StudentStatus.Active, CreatedAt, program.Id, withMembership ? DeliveryMode.Group : DeliveryMode.Individual);
        var guardian = new Guardian(Guid.NewGuid(), student.Id, "Ivana", "Kovač", true, CreatedAt, "majka");
        context.AddRange(grade, program, group, student, guardian);
        if (withMembership)
        {
            context.GroupMemberships.Add(new GroupMembership(Guid.NewGuid(), teacherId, group.Id, student.Id, CreatedAt));
        }
        await context.SaveChangesAsync();
        return new Fixture(context, teacherId, grade, program, group, student, guardian);
    }

    private sealed record Fixture(Plus5DbContext Context, Guid TeacherId, SchoolGrade Grade, TeachingProgram Program, Group Group, Student Student, Guardian Guardian);
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
