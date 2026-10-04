using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Evidence;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialDetailQueryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OwnerReceivesCurrentCleanSnapshotWithExactMappings()
    {
        var teacherId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var fixture = AddActiveMaterial(db, teacherId);
        await db.SaveChangesAsync();

        var detail = await new EfMaterialDetailQuery(db).GetAsync(
            teacherId,
            fixture.Material.Id,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.True(detail.IsOwner);
        Assert.Null(detail.ShareAccess);
        Assert.Equal("Present Perfect – pravila", detail.Title);
        Assert.Equal("English 8", detail.Program?.Name);
        Assert.Equal("8R", detail.SchoolGrade?.Code);
        Assert.Equal("B1", detail.ProficiencyLevel?.Code);
        Assert.Equal("present perfect", Assert.Single(detail.Tags));
        var component = Assert.Single(detail.KnowledgeComponents);
        Assert.Equal("Present Perfect", component.Name);
        Assert.Equal("Grammar", component.KnowledgeAreaName);
        Assert.Equal("PLUS5-EN", component.KnowledgeModelCode);
        var outcome = Assert.Single(detail.CurriculumOutcomes);
        Assert.Equal("ENG.8.1", outcome.OfficialCode);
        Assert.Equal("Nacionalni kurikulum", outcome.CurriculumName);
        Assert.Equal("present-perfect.pdf", detail.File.OriginalFileName);
        var task = Assert.Single(detail.Tasks);
        Assert.Equal("I ____ London twice.", task.Prompt);
        Assert.Equal(MaterialDetailEvidenceType.Recognition, task.EvidenceType);
        Assert.Equal(1, task.Difficulty);
        Assert.Equal("B – have visited", task.CorrectAnswer);
        Assert.Equal("Present Perfect", Assert.Single(task.KnowledgeComponents).Name);
    }

    [Fact]
    public async Task SharedDetailRequiresSharedVisibilityAndExplicitGrant()
    {
        var ownerId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var fixture = AddActiveMaterial(db, ownerId);
        fixture.Material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddHours(1));
        db.MaterialShares.Add(new MaterialShare(
            fixture.Material,
            recipientId,
            MaterialShareAccess.View,
            CreatedAt.AddHours(1)));
        await db.SaveChangesAsync();

        var detail = await new EfMaterialDetailQuery(db).GetAsync(
            recipientId,
            fixture.Material.Id,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.False(detail.IsOwner);
        Assert.Equal(MaterialLibraryShareAccess.View, detail.ShareAccess);
    }

    [Fact]
    public async Task MissingForeignPrivateAndArchivedMaterialsAreIndistinguishable()
    {
        var ownerId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var active = AddActiveMaterial(db, ownerId);
        var archived = AddActiveMaterial(db, ownerId, "Archived worksheet");
        archived.Material.Archive(CreatedAt.AddHours(2));
        await db.SaveChangesAsync();
        var query = new EfMaterialDetailQuery(db);

        Assert.Null(await query.GetAsync(foreignId, active.Material.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(ownerId, archived.Material.Id, CancellationToken.None));
        Assert.Null(await query.GetAsync(ownerId, Guid.NewGuid(), CancellationToken.None));
    }

    private static MaterialFixture AddActiveMaterial(
        Plus5DbContext db,
        Guid teacherId,
        string title = "Present Perfect – pravila")
    {
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "English 8", CreatedAt);
        var grade = new SchoolGrade(Guid.NewGuid(), "8R", "8. razred", 8);
        var level = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", 3);
        var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5-EN", "2026");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 1);
        var component = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect", 1);
        model.Publish();
        var curriculum = new Curriculum(
            Guid.NewGuid(),
            "ENG-8",
            "Nacionalni kurikulum",
            "2026");
        var outcome = new CurriculumOutcome(
            Guid.NewGuid(),
            curriculum,
            "Primjenjuje Present Perfect u kontekstu.",
            1,
            officialCode: "ENG.8.1",
            sourceAuthority: "MZOM",
            sourceReference: "https://example.test/eng-8");
        var material = new Material(Guid.NewGuid(), teacherId, CreatedAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            title,
            "WORKSHEET",
            CreatedAt,
            "Pravila i primjeri za Present Perfect.",
            "Engleski jezik",
            "hr-HR",
            program,
            grade,
            level,
            "Učenik razlikuje i pravilno primjenjuje Present Perfect.");
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "present-perfect.pdf",
            "application/pdf",
            2048,
            "R2",
            "quarantine",
            CreatedAt);
        db.AddRange(program, grade, level, model, area, component, curriculum, outcome, material, version, file);
        db.MaterialVersionTags.Add(new MaterialVersionTag(version, "present perfect"));
        db.MaterialVersionKnowledgeComponents.Add(
            new MaterialVersionKnowledgeComponent(version, component, model));
        db.MaterialVersionCurriculumOutcomes.Add(
            new MaterialVersionCurriculumOutcome(version, outcome));
        var task = new AssessableTask(Guid.NewGuid(), material, CreatedAt);
        var taskVersion = new AssessableTaskVersion(
            Guid.NewGuid(),
            task,
            version,
            1,
            0,
            "I ____ London twice.",
            "SINGLE_CHOICE",
            1,
            EvidenceType.Recognition,
            1,
            CreatedAt,
            "B – have visited");
        db.AddRange(task, taskVersion);
        db.AssessableTaskVersionKnowledgeComponents.Add(
            new AssessableTaskVersionKnowledgeComponent(
                taskVersion,
                version,
                component,
                model,
                isLeaf: true));
        file.MarkUploaded(2048, new string('A', 64), CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        version.Activate(file, CreatedAt.AddMinutes(4));
        material.SetCurrentVersion(version, CreatedAt.AddMinutes(4));
        return new MaterialFixture(material);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }

    private sealed record MaterialFixture(Material Material);
}
