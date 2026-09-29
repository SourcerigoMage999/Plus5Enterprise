using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialMetadataMappingTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 29, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DraftVersionMapsToExactOutcomeComponentAndProficiencyLevel()
    {
        var level = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", 3);
        var material = CreateMaterial();
        var program = new Plus5.Domain.Teaching.Program(
            Guid.NewGuid(), material.OwnerTeacherId, "Grammar Focus", CreatedAt);
        var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
        var version = CreateVersion(
            material,
            program,
            grade,
            level,
            "Use Present Perfect to describe experiences.");
        var curriculum = new Curriculum(Guid.NewGuid(), "HR-EJ", "English", "2026");
        var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);
        var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5_CORE", "V1");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
        var component = new KnowledgeComponent(
            Guid.NewGuid(),
            model,
            area,
            "Present Perfect",
            0);
        model.Publish();

        var outcomeMapping = new MaterialVersionCurriculumOutcome(version, outcome);
        var componentMapping = new MaterialVersionKnowledgeComponent(
            version,
            component,
            model);
        var tag = new MaterialVersionTag(version, " present perfect ");

        Assert.Equal(program.Id, version.ProgramId);
        Assert.Equal(grade.Id, version.SchoolGradeId);
        Assert.Equal(level.Id, version.ProficiencyLevelId);
        Assert.Equal(
            "Use Present Perfect to describe experiences.",
            version.LearningGoal);
        Assert.Equal(version.Id, outcomeMapping.MaterialVersionId);
        Assert.Equal(outcome.Id, outcomeMapping.CurriculumOutcomeId);
        Assert.Equal(version.Id, componentMapping.MaterialVersionId);
        Assert.Equal(component.Id, componentMapping.KnowledgeComponentId);
        Assert.Equal("present perfect", tag.Name);
        Assert.Equal("PRESENT PERFECT", tag.NormalizedName);
    }

    [Fact]
    public void ComponentMappingRejectsDraftOrUnrelatedKnowledgeModel()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var draftModel = new KnowledgeModel(Guid.NewGuid(), "PLUS5_CORE", "V1");
        var area = new KnowledgeArea(Guid.NewGuid(), draftModel, "Grammar", 0);
        var component = new KnowledgeComponent(
            Guid.NewGuid(),
            draftModel,
            area,
            "Present Perfect",
            0);

        Assert.Throws<InvalidOperationException>(() =>
            new MaterialVersionKnowledgeComponent(version, component, draftModel));

        draftModel.Publish();
        var unrelated = new KnowledgeModel(Guid.NewGuid(), "OTHER", "V1");
        unrelated.Publish();
        Assert.Throws<ArgumentException>(() =>
            new MaterialVersionKnowledgeComponent(version, component, unrelated));
    }

    [Fact]
    public void MetadataRejectsProgramOwnedByAnotherTeacher()
    {
        var material = CreateMaterial();
        var foreignProgram = new Plus5.Domain.Teaching.Program(
            Guid.NewGuid(), Guid.NewGuid(), "Foreign", CreatedAt);

        Assert.Throws<ArgumentException>(() => new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Worksheet",
            "WORKSHEET",
            CreatedAt,
            program: foreignProgram));
    }

    [Fact]
    public void PublishedVersionRejectsNewMetadataMappings()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var file = CreateCleanFile(material, version);
        version.Activate(file, CreatedAt.AddMinutes(4));
        var curriculum = new Curriculum(Guid.NewGuid(), "HR-EJ", "English", "2026");
        var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);
        var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5_CORE", "V1");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
        var component = new KnowledgeComponent(
            Guid.NewGuid(),
            model,
            area,
            "Present Perfect",
            0);
        model.Publish();

        Assert.Throws<InvalidOperationException>(() =>
            new MaterialVersionCurriculumOutcome(version, outcome));
        Assert.Throws<InvalidOperationException>(() =>
            new MaterialVersionKnowledgeComponent(version, component, model));
    }

    [Fact]
    public void RestoredDraftPreservesExactProficiencyLevelReference()
    {
        var material = CreateMaterial();
        var program = new Plus5.Domain.Teaching.Program(
            Guid.NewGuid(), material.OwnerTeacherId, "Grammar Focus", CreatedAt);
        var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
        var level = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", 3);
        var version = CreateVersion(
            material,
            program,
            grade,
            level,
            "Use Present Perfect.");

        var restored = version.CreateRestoredDraft(
            Guid.NewGuid(),
            material,
            2,
            CreatedAt.AddDays(1));

        Assert.Equal(program.Id, restored.ProgramId);
        Assert.Equal(grade.Id, restored.SchoolGradeId);
        Assert.Equal(level.Id, restored.ProficiencyLevelId);
        Assert.Equal("Use Present Perfect.", restored.LearningGoal);
        Assert.Equal(MaterialVersionStatus.Draft, restored.Status);
    }

    [Fact]
    public void EfModelUsesVersionBoundCompositeMappingsAndRestrictiveReferences()
    {
        using var db = CreateDbContext();
        var version = db.Model.FindEntityType(typeof(MaterialVersion))!;
        var outcomeMapping =
            db.Model.FindEntityType(typeof(MaterialVersionCurriculumOutcome))!;
        var componentMapping =
            db.Model.FindEntityType(typeof(MaterialVersionKnowledgeComponent))!;
        var tag = db.Model.FindEntityType(typeof(MaterialVersionTag))!;

        Assert.Contains(version.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(MaterialVersion.CreatedByTeacherId),
                nameof(MaterialVersion.ProgramId),
            ])
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(version.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(MaterialVersion.SchoolGradeId)
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(version.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(MaterialVersion.ProficiencyLevelId)
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal(
            [
                nameof(MaterialVersionCurriculumOutcome.MaterialVersionId),
                nameof(MaterialVersionCurriculumOutcome.CurriculumOutcomeId),
            ],
            outcomeMapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [
                nameof(MaterialVersionKnowledgeComponent.MaterialVersionId),
                nameof(MaterialVersionKnowledgeComponent.KnowledgeComponentId),
            ],
            componentMapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(outcomeMapping.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.All(componentMapping.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Equal(
            [
                nameof(MaterialVersionTag.MaterialVersionId),
                nameof(MaterialVersionTag.NormalizedName),
            ],
            tag.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(tag.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static Material CreateMaterial() =>
        new(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);

    private static MaterialVersion CreateVersion(
        Material material,
        Plus5.Domain.Teaching.Program? program = null,
        SchoolGrade? grade = null,
        ProficiencyLevel? level = null,
        string? learningGoal = null) =>
        new(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect worksheet",
            "WORKSHEET",
            CreatedAt,
            "Practice material",
            "English",
            "en",
            program,
            grade,
            level,
            learningGoal);

    private static MaterialFile CreateCleanFile(Material material, MaterialVersion version)
    {
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "worksheet.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt);
        file.MarkUploaded(100, new string('A', 64), CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        return file;
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
