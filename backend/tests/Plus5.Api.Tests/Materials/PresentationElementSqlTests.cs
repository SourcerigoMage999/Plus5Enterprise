using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationElementSqlTests
{
    private const string PreviousMigration =
        "20261009200502_AddPresentationSlideDocumentModel";
    private const int AssetSnapshotViolation = 51243;
    private const int ElementSnapshotViolation = 51244;
    private const int AssetKindViolation = 51245;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradeAddsEmptyCoreTablesAndIsIdempotent()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            Assert.Empty(await db.PresentationAssets.AsNoTracking().ToListAsync());
            Assert.Empty(await db.PresentationElements.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlEnforcesSameVersionAssetBoundsAndPublishedImmutability()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = new UserAccount(
                Guid.NewGuid(),
                "phase73-integrity@plus5.local",
                "PHASE73-INTEGRITY@PLUS5.LOCAL",
                CreatedAt);
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material, 1);
            var document = new PresentationDocument(version, CreatedAt);
            var slide = new PresentationSlide(Guid.NewGuid(), document, 0);
            var asset = new PresentationAsset(
                Guid.NewGuid(),
                version.Id,
                owner.Id,
                material.Id,
                "image.png",
                "image/png",
                128,
                "S3",
                "clean",
                new string('A', 64),
                CreatedAt);
            var element = new PresentationElement(
                Guid.NewGuid(),
                slide,
                "IMAGE",
                0,
                0,
                0,
                800,
                450,
                """{"altText":"","fit":"contain"}""",
                asset.Id);
            var file = CreateCleanFile(material, version);
            db.AddRange(owner, material, version, document, slide, asset, element, file);
            await db.SaveChangesAsync();

            var invalidBounds = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationElements]
                    SET [X] = 1599, [Width] = 2
                    WHERE [Id] = {{element.Id}};
                    """));
            Assert.Equal(547, invalidBounds.Number);

            var invalidAssetKind = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationElements]
                    SET [ElementTypeCode] = N'AUDIO'
                    WHERE [Id] = {{element.Id}};
                    """));
            Assert.Equal(AssetKindViolation, invalidAssetKind.Number);

            var invalidReferencedStatus = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationAssets]
                    SET [Status] = 2
                    WHERE [Id] = {{asset.Id}};
                    """));
            Assert.Equal(AssetKindViolation, invalidReferencedStatus.Number);

            version.Activate(file, CreatedAt.AddMinutes(5));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var elementMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationElements]
                    SET [X] = 10
                    WHERE [Id] = {{element.Id}};
                    """));
            Assert.Equal(ElementSnapshotViolation, elementMutation.Number);

            var assetMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationAssets]
                    SET [OriginalFileName] = N'changed.png'
                    WHERE [Id] = {{asset.Id}};
                    """));
            Assert.Equal(AssetSnapshotViolation, assetMutation.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static MaterialVersion CreateVersion(Material material, int number) =>
        new(
            Guid.NewGuid(),
            material,
            number,
            "Present Perfect",
            "PRESENTATION",
            CreatedAt,
            "Native presentation",
            "English",
            "en");

    private static MaterialFile CreateCleanFile(Material material, MaterialVersion version)
    {
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Zip,
            "presentation.plus5.zip",
            "application/zip",
            100,
            "S3",
            "clean",
            CreatedAt);
        file.MarkUploaded(100, new string('A', 64), CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        return file;
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName)
        CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase73Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        return (options, databaseName);
    }

    private static async Task DeleteDatabase(
        DbContextOptions<Plus5DbContext> options,
        string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
            {
                Skip = "Local disposable SQL credentials required.";
            }
        }
    }
}
