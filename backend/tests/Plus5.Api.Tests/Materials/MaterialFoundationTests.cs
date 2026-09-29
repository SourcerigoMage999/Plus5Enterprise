using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialFoundationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 27, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MaterialPreservesSingleOwnerVisibilityArchiveAndConcurrencyBoundary()
    {
        var material = CreateMaterial();

        Assert.Equal(MaterialStatus.Active, material.Status);
        Assert.Equal(MaterialVisibility.Private, material.Visibility);
        Assert.Null(material.CurrentVersionId);

        material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddMinutes(1));
        material.Archive(CreatedAt.AddMinutes(2));

        Assert.Equal(MaterialVisibility.Shared, material.Visibility);
        Assert.Equal(MaterialStatus.Archived, material.Status);
        Assert.Equal(CreatedAt.AddMinutes(2), material.ArchivedAtUtc);
        Assert.Throws<InvalidOperationException>(() =>
            material.SetVisibility(MaterialVisibility.Private, CreatedAt.AddMinutes(3)));
    }

    [Theory]
    [InlineData(MaterialFileFormat.Pdf, 50)]
    [InlineData(MaterialFileFormat.Docx, 25)]
    [InlineData(MaterialFileFormat.Pptx, 100)]
    [InlineData(MaterialFileFormat.Mp4, 250)]
    [InlineData(MaterialFileFormat.Zip, 100)]
    public void FilePolicyUsesLockedPerFormatLimits(MaterialFileFormat format, int megabytes)
    {
        Assert.Equal(
            megabytes * MaterialFilePolicy.Megabyte,
            MaterialFilePolicy.MaximumSizeBytes(format));
    }

    [Fact]
    public void ReadAccessPolicyLimitsSignedAccessToFiveMinutes()
    {
        MaterialFilePolicy.EnsureReadAccessLifetime(TimeSpan.FromMinutes(5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MaterialFilePolicy.EnsureReadAccessLifetime(TimeSpan.FromMinutes(5).Add(TimeSpan.FromTicks(1))));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MaterialFilePolicy.EnsureReadAccessLifetime(TimeSpan.Zero));
    }

    [Fact]
    public void FileUsesOpaqueServerKeyAndRejectsPathMimeOrSizeMismatch()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var file = CreatePdf(material, version);

        Assert.Equal(
            $"materials/{material.OwnerTeacherId:N}/{material.Id:N}/{version.Id:N}/{file.Id:N}",
            file.ObjectKey);
        Assert.DoesNotContain(file.OriginalFileName, file.ObjectKey, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "../lesson.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt));
        Assert.Throws<ArgumentException>(() => new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "lesson.pdf",
            "application/octet-stream",
            100,
            "R2",
            "quarantine",
            CreatedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "lesson.pdf",
            "application/pdf",
            MaterialFilePolicy.MaximumSizeBytes(MaterialFileFormat.Pdf) + 1,
            "R2",
            "quarantine",
            CreatedAt));

        var unicodeFile = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "Cafe\u0301.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt);
        Assert.Equal("Café.pdf", unicodeFile.OriginalFileName);
    }

    [Fact]
    public void FileLifecycleFailsClosedAndBoundsTransientScanRetries()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var file = CreatePdf(material, version);
        var checksum = new string('A', MaterialFilePolicy.ChecksumMaxLength);

        Assert.Throws<InvalidOperationException>(() =>
            file.MarkClean(CreatedAt.AddMinutes(1)));

        file.MarkUploaded(100, checksum, CreatedAt.AddMinutes(1));
        for (var attempt = 1; attempt <= MaterialFilePolicy.MaximumScanAttempts; attempt++)
        {
            file.StartScanning(CreatedAt.AddMinutes(attempt * 2));
            file.MarkFailed("SCANNER_UNAVAILABLE", CreatedAt.AddMinutes((attempt * 2) + 1));
        }

        Assert.Equal(MaterialFileStatus.Failed, file.Status);
        Assert.Equal(MaterialFilePolicy.MaximumScanAttempts, file.ScanAttemptCount);
        Assert.Throws<InvalidOperationException>(() =>
            file.StartScanning(CreatedAt.AddMinutes(10)));
    }

    [Fact]
    public void OnlyCleanFileActivatesVersionAndPublishedSnapshotIsImmutable()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var file = CreatePdf(material, version);

        Assert.Throws<InvalidOperationException>(() =>
            version.Activate(file, CreatedAt.AddMinutes(1)));

        file.MarkUploaded(
            file.DeclaredSizeBytes,
            new string('B', MaterialFilePolicy.ChecksumMaxLength),
            CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        version.Activate(file, CreatedAt.AddMinutes(4));
        material.SetCurrentVersion(version, CreatedAt.AddMinutes(4));

        Assert.Equal(version.Id, material.CurrentVersionId);
        Assert.Equal(MaterialVersionStatus.Active, version.Status);
        Assert.Throws<InvalidOperationException>(() =>
            version.UpdateDraft("Changed", "WORKSHEET"));

        version.Supersede(CreatedAt.AddMinutes(5));
        Assert.Equal(MaterialVersionStatus.Superseded, version.Status);
    }

    [Fact]
    public void RestoreCopiesMetadataIntoNewDraftInsteadOfReactivatingHistory()
    {
        var material = CreateMaterial();
        var version = CreateVersion(material);
        var restored = version.CreateRestoredDraft(
            Guid.NewGuid(),
            material,
            4,
            CreatedAt.AddDays(1));

        Assert.NotEqual(version.Id, restored.Id);
        Assert.Equal(4, restored.VersionNumber);
        Assert.Equal(version.Title, restored.Title);
        Assert.Equal(MaterialVersionStatus.Draft, restored.Status);
    }

    [Fact]
    public void ShareSupportsOnlyViewOrUseAndNeverTransfersOwnership()
    {
        var material = CreateMaterial();
        var recipient = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => new MaterialShare(
            material,
            recipient,
            MaterialShareAccess.View,
            CreatedAt));

        material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddMinutes(1));
        var share = new MaterialShare(
            material,
            recipient,
            MaterialShareAccess.View,
            CreatedAt.AddMinutes(1));

        share.ChangePermission(MaterialShareAccess.Use, CreatedAt.AddMinutes(2));

        Assert.Equal(material.Id, share.MaterialId);
        Assert.Equal(recipient, share.SharedWithTeacherId);
        Assert.Equal(MaterialShareAccess.Use, share.Permission);
        Assert.Throws<ArgumentException>(() => new MaterialShare(
            material,
            material.OwnerTeacherId,
            MaterialShareAccess.View,
            CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void EfModelProtectsOwnershipVersionsFilesSharesAndAvoidsBinaryBlobs()
    {
        using var db = CreateDbContext();
        var material = db.Model.FindEntityType(typeof(Material))!;
        var version = db.Model.FindEntityType(typeof(MaterialVersion))!;
        var file = db.Model.FindEntityType(typeof(MaterialFile))!;
        var share = db.Model.FindEntityType(typeof(MaterialShare))!;

        Assert.True(material.FindProperty(nameof(Material.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(material.GetForeignKeys(), key =>
            key.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Material.Id),
                nameof(Material.CurrentVersionId),
            ]));
        Assert.All(material.GetForeignKeys(), key =>
            Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        Assert.Contains(version.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(MaterialVersion.MaterialId),
                nameof(MaterialVersion.VersionNumber),
            ]));
        Assert.Contains(file.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Single().Name == nameof(MaterialFile.ObjectKey));
        Assert.DoesNotContain(file.GetProperties(), property =>
            property.ClrType == typeof(byte[]));
        Assert.Equal(
            [nameof(MaterialShare.MaterialId), nameof(MaterialShare.SharedWithTeacherId)],
            share.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(share.GetForeignKeys(), key =>
            Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));

        Assert.True(typeof(IMaterialObjectStorage).IsInterface);
        Assert.True(typeof(IMaterialFileValidator).IsInterface);
        Assert.True(typeof(IMalwareScanner).IsInterface);
        Assert.True(typeof(IMaterialAnalysisService).IsInterface);
    }

    private static Material CreateMaterial() =>
        new(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);

    private static MaterialVersion CreateVersion(Material material) =>
        new(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect worksheet",
            "WORKSHEET",
            CreatedAt,
            "Practice material",
            "English",
            "en");

    private static MaterialFile CreatePdf(Material material, MaterialVersion version) =>
        new(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "Present Perfect worksheet.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
