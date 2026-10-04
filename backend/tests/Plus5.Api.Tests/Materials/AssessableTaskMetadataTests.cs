using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Evidence;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class AssessableTaskMetadataTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 4, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TaskVersionCapturesVersionBoundEvidenceMetadata()
    {
        var material = CreateMaterial();
        var materialVersion = CreateMaterialVersion(material);
        var task = new AssessableTask(Guid.NewGuid(), material, CreatedAt);

        var version = new AssessableTaskVersion(
            Guid.NewGuid(),
            task,
            materialVersion,
            1,
            0,
            "  I ____ London twice.  ",
            "single_choice",
            2,
            EvidenceType.Application,
            1.5m,
            CreatedAt,
            " have visited ");

        Assert.Equal(material.Id, version.MaterialId);
        Assert.Equal(materialVersion.Id, version.MaterialVersionId);
        Assert.Equal("I ____ London twice.", version.Prompt);
        Assert.Equal("SINGLE_CHOICE", version.TaskTypeCode);
        Assert.Equal("have visited", version.CorrectAnswer);
        Assert.Equal(2, version.Difficulty);
        Assert.Equal(EvidenceType.Application, version.EvidenceType);
        Assert.Equal(1.5m, version.MaxPoints);
    }

    [Fact]
    public void TaskVersionRejectsInvalidEvidenceShapeOrCrossMaterialReference()
    {
        var material = CreateMaterial();
        var version = CreateMaterialVersion(material);
        var task = new AssessableTask(Guid.NewGuid(), material, CreatedAt);

        Assert.Throws<ArgumentException>(() => new AssessableTaskVersion(
            Guid.NewGuid(), task, version, 1, 0, "Prompt", "QUESTION", 1,
            EvidenceType.Recognition, 1, CreatedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AssessableTaskVersion(
            Guid.NewGuid(), task, version, 1, 0, "Prompt", "QUESTION", 0,
            EvidenceType.Recognition, 1, CreatedAt, correctAnswer: "Answer"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AssessableTaskVersion(
            Guid.NewGuid(), task, version, 1, 0, "Prompt", "QUESTION", 1,
            EvidenceType.Recognition, 0, CreatedAt, correctAnswer: "Answer"));

        var otherMaterial = CreateMaterial();
        var otherVersion = CreateMaterialVersion(otherMaterial);
        Assert.Throws<ArgumentException>(() => new AssessableTaskVersion(
            Guid.NewGuid(), task, otherVersion, 1, 0, "Prompt", "QUESTION", 1,
            EvidenceType.Recognition, 1, CreatedAt, correctAnswer: "Answer"));
    }

    [Fact]
    public void TaskKnowledgeMappingRequiresPublishedOrRetiredLeafComponent()
    {
        var material = CreateMaterial();
        var materialVersion = CreateMaterialVersion(material);
        var task = new AssessableTask(Guid.NewGuid(), material, CreatedAt);
        var taskVersion = CreateTaskVersion(task, materialVersion);
        var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5-EN", "2026");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
        var component = new KnowledgeComponent(
            Guid.NewGuid(), model, area, "Present Perfect", 0);

        Assert.Throws<InvalidOperationException>(() =>
            new AssessableTaskVersionKnowledgeComponent(
                taskVersion, materialVersion, component, model, isLeaf: true));

        model.Publish();
        Assert.Throws<ArgumentException>(() =>
            new AssessableTaskVersionKnowledgeComponent(
                taskVersion, materialVersion, component, model, isLeaf: false));

        var mapping = new AssessableTaskVersionKnowledgeComponent(
            taskVersion, materialVersion, component, model, isLeaf: true);
        Assert.Equal(taskVersion.Id, mapping.AssessableTaskVersionId);
        Assert.Equal(component.Id, mapping.KnowledgeComponentId);
    }

    [Fact]
    public void EfModelUsesVersionBoundRestrictiveTaskReferences()
    {
        using var db = CreateDbContext();
        var taskVersion = db.Model.FindEntityType(typeof(AssessableTaskVersion))!;
        var mapping = db.Model.FindEntityType(
            typeof(AssessableTaskVersionKnowledgeComponent))!;

        Assert.Contains(taskVersion.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(AssessableTaskVersion.AssessableTaskId),
                nameof(AssessableTaskVersion.VersionNumber),
            ]));
        Assert.All(taskVersion.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Equal(
            [
                nameof(AssessableTaskVersionKnowledgeComponent.AssessableTaskVersionId),
                nameof(AssessableTaskVersionKnowledgeComponent.KnowledgeComponentId),
            ],
            mapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(mapping.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static Material CreateMaterial() =>
        new(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);

    private static MaterialVersion CreateMaterialVersion(Material material) =>
        new(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect",
            "WORKSHEET",
            CreatedAt);

    private static AssessableTaskVersion CreateTaskVersion(
        AssessableTask task,
        MaterialVersion materialVersion) =>
        new(
            Guid.NewGuid(),
            task,
            materialVersion,
            1,
            0,
            "I ____ London twice.",
            "SINGLE_CHOICE",
            1,
            EvidenceType.Recognition,
            1,
            CreatedAt,
            correctAnswer: "have visited");

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
