using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Application.Materials;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationSlideSqlTests
{
    private const string PreviousMigration = "20261004132220_AddAssessableTaskMetadata";
    private const int DocumentSnapshotViolation = 51241;
    private const int SlideSnapshotViolation = 51242;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradeAddsEmptyPresentationTablesWithoutChangingMaterials()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var owner = CreateAccount("phase72-upgrade@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material);
            db.AddRange(owner, material, version);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            Assert.True(await db.MaterialVersions.AsNoTracking()
                .AnyAsync(item => item.Id == version.Id));
            Assert.Empty(await db.PresentationDocuments.AsNoTracking().ToListAsync());
            Assert.Empty(await db.PresentationSlides.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task OwnerSlideCommandsAreAtomicOrderedAndConcurrencySafe()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase72-owner@plus5.local");
            var foreign = CreateAccount("phase72-foreign@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material);
            db.AddRange(owner, foreign, material, version);
            await db.SaveChangesAsync();

            var query = new EfPresentationEditorQuery(db);
            var service = new EfPresentationSlideService(db, TimeProvider.System);
            var initialMaterialRowVersion = Convert.ToBase64String(material.RowVersion);
            var initialized = await service.InitializeAsync(
                owner.Id,
                material.Id,
                new InitializePresentationCommand(version.Id, initialMaterialRowVersion),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.Success, initialized.Outcome);

            db.ChangeTracker.Clear();
            var workspace = (await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None))!;
            var first = Assert.Single(workspace.Document!.Slides);
            var stale = workspace.Document.RowVersion;

            await using var competingDb = new Plus5DbContext(options);
            var competingService = new EfPresentationSlideService(
                competingDb,
                TimeProvider.System);
            var competingWrites = await Task.WhenAll(
                service.AddAsync(
                    owner.Id,
                    material.Id,
                    new AddPresentationSlideCommand(workspace.Document.RowVersion, first.Id),
                    CancellationToken.None),
                competingService.AddAsync(
                    owner.Id,
                    material.Id,
                    new AddPresentationSlideCommand(workspace.Document.RowVersion, first.Id),
                    CancellationToken.None));
            Assert.Single(competingWrites, result =>
                result.Outcome == PresentationMutationOutcome.Success);
            Assert.Single(competingWrites, result =>
                result.Outcome == PresentationMutationOutcome.Conflict);

            db.ChangeTracker.Clear();
            workspace = (await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None))!;
            Assert.Equal([0, 1], workspace.Document!.Slides.Select(slide => slide.SortOrder));

            var staleWrite = await service.DuplicateAsync(
                owner.Id,
                material.Id,
                first.Id,
                new PresentationConcurrencyCommand(stale),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.Conflict, staleWrite.Outcome);

            db.ChangeTracker.Clear();
            var foreignWrite = await service.DuplicateAsync(
                foreign.Id,
                material.Id,
                first.Id,
                new PresentationConcurrencyCommand(workspace.Document.RowVersion),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.NotFound, foreignWrite.Outcome);

            db.ChangeTracker.Clear();
            var reorderedIds = workspace.Document.Slides
                .Select(slide => slide.Id)
                .Reverse()
                .ToList();
            var reordered = await service.ReorderAsync(
                owner.Id,
                material.Id,
                new ReorderPresentationSlidesCommand(
                    workspace.Document.RowVersion,
                    reorderedIds),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.Success, reordered.Outcome);

            db.ChangeTracker.Clear();
            workspace = (await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None))!;
            Assert.Equal(
                reorderedIds,
                workspace.Document!.Slides.Select(slide => slide.Id));

            var deleted = await service.DeleteAsync(
                owner.Id,
                material.Id,
                workspace.Document.Slides[0].Id,
                new PresentationConcurrencyCommand(workspace.Document.RowVersion),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.Success, deleted.Outcome);

            db.ChangeTracker.Clear();
            workspace = (await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None))!;
            Assert.Single(workspace.Document!.Slides);
            Assert.Equal(0, workspace.Document.Slides[0].SortOrder);

            var lastDelete = await service.DeleteAsync(
                owner.Id,
                material.Id,
                workspace.Document.Slides[0].Id,
                new PresentationConcurrencyCommand(workspace.Document.RowVersion),
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.LastSlide, lastDelete.Outcome);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlRejectsNonPresentationDocumentAndPublishedSnapshotMutation()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase72-integrity@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material);
            var document = new PresentationDocument(version, CreatedAt);
            var slide = new PresentationSlide(Guid.NewGuid(), document, 0);
            var file = CreateCleanFile(material, version);
            db.AddRange(owner, material, version, document, slide, file);
            await db.SaveChangesAsync();

            var worksheetMaterial = new Material(Guid.NewGuid(), owner.Id, CreatedAt.AddHours(1));
            var worksheetVersion = new MaterialVersion(
                Guid.NewGuid(),
                worksheetMaterial,
                1,
                "Worksheet",
                "WORKSHEET",
                CreatedAt.AddHours(1));
            db.AddRange(worksheetMaterial, worksheetVersion);
            await db.SaveChangesAsync();
            var invalidDocument = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [PresentationDocuments]
                        ([MaterialVersionId], [DocumentSchemaVersion], [CanvasWidth], [CanvasHeight],
                         [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES
                        ({{worksheetVersion.Id}}, 1, 1600, 900, {{CreatedAt}}, {{CreatedAt}});
                    """));
            Assert.Equal(DocumentSnapshotViolation, invalidDocument.Number);

            version.Activate(file, CreatedAt.AddMinutes(5));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var documentMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationDocuments]
                    SET [UpdatedAtUtc] = {{CreatedAt.AddMinutes(6)}}
                    WHERE [MaterialVersionId] = {{version.Id}};
                    """));
            Assert.Equal(DocumentSnapshotViolation, documentMutation.Number);

            var slideMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationSlides]
                    SET [SortOrder] = 1
                    WHERE [Id] = {{slide.Id}};
                    """));
            Assert.Equal(SlideSnapshotViolation, slideMutation.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static UserAccount CreateAccount(string email) =>
        new(Guid.NewGuid(), email, email.ToUpperInvariant(), CreatedAt);

    private static MaterialVersion CreateVersion(Material material) =>
        new(
            Guid.NewGuid(),
            material,
            1,
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
            "R2",
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
        var databaseName = "Plus5_Phase72Test_" + Guid.NewGuid().ToString("N");
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
