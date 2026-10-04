using Plus5.Api.Materials;
using Plus5.Application.Materials;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialDetailEndpointContractTests
{
    [Fact]
    public void ApiResponseCarriesAssessableTaskSnapshot()
    {
        var component = new MaterialDetailKnowledgeComponent(
            Guid.NewGuid(),
            "Present Perfect",
            "Grammar",
            "EN-8",
            "2026",
            MaterialDetailKnowledgeModelStatus.Published);
        var taskId = Guid.NewGuid();
        var taskVersionId = Guid.NewGuid();
        var material = new MaterialDetail(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Present Perfect – zadaci",
            null,
            "WORKSHEET",
            "Engleski jezik",
            "hr",
            null,
            null,
            null,
            null,
            new MaterialDetailFile(MaterialLibraryFileFormat.Pdf, "tasks.pdf", "application/pdf", 1024),
            DateTimeOffset.UtcNow,
            true,
            null,
            [],
            [],
            [],
            [new MaterialDetailTask(
                taskId,
                taskVersionId,
                2,
                1,
                "Dopuni rečenicu.",
                "FILL_GAP",
                3,
                MaterialDetailEvidenceType.Understanding,
                "finished",
                null,
                2.5m,
                [component])]);

        var response = MaterialLibraryEndpoints.MapDetail(material);

        var task = Assert.Single(response.Tasks);
        Assert.Equal(taskId, task.Id);
        Assert.Equal(taskVersionId, task.VersionId);
        Assert.Equal("understanding", task.EvidenceType);
        Assert.Equal(2.5m, task.MaxPoints);
        var mappedComponent = Assert.Single(task.KnowledgeComponents);
        Assert.Equal(component.Id, mappedComponent.Id);
        Assert.Equal("published", mappedComponent.KnowledgeModelStatus);
    }
}
