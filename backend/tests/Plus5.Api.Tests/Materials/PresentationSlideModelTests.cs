using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationSlideModelTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DraftPresentationCreatesVersionBoundDocumentAndSlide()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect",
            "PRESENTATION",
            CreatedAt);
        var document = new PresentationDocument(version, CreatedAt);
        var slide = new PresentationSlide(Guid.NewGuid(), document, 0);

        Assert.Equal(version.Id, document.MaterialVersionId);
        Assert.Equal(PresentationDocument.CanvasWidthValue, document.CanvasWidth);
        Assert.Equal(PresentationDocument.CanvasHeightValue, document.CanvasHeight);
        Assert.Equal(document.MaterialVersionId, slide.MaterialVersionId);
        Assert.Equal(0, slide.SortOrder);
    }

    [Fact]
    public void DocumentAndSlideRejectInvalidShape()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);
        var worksheet = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Worksheet",
            "WORKSHEET",
            CreatedAt);

        Assert.Throws<ArgumentException>(() =>
            new PresentationDocument(worksheet, CreatedAt));

        var presentation = new MaterialVersion(
            Guid.NewGuid(),
            material,
            2,
            "Presentation",
            "PRESENTATION",
            CreatedAt);
        var document = new PresentationDocument(presentation, CreatedAt);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PresentationSlide(Guid.NewGuid(), document, -1));
    }

    [Fact]
    public void EfModelUsesDocumentConcurrencyAndUniqueSlideOrder()
    {
        using var db = CreateDbContext();
        var document = db.Model.FindEntityType(typeof(PresentationDocument))!;
        var slide = db.Model.FindEntityType(typeof(PresentationSlide))!;

        Assert.True(document.FindProperty(nameof(PresentationDocument.RowVersion))!
            .IsConcurrencyToken);
        Assert.Contains(slide.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(PresentationSlide.MaterialVersionId),
                nameof(PresentationSlide.SortOrder),
            ]));
        Assert.All(document.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.All(slide.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public async Task MissingRowVersionIsRejectedWithoutThrowing()
    {
        await using var db = CreateDbContext();
        var service = new EfPresentationSlideService(db, TimeProvider.System);

        var result = await service.AddAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AddPresentationSlideCommand(null!, null),
            CancellationToken.None);

        Assert.Equal(PresentationMutationOutcome.InvalidInput, result.Outcome);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
