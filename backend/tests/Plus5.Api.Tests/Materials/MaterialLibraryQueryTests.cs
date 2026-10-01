using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialLibraryQueryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MineIsOwnerScopedSearchableFilterableAndExcludesNonCurrentContent()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "English 8", CreatedAt);
        var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
        var level = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", 3);
        await using var db = CreateDbContext();
        db.AddRange(program, grade, level);

        var matching = AddActiveMaterial(
            db,
            teacherId,
            "Present Perfect worksheet",
            "WORKSHEET",
            CreatedAt.AddHours(1),
            program,
            grade,
            level,
            "Grammar",
            "present perfect");
        AddActiveMaterial(
            db,
            teacherId,
            "Listening interview",
            "AUDIO",
            CreatedAt.AddHours(2),
            program,
            grade,
            level,
            "Listening");
        AddActiveMaterial(
            db,
            otherTeacherId,
            "Present Perfect foreign",
            "WORKSHEET",
            CreatedAt.AddHours(3),
            subject: "Grammar");
        var draftMaterial = new Material(Guid.NewGuid(), teacherId, CreatedAt.AddHours(4));
        var draft = new MaterialVersion(
            Guid.NewGuid(),
            draftMaterial,
            1,
            "Present Perfect draft",
            "WORKSHEET",
            CreatedAt.AddHours(4));
        db.AddRange(draftMaterial, draft);
        await db.SaveChangesAsync();

        var query = new EfMaterialLibraryQuery(db);
        var page = await query.GetPageAsync(
            teacherId,
            new MaterialLibraryCriteria(
                1,
                24,
                MaterialLibraryOwnership.Mine,
                MaterialLibrarySort.Newest,
                "Present Perfect",
                "Grammar",
                program.Id,
                grade.Id,
                "WORKSHEET",
                "present perfect"),
            CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(matching.Material.Id, item.Id);
        Assert.True(item.IsOwner);
        Assert.Null(item.ShareAccess);
        Assert.Equal(["present perfect"], item.Tags);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task SharedWithMeRequiresExplicitGrantAndSharedVisibility()
    {
        var ownerId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var unrelatedRecipientId = Guid.NewGuid();
        await using var db = CreateDbContext();

        var shared = AddActiveMaterial(
            db,
            ownerId,
            "Shared presentation",
            "PRESENTATION",
            CreatedAt.AddHours(1));
        shared.Material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddHours(2));
        db.MaterialShares.Add(new MaterialShare(
            shared.Material,
            recipientId,
            MaterialShareAccess.Use,
            CreatedAt.AddHours(2)));

        var privateMaterial = AddActiveMaterial(
            db,
            ownerId,
            "Private worksheet",
            "WORKSHEET",
            CreatedAt.AddHours(3));
        var sharedElsewhere = AddActiveMaterial(
            db,
            ownerId,
            "Other recipient",
            "VIDEO",
            CreatedAt.AddHours(4));
        sharedElsewhere.Material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddHours(5));
        db.MaterialShares.Add(new MaterialShare(
            sharedElsewhere.Material,
            unrelatedRecipientId,
            MaterialShareAccess.View,
            CreatedAt.AddHours(5)));
        await db.SaveChangesAsync();

        var query = new EfMaterialLibraryQuery(db);
        var page = await query.GetPageAsync(
            recipientId,
            DefaultCriteria(MaterialLibraryOwnership.SharedWithMe),
            CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(shared.Material.Id, item.Id);
        Assert.False(item.IsOwner);
        Assert.Equal(MaterialLibraryShareAccess.Use, item.ShareAccess);
        Assert.DoesNotContain(page.Items, entry => entry.Id == privateMaterial.Material.Id);
    }

    [Fact]
    public async Task OverviewUsesOnlyAccessibleCurrentMaterialsAndReturnsActualFacets()
    {
        var teacherId = Guid.NewGuid();
        var program = new TeachingProgram(Guid.NewGuid(), teacherId, "English 8", CreatedAt);
        var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
        await using var db = CreateDbContext();
        db.AddRange(program, grade);
        AddActiveMaterial(
            db,
            teacherId,
            "Grammar worksheet",
            "WORKSHEET",
            CreatedAt.AddHours(1),
            program,
            grade,
            subject: "Grammar",
            tag: "revision");
        AddActiveMaterial(
            db,
            teacherId,
            "Grammar presentation",
            "PRESENTATION",
            CreatedAt.AddHours(2),
            program,
            grade,
            subject: "Grammar");
        AddActiveMaterial(
            db,
            Guid.NewGuid(),
            "Foreign video",
            "VIDEO",
            CreatedAt.AddHours(3));
        await db.SaveChangesAsync();

        var query = new EfMaterialLibraryQuery(db);
        var overview = await query.GetOverviewAsync(
            teacherId,
            MaterialLibraryOwnership.Mine,
            CancellationToken.None);

        Assert.Equal(["Grammar"], overview.Subjects);
        Assert.Equal(program.Id, Assert.Single(overview.Programs).Id);
        Assert.Equal(grade.Id, Assert.Single(overview.SchoolGrades).Id);
        Assert.Equal(["PRESENTATION", "WORKSHEET"], overview.MaterialTypes);
        Assert.Equal(["revision"], overview.Tags);
        Assert.Equal(2, overview.MaterialTypeCounts.Sum(item => item.Count));
        Assert.Equal("Grammar presentation", overview.RecentlyAdded[0].Title);
        Assert.DoesNotContain(overview.MaterialTypes, code => code == "VIDEO");
    }

    private static MaterialLibraryCriteria DefaultCriteria(MaterialLibraryOwnership ownership) =>
        new(1, 24, ownership, MaterialLibrarySort.Newest, null, null, null, null, null, null);

    private static MaterialFixture AddActiveMaterial(
        Plus5DbContext db,
        Guid teacherId,
        string title,
        string materialTypeCode,
        DateTimeOffset createdAt,
        TeachingProgram? program = null,
        SchoolGrade? grade = null,
        ProficiencyLevel? level = null,
        string? subject = null,
        string? tag = null)
    {
        var material = new Material(Guid.NewGuid(), teacherId, createdAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            title,
            materialTypeCode,
            createdAt,
            subject: subject,
            program: program,
            schoolGrade: grade,
            proficiencyLevel: level);
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "material.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            createdAt);
        db.AddRange(material, version, file);
        if (tag is not null)
        {
            db.MaterialVersionTags.Add(new MaterialVersionTag(version, tag));
        }

        file.MarkUploaded(100, new string('A', 64), createdAt.AddMinutes(1));
        file.StartScanning(createdAt.AddMinutes(2));
        file.MarkClean(createdAt.AddMinutes(3));
        version.Activate(file, createdAt.AddMinutes(4));
        material.SetCurrentVersion(version, createdAt.AddMinutes(4));
        return new MaterialFixture(material, version);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }

    private sealed record MaterialFixture(Material Material, MaterialVersion Version);
}
