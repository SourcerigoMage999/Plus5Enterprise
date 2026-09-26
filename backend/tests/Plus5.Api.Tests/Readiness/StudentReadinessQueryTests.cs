using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Readiness;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Readiness;

namespace Plus5.Api.Tests.Readiness;

public sealed class StudentReadinessQueryTests
{
    private static readonly DateTimeOffset CalculatedAt =
        new(2026, 9, 26, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnsOwnedStudentAreaProjectionsWithTransparentMetadata()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var student = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Ana", "Anić",
            StudentStatus.Active, CalculatedAt.AddDays(-10));
        var model = new KnowledgeModel(Guid.NewGuid(), "english-7", "2026.1");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 1);
        model.Publish();
        var calculation = new MasteryCalculation(
            0.72m,
            ReadinessConfidence.Medium,
            ReadinessStatus.Developing,
            2,
            2.5m,
            new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        var estimate = new KnowledgeAreaReadinessEstimate(
            student.Id,
            area.Id,
            calculation,
            CalculatedAt,
            MasteryReadinessCalculator.AlgorithmVersion);

        await using var db = CreateDbContext();
        db.AddRange(grade, student, model, area, estimate);
        await db.SaveChangesAsync();

        var result = await new EfStudentReadinessQuery(db)
            .GetAsync(teacherId, student.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Ana", result.FirstName);
        Assert.Equal("7R", result.SchoolGradeCode);
        var readinessArea = Assert.Single(result.Areas);
        Assert.Equal("Grammar", readinessArea.Name);
        Assert.Equal("ENGLISH-7", readinessArea.KnowledgeModelCode);
        Assert.Equal(0.72m, readinessArea.Score);
        Assert.Equal("Medium", readinessArea.Confidence);
        Assert.Equal("Developing", readinessArea.Readiness);
        Assert.Equal(2, readinessArea.EvidenceCount);
        Assert.Equal("readiness-v1", readinessArea.AlgorithmVersion);
    }

    [Fact]
    public async Task ForeignArchivedAndMissingStudentsShareNotFoundResult()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "8R", "Osmi razred", 8);
        var foreign = new Student(
            Guid.NewGuid(), Guid.NewGuid(), grade.Id, "Tuđa", "Učenica",
            StudentStatus.Active, CalculatedAt.AddDays(-2));
        var archived = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Arhivirana", "Učenica",
            StudentStatus.Active, CalculatedAt.AddDays(-2));
        archived.Archive(CalculatedAt.AddDays(-1));

        await using var db = CreateDbContext();
        db.AddRange(grade, foreign, archived);
        await db.SaveChangesAsync();
        var query = new EfStudentReadinessQuery(db);

        Assert.Null(await query.GetAsync(teacherId, Guid.NewGuid(), CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, foreign.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, archived.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OwnedStudentWithoutProjectionReturnsExplicitEmptySnapshot()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "5R", "Peti razred", 5);
        var student = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Novi", "Učenik",
            StudentStatus.Active, CalculatedAt.AddDays(-1));

        await using var db = CreateDbContext();
        db.AddRange(grade, student);
        await db.SaveChangesAsync();

        var result = await new EfStudentReadinessQuery(db)
            .GetAsync(teacherId, student.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Areas);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
