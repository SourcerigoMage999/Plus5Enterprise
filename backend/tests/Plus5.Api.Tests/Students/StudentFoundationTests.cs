using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Students;

public sealed class StudentFoundationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StudentPreservesRequiredProfileAndNormalizesOptionalText()
    {
        var teacherAccountId = Guid.NewGuid();
        var schoolGradeId = Guid.NewGuid();

        var student = new Student(
            Guid.NewGuid(),
            teacherAccountId,
            schoolGradeId,
            "  Ana ",
            " Kovač  ",
            StudentStatus.Active,
            CreatedAtUtc,
            nickname: "  Ani  ",
            email: "  ana@example.test  ",
            phone: "  +385 91 555 0101  ");

        Assert.Equal(teacherAccountId, student.TeacherAccountId);
        Assert.Equal(schoolGradeId, student.SchoolGradeId);
        Assert.Equal("Ana", student.FirstName);
        Assert.Equal("Kovač", student.LastName);
        Assert.Equal("Ani", student.Nickname);
        Assert.Equal("ana@example.test", student.Email);
        Assert.Equal("+385 91 555 0101", student.Phone);
        Assert.Null(student.ProgramId);
        Assert.Null(student.DeliveryMode);
    }

    [Fact]
    public void StudentRequiresProgramAndDeliveryModeTogether()
    {
        Assert.Throws<ArgumentException>(() => CreateStudent(
            programId: Guid.NewGuid(),
            deliveryMode: null));
        Assert.Throws<ArgumentException>(() => CreateStudent(
            programId: null,
            deliveryMode: DeliveryMode.Individual));

        var student = CreateStudent(Guid.NewGuid(), DeliveryMode.Individual);

        Assert.NotNull(student.ProgramId);
        Assert.Equal(DeliveryMode.Individual, student.DeliveryMode);
    }

    [Fact]
    public void ArchiveMakesStudentInactiveAndPreservesTimestamp()
    {
        var student = CreateStudent();
        var archivedAtUtc = CreatedAtUtc.AddDays(1);

        student.Archive(archivedAtUtc);

        Assert.Equal(StudentStatus.Inactive, student.Status);
        Assert.Equal(archivedAtUtc, student.ArchivedAtUtc);
        Assert.Equal(archivedAtUtc, student.UpdatedAtUtc);
    }

    [Fact]
    public void ArchiveRejectsTimestampBeforeCreation()
    {
        var student = CreateStudent();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            student.Archive(CreatedAtUtc.AddTicks(-1)));
    }

    [Fact]
    public void GuardianIsAnOptionalStudentOwnedContact()
    {
        var studentId = Guid.NewGuid();

        var guardian = new Guardian(
            Guid.NewGuid(),
            studentId,
            "  Ivana  ",
            "  Kovač ",
            true,
            CreatedAtUtc,
            "  majka  ",
            "  ivana@example.test  ");

        Assert.Equal(studentId, guardian.StudentId);
        Assert.Equal("Ivana", guardian.FirstName);
        Assert.Equal("Kovač", guardian.LastName);
        Assert.Equal("majka", guardian.Relationship);
        Assert.True(guardian.IsPrimary);
        Assert.Null(guardian.Phone);
    }

    [Fact]
    public void EfModelProtectsStudentOwnershipAndReferenceBoundaries()
    {
        using var dbContext = CreateDbContext();
        var student = dbContext.Model.FindEntityType(typeof(Student))!;
        var foreignKeys = student.GetForeignKeys().ToArray();

        Assert.Equal(3, foreignKeys.Length);
        Assert.All(foreignKeys, foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(TeachingProgram)
            && foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Student.TeacherAccountId),
                nameof(Student.ProgramId),
            ]));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(SchoolGrade));
    }

    [Fact]
    public void EfModelAllowsOnlyOnePrimaryGuardianPerStudent()
    {
        using var dbContext = CreateDbContext();
        var guardian = dbContext.Model.FindEntityType(typeof(Guardian))!;
        var primaryIndex = Assert.Single(
            guardian.GetIndexes(),
            index => index.IsUnique);

        Assert.Equal(nameof(Guardian.StudentId), Assert.Single(primaryIndex.Properties).Name);
        Assert.Equal("[IsPrimary] = 1", primaryIndex.GetFilter());
        Assert.Equal(
            DeleteBehavior.Restrict,
            Assert.Single(guardian.GetForeignKeys()).DeleteBehavior);
    }

    private static Student CreateStudent(
        Guid? programId = null,
        DeliveryMode? deliveryMode = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ana",
            "Kovač",
            StudentStatus.Active,
            CreatedAtUtc,
            programId,
            deliveryMode);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
