using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Teaching;

public sealed class KnowledgeComponentModelTests
{
    [Fact]
    public void ModelLifecycleUsesVersionBoundaryAndOneWayTransitions()
    {
        var model = CreateModel("plus5_core", "v1");

        Assert.Equal("PLUS5_CORE", model.Code);
        Assert.Equal("V1", model.Version);
        Assert.Equal(KnowledgeModelStatus.Draft, model.Status);

        model.Publish();
        Assert.Equal(KnowledgeModelStatus.Published, model.Status);
        Assert.Throws<InvalidOperationException>(model.Publish);

        model.Retire();
        Assert.Equal(KnowledgeModelStatus.Retired, model.Status);
        Assert.Throws<InvalidOperationException>(model.Retire);
    }

    [Fact]
    public void AreaAndSingleParentComponentRemainSeparateAndScoped()
    {
        var model = CreateModel("PLUS5_CORE", "V1");
        var firstArea = CreateArea(model, "Area A", 0);
        var secondArea = CreateArea(model, "Area B", 1);
        var root = CreateComponent(model, firstArea, "Root", 0);
        var child = CreateComponent(model, firstArea, "Child", 0, root);

        Assert.Equal(firstArea.Id, root.KnowledgeAreaId);
        Assert.Null(root.ParentComponentId);
        Assert.Equal(root.Id, child.ParentComponentId);

        Assert.Throws<ArgumentException>(() =>
            CreateComponent(model, secondArea, "Cross-area child", 0, root));

        var otherModel = CreateModel("PLUS5_CORE", "V2");
        var otherArea = CreateArea(otherModel, "Area A", 0);
        Assert.Throws<ArgumentException>(() =>
            CreateComponent(otherModel, otherArea, "Cross-model child", 0, root));
    }

    [Fact]
    public void ComponentLineageStaysInModelFamilyAndCrossesVersions()
    {
        var previousModel = CreateModel("PLUS5_CORE", "V1");
        var currentModel = CreateModel("PLUS5_CORE", "V2");
        var sameVersionModel = CreateModel("PLUS5_CORE", "V2");
        var otherFamilyModel = CreateModel("OTHER", "V1");
        var previous = CreateComponent(
            previousModel,
            CreateArea(previousModel, "Area", 0),
            "Previous",
            0);
        var sameVersion = CreateComponent(
            sameVersionModel,
            CreateArea(sameVersionModel, "Area", 0),
            "Same version",
            0);
        var otherFamily = CreateComponent(
            otherFamilyModel,
            CreateArea(otherFamilyModel, "Area", 0),
            "Other family",
            0);
        var currentArea = CreateArea(currentModel, "Area", 0);

        var successor = CreateComponent(
            currentModel,
            currentArea,
            "Successor",
            0,
            supersedes: previous,
            supersededModel: previousModel);
        Assert.Equal(previous.Id, successor.SupersedesKnowledgeComponentId);

        Assert.Throws<ArgumentException>(() => CreateComponent(
            currentModel,
            currentArea,
            "Invalid same version",
            0,
            supersedes: sameVersion,
            supersededModel: sameVersionModel));
        Assert.Throws<ArgumentException>(() => CreateComponent(
            currentModel,
            currentArea,
            "Invalid family",
            0,
            supersedes: otherFamily,
            supersededModel: otherFamilyModel));
    }

    [Fact]
    public void PublishedModelRejectsStructuralMutation()
    {
        var model = CreateModel("PLUS5_CORE", "V1");
        var area = CreateArea(model, "Area", 0);
        var component = CreateComponent(model, area, "Component", 0);
        model.Publish();

        Assert.Throws<InvalidOperationException>(() =>
            area.UpdateDraft(model, "Changed", 1));
        Assert.Throws<InvalidOperationException>(() =>
            component.UpdateDraft(model, area, "Changed", 1));
        Assert.Throws<InvalidOperationException>(() =>
            CreateArea(model, "New area", 2));
        Assert.Throws<InvalidOperationException>(() =>
            CreateComponent(model, area, "New component", 2));
    }

    [Fact]
    public void MappingUsesOnlyExplicitCompositeIdentity()
    {
        var curriculum = new Curriculum(Guid.NewGuid(), "MAT-7", "Mathematics", "2026");
        var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);
        var model = CreateModel("PLUS5_CORE", "V1");
        var component = CreateComponent(
            model,
            CreateArea(model, "Area", 0),
            "Component",
            0);
        var mapping = new CurriculumOutcomeKnowledgeComponent(outcome, component);

        Assert.Equal(outcome.Id, mapping.CurriculumOutcomeId);
        Assert.Equal(component.Id, mapping.KnowledgeComponentId);
    }

    [Fact]
    public void EfModelProtectsTreeVersionAndMappingBoundaries()
    {
        using var db = CreateDbContext();
        var component = db.Model.FindEntityType(typeof(KnowledgeComponent))!;
        var mapping = db.Model.FindEntityType(typeof(CurriculumOutcomeKnowledgeComponent))!;

        Assert.Equal(4, component.GetForeignKeys().Count());
        Assert.All(component.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(component.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(KnowledgeComponent.KnowledgeModelId),
                nameof(KnowledgeComponent.KnowledgeAreaId),
                nameof(KnowledgeComponent.ParentComponentId),
            ]));
        Assert.DoesNotContain(component.GetProperties(), property =>
            property.Name is "CurriculumId" or "SchoolGradeId" or "ProficiencyLevelId"
                or "TeacherAccountId" or "EvidenceEligible");
        Assert.Equal(
            [
                nameof(CurriculumOutcomeKnowledgeComponent.CurriculumOutcomeId),
                nameof(CurriculumOutcomeKnowledgeComponent.KnowledgeComponentId),
            ],
            mapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(mapping.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static KnowledgeModel CreateModel(string code, string version) =>
        new(Guid.NewGuid(), code, version);

    private static KnowledgeArea CreateArea(
        KnowledgeModel model,
        string name,
        int sortOrder) =>
        new(Guid.NewGuid(), model, name, sortOrder);

    private static KnowledgeComponent CreateComponent(
        KnowledgeModel model,
        KnowledgeArea area,
        string name,
        int sortOrder,
        KnowledgeComponent? parent = null,
        KnowledgeComponent? supersedes = null,
        KnowledgeModel? supersededModel = null) =>
        new(
            Guid.NewGuid(),
            model,
            area,
            name,
            sortOrder,
            parent,
            supersedes,
            supersededModel);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
