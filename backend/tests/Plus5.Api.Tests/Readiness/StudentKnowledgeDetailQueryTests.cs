using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Readiness;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Readiness;

namespace Plus5.Api.Tests.Readiness;

public sealed class StudentKnowledgeDetailQueryTests
{
    private static readonly DateTimeOffset CalculatedAt =
        new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnsComponentHierarchyAndKeepsModelVersionsSeparate()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        var student = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Ana", "Anić",
            StudentStatus.Active, CalculatedAt.AddDays(-10), schoolName: "OŠ Plus 5");
        var currentModel = new KnowledgeModel(Guid.NewGuid(), "english-7", "2026.1");
        var currentArea = new KnowledgeArea(Guid.NewGuid(), currentModel, "Grammar", 1);
        var root = new KnowledgeComponent(
            Guid.NewGuid(), currentModel, currentArea, "Verb forms", 1);
        var leaf = new KnowledgeComponent(
            Guid.NewGuid(), currentModel, currentArea, "Past Simple", 2, root);
        currentModel.Publish();

        var oldModel = new KnowledgeModel(Guid.NewGuid(), "english-7", "2025.1");
        var oldArea = new KnowledgeArea(Guid.NewGuid(), oldModel, "Grammar", 1);
        var oldLeaf = new KnowledgeComponent(
            Guid.NewGuid(), oldModel, oldArea, "Past Simple", 1);
        oldModel.Publish();
        oldModel.Retire();

        var rootCalculation = Calculation(0.74m, 3);
        var leafCalculation = Calculation(0.81m, 2);
        var oldCalculation = Calculation(0.62m, 1);

        await using var db = CreateDbContext();
        db.AddRange(
            grade,
            student,
            currentModel,
            currentArea,
            root,
            leaf,
            oldModel,
            oldArea,
            oldLeaf,
            new MasteryEstimate(student.Id, root.Id, rootCalculation, CalculatedAt, "readiness-v1"),
            new MasteryEstimate(student.Id, leaf.Id, leafCalculation, CalculatedAt, "readiness-v1"),
            new MasteryEstimate(student.Id, oldLeaf.Id, oldCalculation, CalculatedAt, "readiness-v1"),
            new KnowledgeAreaReadinessEstimate(student.Id, currentArea.Id, rootCalculation, CalculatedAt, "readiness-v1"),
            new KnowledgeAreaReadinessEstimate(student.Id, oldArea.Id, oldCalculation, CalculatedAt, "readiness-v1"));
        await db.SaveChangesAsync();

        var result = await new EfStudentKnowledgeDetailQuery(db)
            .GetAsync(teacherId, student.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("OŠ Plus 5", result.SchoolName);
        Assert.Equal(2, result.Models.Count);
        var current = Assert.Single(result.Models, model => model.Version == "2026.1");
        Assert.Equal("Published", current.Status);
        var currentAreaResult = Assert.Single(current.Areas);
        Assert.Equal(0.74m, currentAreaResult.Score);
        Assert.Collection(
            currentAreaResult.Components,
            component =>
            {
                Assert.Equal("Verb forms", component.Name);
                Assert.Null(component.ParentKnowledgeComponentId);
            },
            component =>
            {
                Assert.Equal("Past Simple", component.Name);
                Assert.Equal(root.Id, component.ParentKnowledgeComponentId);
                Assert.Equal(0.81m, component.Score);
            });
        Assert.Equal("Retired", Assert.Single(result.Models, model => model.Version == "2025.1").Status);
    }

    [Fact]
    public async Task OwnedStudentWithoutMasteryReturnsExplicitEmptySnapshot()
    {
        var teacherId = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "5R", "Peti razred", 5);
        var student = new Student(
            Guid.NewGuid(), teacherId, grade.Id, "Novi", "Učenik",
            StudentStatus.Active, CalculatedAt.AddDays(-1));

        await using var db = CreateDbContext();
        db.AddRange(grade, student);
        await db.SaveChangesAsync();

        var result = await new EfStudentKnowledgeDetailQuery(db)
            .GetAsync(teacherId, student.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Models);
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
        var query = new EfStudentKnowledgeDetailQuery(db);

        Assert.Null(await query.GetAsync(teacherId, Guid.NewGuid(), CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, foreign.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(teacherId, archived.Id, CancellationToken.None));
    }

    private static MasteryCalculation Calculation(decimal score, int evidenceCount)
    {
        return new MasteryCalculation(
            score,
            ReadinessConfidence.Medium,
            ReadinessStatus.Developing,
            evidenceCount,
            evidenceCount,
            Enumerable.Range(0, evidenceCount).Select(_ => Guid.NewGuid()).ToHashSet());
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
