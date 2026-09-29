using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialFoundationSqlTests
{
    private const string PreviousMigration = "20260924214206_AddMasteryReadinessV1";
    private const int CheckConstraintViolation = 547;
    private const int MaterialVersionLifecycleViolation = 51221;
    private const int MaterialFileLifecycleViolation = 51222;
    private const int MaterialSelfShareViolation = 51223;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradePreservesExistingRowsAndStartsWithEmptyMaterialCatalog()
    {
        var (options, databaseName) = CreateDatabase();
        var owner = CreateAccount("owner-upgrade@plus5.local");

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            db.Add(owner);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.True(await db.UserAccounts.AsNoTracking()
                .AnyAsync(account => account.Id == owner.Id));
            Assert.Empty(await db.Materials.AsNoTracking().ToListAsync());
            Assert.Empty(await db.MaterialVersions.AsNoTracking().ToListAsync());
            Assert.Empty(await db.MaterialFiles.AsNoTracking().ToListAsync());
            Assert.Empty(await db.MaterialShares.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlLifecycleRequiresCleanFileAndProtectsPublishedSnapshots()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("owner-lifecycle@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material, 1);
            var file = CreatePdf(material, version);
            db.AddRange(owner, material, version, file);
            await db.SaveChangesAsync();

            var activationWithoutClean = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersions]
                    SET [Status] = 2, [ActivatedAtUtc] = {{CreatedAt.AddMinutes(1)}}
                    WHERE [Id] = {{version.Id}};
                    """));
            Assert.Equal(MaterialVersionLifecycleViolation, activationWithoutClean.Number);

            file.MarkUploaded(
                file.DeclaredSizeBytes,
                new string('A', MaterialFilePolicy.ChecksumMaxLength),
                CreatedAt.AddMinutes(2));
            await db.SaveChangesAsync();
            file.StartScanning(CreatedAt.AddMinutes(3));
            await db.SaveChangesAsync();
            file.MarkClean(CreatedAt.AddMinutes(4));
            await db.SaveChangesAsync();

            version.Activate(file, CreatedAt.AddMinutes(5));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(
                version.Id,
                (await db.Materials.AsNoTracking()
                    .SingleAsync(item => item.Id == material.Id)).CurrentVersionId);

            var versionMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersions]
                    SET [Title] = N'Changed outside the domain'
                    WHERE [Id] = {{version.Id}};
                    """));
            Assert.Equal(MaterialVersionLifecycleViolation, versionMutation.Number);

            var fileMutation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialFiles]
                    SET [ObjectKey] = N'materials/changed'
                    WHERE [Id] = {{file.Id}};
                    """));
            Assert.Equal(MaterialFileLifecycleViolation, fileMutation.Number);

            version = await db.MaterialVersions.SingleAsync(item => item.Id == version.Id);
            version.Supersede(CreatedAt.AddMinutes(6));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Assert.Null((await db.Materials.AsNoTracking()
                .SingleAsync(item => item.Id == material.Id)).CurrentVersionId);
            Assert.Equal(
                MaterialVersionStatus.Superseded,
                (await db.MaterialVersions.AsNoTracking()
                    .SingleAsync(item => item.Id == version.Id)).Status);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlConstraintsRejectSelfShareAndOversizedFormat()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("owner-constraints@plus5.local");
            var recipient = CreateAccount("recipient-constraints@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material, 1);
            db.AddRange(owner, recipient, material, version);
            await db.SaveChangesAsync();

            var privateShare = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialShares]
                        ([MaterialId], [SharedWithTeacherId], [Permission], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES
                        ({{material.Id}}, {{recipient.Id}}, 1, {{CreatedAt}}, {{CreatedAt}});
                    """));
            Assert.Equal(MaterialSelfShareViolation, privateShare.Number);

            material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddMinutes(1));
            await db.SaveChangesAsync();

            var selfShare = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialShares]
                        ([MaterialId], [SharedWithTeacherId], [Permission], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES
                        ({{material.Id}}, {{owner.Id}}, 1, {{CreatedAt}}, {{CreatedAt}});
                    """));
            Assert.Equal(MaterialSelfShareViolation, selfShare.Number);

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [MaterialShares]
                    ([MaterialId], [SharedWithTeacherId], [Permission], [CreatedAtUtc], [UpdatedAtUtc])
                VALUES
                    ({{material.Id}}, {{recipient.Id}}, 2, {{CreatedAt}}, {{CreatedAt}});
                """);
            Assert.True(await db.MaterialShares.AsNoTracking().AnyAsync(share =>
                share.MaterialId == material.Id
                && share.SharedWithTeacherId == recipient.Id));

            var fileId = Guid.NewGuid();
            var oversized = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialFiles]
                        ([Id], [MaterialVersionId], [Format], [OriginalFileName], [DeclaredMediaType],
                         [DeclaredSizeBytes], [ActualSizeBytes], [StorageProvider], [StorageContainer],
                         [ObjectKey], [Sha256Checksum], [Status], [ScanAttemptCount],
                         [LastScanResultCategory], [CreatedAtUtc], [UpdatedAtUtc], [UploadedAtUtc], [ScannedAtUtc])
                    VALUES
                        ({{fileId}}, {{version.Id}}, 1, N'oversized.pdf', N'application/pdf',
                         52428801, NULL, N'R2', N'quarantine',
                         {{CreateObjectKey(owner.Id, material.Id, version.Id, fileId)}}, NULL, 1, 0,
                         NULL, {{CreatedAt}}, {{CreatedAt}}, NULL, NULL);
                    """));
            Assert.Equal(CheckConstraintViolation, oversized.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static UserAccount CreateAccount(string email) =>
        new(Guid.NewGuid(), email, email.ToUpperInvariant(), CreatedAt);

    private static MaterialVersion CreateVersion(Material material, int versionNumber) =>
        new(
            Guid.NewGuid(),
            material,
            versionNumber,
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

    private static string CreateObjectKey(
        Guid ownerId,
        Guid materialId,
        Guid versionId,
        Guid fileId) =>
        $"materials/{ownerId:N}/{materialId:N}/{versionId:N}/{fileId:N}";

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase61Test_" + Guid.NewGuid().ToString("N");
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
