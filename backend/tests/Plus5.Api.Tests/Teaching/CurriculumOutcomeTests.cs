using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Teaching;

public sealed class CurriculumOutcomeTests
{
    [Fact]
    public void OutcomePreservesPublishedCodeAndExplicitHierarchy()
    {
        var curriculum = CreateCurriculum("HR-EJ", "2026");
        var root = CreateOutcome(curriculum, "Komunikacijska jezična kompetencija", 0,
            officialCode: "OŠ (1) EJ A.7.1.");
        var child = CreateOutcome(curriculum, "Razumije srednje dug tekst", 2, root);

        Assert.Null(root.ParentOutcomeId);
        Assert.Equal(root.Id, child.ParentOutcomeId);
        Assert.Equal("OŠ (1) EJ A.7.1.", root.OfficialCode);
        Assert.Equal("Ministarstvo", root.SourceAuthority);
        Assert.Equal("Kurikulum/2026", root.SourceReference);
    }

    [Fact]
    public void ChildRejectsParentFromAnotherCurriculumVersion()
    {
        var parent = CreateOutcome(CreateCurriculum("HR-EJ", "2025"), "Root", 0);

        Assert.Throws<ArgumentException>(() =>
            CreateOutcome(CreateCurriculum("HR-EJ", "2026"), "Invalid child", 0, parent));
    }

    [Fact]
    public void OfficialCodeRequiresSourceProvenance()
    {
        Assert.Throws<ArgumentException>(() => new CurriculumOutcome(
            Guid.NewGuid(),
            CreateCurriculum("HR-EJ", "2026"),
            "Outcome",
            0,
            officialCode: "OFFICIAL-1"));
    }

    [Fact]
    public void SupersessionMustStayInCurriculumFamilyAndCrossVersions()
    {
        var currentCurriculum = CreateCurriculum("HR-EJ", "2026");
        var previousCurriculum = CreateCurriculum("HR-EJ", "2025");
        var sameVersionCurriculum = CreateCurriculum("HR-EJ", "2026");
        var otherCurriculum = CreateCurriculum("HR-MAT", "2025");
        var previous = CreateOutcome(previousCurriculum, "Previous", 0);
        var sameVersion = CreateOutcome(sameVersionCurriculum, "Same version", 0);
        var other = CreateOutcome(otherCurriculum, "Other", 0);

        Assert.Throws<ArgumentException>(() => new CurriculumOutcome(
            Guid.NewGuid(),
            currentCurriculum,
            "Replacement",
            0,
            supersedesOutcome: sameVersion,
            supersededOutcomeCurriculum: sameVersionCurriculum));

        Assert.Throws<ArgumentException>(() => new CurriculumOutcome(
            Guid.NewGuid(),
            currentCurriculum,
            "Invalid cross-family replacement",
            0,
            supersedesOutcome: other,
            supersededOutcomeCurriculum: otherCurriculum));

        var replacement = new CurriculumOutcome(
            Guid.NewGuid(),
            currentCurriculum,
            "Replacement",
            0,
            supersedesOutcome: previous,
            supersededOutcomeCurriculum: previousCurriculum);
        Assert.Equal(previous.Id, replacement.SupersedesOutcomeId);
    }

    [Fact]
    public void EfModelProtectsHierarchyProvenanceAndQueryOrder()
    {
        using var db = CreateDbContext();
        var outcome = db.Model.FindEntityType(typeof(CurriculumOutcome))!;

        Assert.Equal(3, outcome.GetForeignKeys().Count());
        Assert.All(outcome.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(outcome.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(CurriculumOutcome.CurriculumId),
                nameof(CurriculumOutcome.ParentOutcomeId),
            ]));

        var officialCode = Assert.Single(outcome.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(CurriculumOutcome.CurriculumId),
                nameof(CurriculumOutcome.OfficialCode),
            ]));
        Assert.Equal("[OfficialCode] IS NOT NULL", officialCode.GetFilter());
        Assert.Contains(outcome.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(CurriculumOutcome.CurriculumId),
                nameof(CurriculumOutcome.ParentOutcomeId),
                nameof(CurriculumOutcome.SortOrder),
                nameof(CurriculumOutcome.Id),
            ]));
    }

    private static CurriculumOutcome CreateOutcome(
        Curriculum curriculum,
        string title,
        int sortOrder,
        CurriculumOutcome? parent = null,
        string? officialCode = null) => new(
        Guid.NewGuid(),
        curriculum,
        title,
        sortOrder,
        parent,
        officialCode: officialCode,
        sourceAuthority: officialCode is null ? null : "Ministarstvo",
        sourceReference: officialCode is null ? null : "Kurikulum/2026");

    private static Curriculum CreateCurriculum(string code, string version) => new(
        Guid.NewGuid(),
        code,
        $"Curriculum {version}",
        version);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
