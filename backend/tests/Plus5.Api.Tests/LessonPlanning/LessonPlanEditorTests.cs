using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonPlanEditorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CandidatesAreCurrentCleanOwnerOrSharedUseAndExactVersionsCanBeAttachedAndDetached()
    {
        await using var db = CreateDbContext();
        var fixture = await CreatePlanAsync(db);
        var ownerMaterial = AddActiveMaterial(db, fixture.Teacher.Id, "Present Perfect worksheet",
            fixture.Component, fixture.Model);

        var otherTeacher = new UserAccount(Guid.NewGuid(), "other@plus5.local", "OTHER@PLUS5.LOCAL", Now);
        var sharedMaterial = AddActiveMaterial(db, otherTeacher.Id, "Shared grammar cards");
        sharedMaterial.Material.SetVisibility(MaterialVisibility.Shared, Now.AddMinutes(5));
        db.Add(otherTeacher);
        db.MaterialShares.Add(new MaterialShare(sharedMaterial.Material, fixture.Teacher.Id,
            MaterialShareAccess.Use, Now.AddMinutes(5)));
        _ = AddActiveMaterial(db, otherTeacher.Id, "Private foreign material");
        await db.SaveChangesAsync();

        var structure = new EfLessonStructureService(db, new FixedTimeProvider());
        var applied = await structure.ApplyBaselineAsync(fixture.Teacher.Id, fixture.Plan.Id,
            Convert.ToBase64String(new byte[8]), CancellationToken.None);
        var activity = applied.View!.Activities.First(item => item.KnowledgeComponentIds.Count > 0);
        var service = new EfLessonPlanEditorService(db, structure, new FixedTimeProvider());

        var candidates = await service.GetMaterialCandidatesAsync(fixture.Teacher.Id, fixture.Plan.Id,
            activity.Id, null, 1, 20, CancellationToken.None);

        Assert.Equal(LessonPlanEditorFailure.None, candidates.Failure);
        Assert.Equal(2, candidates.Page!.TotalCount);
        Assert.Equal(ownerMaterial.Version.Id, candidates.Page.Items[0].MaterialVersionId);
        Assert.Equal(1, candidates.Page.Items[0].MatchingKnowledgeComponentCount);
        Assert.Contains(candidates.Page.Items, item => item.MaterialVersionId == sharedMaterial.Version.Id && !item.IsOwner);

        var attached = await service.AttachMaterialAsync(fixture.Teacher.Id, fixture.Plan.Id,
            activity.Id, ownerMaterial.Version.Id, applied.View.RowVersion, CancellationToken.None);
        Assert.Equal(LessonPlanEditorFailure.None, attached.Failure);
        var attachment = Assert.Single(attached.View!.Activities.Single(item => item.Id == activity.Id).Materials);
        Assert.Equal(ownerMaterial.Version.Id, attachment.MaterialVersionId);
        Assert.Equal(1, attachment.MatchingKnowledgeComponentCount);
        Assert.Contains(attached.View.Balance, item => item.Code == "MaterialRelevance"
            && item.Status == LessonBalanceStatus.Satisfied);

        var removed = await service.DetachMaterialAsync(fixture.Teacher.Id, fixture.Plan.Id,
            activity.Id, ownerMaterial.Version.Id, attached.View.RowVersion, CancellationToken.None);
        Assert.Equal(LessonPlanEditorFailure.None, removed.Failure);
        Assert.Empty(removed.View!.AttachedMaterials);
    }

    [Fact]
    public async Task ForeignPlanAndNonCurrentMaterialVersionAreNotExposed()
    {
        await using var db = CreateDbContext();
        var fixture = await CreatePlanAsync(db);
        var material = AddActiveMaterial(db, fixture.Teacher.Id, "Versioned worksheet");
        await db.SaveChangesAsync();
        var structure = new EfLessonStructureService(db, new FixedTimeProvider());
        var applied = await structure.ApplyBaselineAsync(fixture.Teacher.Id, fixture.Plan.Id,
            Convert.ToBase64String(new byte[8]), CancellationToken.None);
        var activity = applied.View!.Activities[0];

        var replacement = new MaterialVersion(Guid.NewGuid(), material.Material, 2,
            "Versioned worksheet v2", "WORKSHEET", Now.AddMinutes(10));
        var file = AddCleanFile(db, material.Material, replacement, Now.AddMinutes(10));
        db.Add(replacement);
        replacement.Activate(file, Now.AddMinutes(14));
        material.Version.Supersede(Now.AddMinutes(14));
        material.Material.SetCurrentVersion(replacement, Now.AddMinutes(14));
        await db.SaveChangesAsync();

        var service = new EfLessonPlanEditorService(db, structure, new FixedTimeProvider());
        var foreign = await service.GetAsync(Guid.NewGuid(), fixture.Plan.Id, CancellationToken.None);
        Assert.Equal(LessonPlanEditorFailure.NotFound, foreign.Failure);

        var oldVersion = await service.AttachMaterialAsync(fixture.Teacher.Id, fixture.Plan.Id,
            activity.Id, material.Version.Id, applied.View.RowVersion, CancellationToken.None);
        Assert.Equal(LessonPlanEditorFailure.MaterialUnavailable, oldVersion.Failure);

        var page = await service.GetMaterialCandidatesAsync(fixture.Teacher.Id, fixture.Plan.Id,
            activity.Id, null, 1, 20, CancellationToken.None);
        Assert.DoesNotContain(page.Page!.Items, item => item.MaterialVersionId == material.Version.Id);
        Assert.Contains(page.Page.Items, item => item.MaterialVersionId == replacement.Id);
    }

    private static async Task<PlanFixture> CreatePlanAsync(Plus5DbContext db)
    {
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
        return new(teacher, plan, model, component);
    }

    private static MaterialFixture AddActiveMaterial(Plus5DbContext db, Guid teacherId, string title,
        KnowledgeComponent? component = null, KnowledgeModel? model = null)
    {
        var material = new Material(Guid.NewGuid(), teacherId, Now);
        var version = new MaterialVersion(Guid.NewGuid(), material, 1, title, "WORKSHEET", Now);
        var file = AddCleanFile(db, material, version, Now);
        if (component is not null && model is not null)
            db.MaterialVersionKnowledgeComponents.Add(new(version, component, model));
        version.Activate(file, Now.AddMinutes(4));
        material.SetCurrentVersion(version, Now.AddMinutes(4));
        db.AddRange(material, version);
        return new(material, version);
    }

    private static MaterialFile AddCleanFile(Plus5DbContext db, Material material,
        MaterialVersion version, DateTimeOffset createdAt)
    {
        var file = new MaterialFile(Guid.NewGuid(), material, version, MaterialFileFormat.Pdf,
            "material.pdf", "application/pdf", 100, "R2", "quarantine", createdAt);
        file.MarkUploaded(100, new string('A', 64), createdAt.AddMinutes(1));
        file.StartScanning(createdAt.AddMinutes(2));
        file.MarkClean(createdAt.AddMinutes(3));
        db.Add(file);
        return file;
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
    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => Now.AddMinutes(20); }
    private sealed record PlanFixture(UserAccount Teacher, LessonPlan Plan, KnowledgeModel Model, KnowledgeComponent Component);
    private sealed record MaterialFixture(Material Material, MaterialVersion Version);
}
