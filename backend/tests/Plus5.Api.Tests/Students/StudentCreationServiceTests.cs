using Microsoft.EntityFrameworkCore;
using Plus5.Application.Students;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Students;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Students;

public sealed class StudentCreationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesStudentWithoutProgramAndPrimaryGuardian()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "6R", "Šesti razred", 6);
        await using var dbContext = CreateDbContext();
        dbContext.SchoolGrades.Add(grade);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            teacherId,
            Command(grade.Id) with
            {
                Guardian = new StudentGuardianInput(
                    "Iva", "Ivić", "iva@example.test", "+38599111222"),
            },
            CancellationToken.None);

        Assert.Equal(StudentCreateFailure.None, result.Failure);
        var student = await dbContext.Students.SingleAsync();
        Assert.Equal(result.StudentId, student.Id);
        Assert.Equal(teacherId, student.TeacherAccountId);
        Assert.Null(student.ProgramId);
        Assert.Null(student.DeliveryMode);
        var guardian = await dbContext.Guardians.SingleAsync();
        Assert.Equal(student.Id, guardian.StudentId);
        Assert.True(guardian.IsPrimary);
    }

    [Fact]
    public async Task GroupCreationAddsMembershipAndRecordsGroupChange()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Matematika 7", Now);
        var group = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Orion", 2,
            GroupStatus.Active, Now);
        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, program, group);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            teacherId,
            Command(grade.Id) with
            {
                ProgramId = program.Id,
                DeliveryMode = StudentCreationDeliveryMode.Group,
                GroupId = group.Id,
            },
            CancellationToken.None);

        Assert.Equal(StudentCreateFailure.None, result.Failure);
        var student = await dbContext.Students.SingleAsync();
        Assert.Equal(program.Id, student.ProgramId);
        Assert.Equal(DeliveryMode.Group, student.DeliveryMode);
        var membership = await dbContext.GroupMemberships.SingleAsync();
        Assert.Equal(student.Id, membership.StudentId);
        Assert.Equal(group.Id, membership.GroupId);
        Assert.Equal(Now, group.UpdatedAtUtc);
    }

    [Fact]
    public async Task RejectsForeignProgramWithoutDisclosingIt()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "8R", "Osmi razred", 8);
        var foreignProgram = new TeachingProgram(
            Guid.NewGuid(), Guid.NewGuid(), "Tuđi program", Now);
        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, foreignProgram);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            teacherId,
            Command(grade.Id) with
            {
                ProgramId = foreignProgram.Id,
                DeliveryMode = StudentCreationDeliveryMode.Individual,
            },
            CancellationToken.None);

        Assert.Equal(StudentCreateFailure.ProgramNotFound, result.Failure);
        Assert.Empty(dbContext.Students);
    }

    [Fact]
    public async Task OptionsAreOwnerScopedAndOnlyExposeAvailableGroups()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "5R", "Peti razred", 5);
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "Program A", Now);
        var otherProgram = new TeachingProgram(Guid.NewGuid(), otherTeacherId, "Program B", Now);
        var activeGroup = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Aktivna", 4,
            GroupStatus.Active, Now);
        var inactiveGroup = new Group(
            Guid.NewGuid(), teacherId, program.Id, grade.Id, "Neaktivna", 4,
            GroupStatus.Inactive, Now);
        await using var dbContext = CreateDbContext();
        dbContext.AddRange(grade, program, otherProgram, activeGroup, inactiveGroup);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var options = await service.GetOptionsAsync(
            teacherId, program.Id, CancellationToken.None);

        Assert.Equal(grade.Id, Assert.Single(options.SchoolGrades).Id);
        Assert.Equal(program.Id, Assert.Single(options.Programs).Id);
        Assert.Equal(activeGroup.Id, Assert.Single(options.Groups).Id);
    }

    private static StudentCreateCommand Command(Guid schoolGradeId) => new(
        "Ana", "Anić", schoolGradeId, null, null, null, null, null,
        null, null, null, StudentCreationStatus.Active, null);

    private static EfStudentCreationService CreateService(Plus5DbContext dbContext) =>
        new(dbContext, new FixedTimeProvider(Now));

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
