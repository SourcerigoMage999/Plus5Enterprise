using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonStructureTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(60, 5, 10, 15, 20, 10)]
    [InlineData(45, 4, 8, 11, 14, 8)]
    [InlineData(5, 1, 1, 1, 1, 1)]
    public void AllocationIsDeterministicAndPreservesExactTarget(int target, params int[] expected)
    {
        var result = LessonStructurePolicy.AllocateDurations(target);
        Assert.Equal(expected, result);
        Assert.Equal(target, result.Sum());
    }

    [Fact]
    public async Task PreviewIsNoCostAndApplyPersistsExactBaselineAtomically()
    {
        await using var db = CreateDbContext();
        var teacher = new UserAccount(Guid.NewGuid(), "owner@plus5.local", "OWNER@PLUS5.LOCAL", Now);
        var grade = new SchoolGrade(Guid.NewGuid(), "G8", "8. razred", 8);
        var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "Grammar", Now);
        var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
            StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
        var model = new KnowledgeModel(Guid.NewGuid(), "ENG-G8", "2026");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
        var component = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect", 0);
        model.Publish();
        var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
        var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Razlikovati vremena.", Now);
        db.AddRange(teacher, grade, program, student, model, area, component, plan, version,
            new LessonPlanVersionKnowledgeComponent(version.Id, component.Id));
        foreach (var block in CreatePublishedBlocks()) db.AddRange(block.Root, block.Version);
        await db.SaveChangesAsync();
        db.Entry(version).Property(item => item.RowVersion).CurrentValue = new byte[8];
        await db.SaveChangesAsync();

        var service = new EfLessonStructureService(db, new FixedTimeProvider());
        var preview = await service.GetAsync(teacher.Id, plan.Id, CancellationToken.None);
        Assert.Equal(LessonStructureFailure.None, preview.Failure);
        Assert.False(preview.View!.IsApplied);
        Assert.Equal("SYSTEM_BASELINE_V1", preview.View.PolicyCode);
        Assert.Equal([5, 10, 15, 20, 10], preview.View.Activities.Select(item => item.DurationMinutes));
        Assert.Empty(db.LessonActivities);
        var foreign = await service.GetAsync(Guid.NewGuid(), plan.Id, CancellationToken.None);
        Assert.Equal(LessonStructureFailure.NotFound, foreign.Failure);

        var applied = await service.ApplyBaselineAsync(teacher.Id, plan.Id,
            Convert.ToBase64String(new byte[8]), CancellationToken.None);
        Assert.Equal(LessonStructureFailure.None, applied.Failure);
        Assert.True(applied.View!.IsApplied);
        Assert.Equal(5, await db.LessonActivities.CountAsync());
        Assert.Equal(4, await db.LessonActivityKnowledgeComponents.CountAsync());
        Assert.Null((await db.LessonActivities.OrderBy(item => item.SortOrder).LastAsync()).KnowledgeBlockVersionId);

        var second = await service.ApplyBaselineAsync(teacher.Id, plan.Id,
            Convert.ToBase64String(new byte[8]), CancellationToken.None);
        Assert.Equal(LessonStructureFailure.AlreadyApplied, second.Failure);
    }

    private static IEnumerable<(KnowledgeBlock Root, KnowledgeBlockVersion Version)> CreatePublishedBlocks()
    {
        var definitions = new[] { ("KB-001", "Small Talk", 3, 5, 10), ("KB-002", "Aktivacija predznanja", 5, 10, 15),
            ("KB-004", "Objašnjenje novog gradiva", 8, 15, 25), ("KB-005", "Interaktivni zadaci", 10, 20, 30) };
        foreach (var item in definitions)
        {
            var root = new KnowledgeBlock(Guid.NewGuid(), item.Item1, Now);
            var version = new KnowledgeBlockVersion(Guid.NewGuid(), root.Id, 1, item.Item2, Now);
            version.Publish(item.Item3, item.Item4, item.Item5, Now);
            root.SetCurrentPublishedVersion(version);
            yield return (root, version);
        }
    }

    private static Plus5DbContext CreateDbContext() => new(new DbContextOptionsBuilder<Plus5DbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
}
