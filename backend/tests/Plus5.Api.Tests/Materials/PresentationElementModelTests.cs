using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationElementModelTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CoreElementRegistryNormalizesClosedStructuredPayloads()
    {
        Assert.True(PresentationElementPayloadRegistry.TryNormalize(
            "TEXT",
            """{"text":"  Present Perfect  ","fontSize":52,"fontWeight":"bold","color":"#173E67","align":"left"}""",
            out var normalized));
        Assert.Contains("Present Perfect", normalized, StringComparison.Ordinal);

        Assert.False(PresentationElementPayloadRegistry.TryNormalize(
            "TEXT",
            """{"text":"unsafe","fontSize":52,"fontWeight":"bold","color":"#173E67","align":"left","html":"<script>"}""",
            out _));
        Assert.False(PresentationElementPayloadRegistry.TryNormalize(
            "QUICK_CHECK",
            "{}",
            out _));
    }

    [Fact]
    public void ElementRequiresSupportedTypeAssetBoundaryAndCanvasBounds()
    {
        var (_, _, slide) = CreatePresentation();
        var text = new PresentationElement(
            Guid.NewGuid(),
            slide,
            "TEXT",
            0,
            0,
            0,
            800,
            200,
            "{}");
        Assert.Equal("TEXT", text.ElementTypeCode);

        Assert.Throws<ArgumentException>(() => new PresentationElement(
            Guid.NewGuid(), slide, "VIDEO", 0, 0, 0, 100, 100, "{}"));
        Assert.Throws<ArgumentException>(() => new PresentationElement(
            Guid.NewGuid(), slide, "IMAGE", 0, 0, 0, 100, 100, "{}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PresentationElement(
            Guid.NewGuid(), slide, "TEXT", 0, 1500, 0, 200, 100, "{}"));
    }

    [Theory]
    [InlineData("image/png", "image.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("image/jpeg", "image.jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0x00 })]
    [InlineData("image/webp", "image.webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 })]
    public async Task ImageValidatorRequiresMatchingExtensionAndSignature(
        string mediaType,
        string fileName,
        byte[] bytes)
    {
        var validator = new PresentationImageValidator();
        await using var stream = new MemoryStream(bytes);

        var result = await validator.ValidateAsync(
            fileName,
            mediaType,
            bytes.Length,
            stream,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Sha256Checksum);
    }

    [Fact]
    public void EfModelUsesRestrictiveVersionBoundElementAndAssetRelationships()
    {
        using var db = CreateDbContext();
        var element = db.Model.FindEntityType(typeof(PresentationElement))!;
        var asset = db.Model.FindEntityType(typeof(PresentationAsset))!;

        Assert.All(element.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.All(asset.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(element.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(PresentationElement.PresentationSlideId),
                nameof(PresentationElement.ZIndex),
            ]));
    }

    private static (MaterialVersion Version, PresentationDocument Document, PresentationSlide Slide)
        CreatePresentation()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Presentation",
            "PRESENTATION",
            CreatedAt);
        var document = new PresentationDocument(version, CreatedAt);
        return (version, document, new PresentationSlide(Guid.NewGuid(), document, 0));
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
