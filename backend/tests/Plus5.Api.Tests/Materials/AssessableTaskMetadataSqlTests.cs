using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Evidence;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class AssessableTaskMetadataSqlTests
{
    private const string PreviousMigration = "20260929204328_AddMaterialMetadataMapping";
    private const int SnapshotMutationViolation = 51231;
    private const int KnowledgeTargetViolation = 51232;
    private const int MissingKnowledgeTargetViolation = 51233;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 4, 8, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradeAddsEmptyTaskMetadataWithoutChangingMaterials()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var owner = CreateAccount("phase65-upgrade@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateMaterialVersion(material);
            db.AddRange(owner, material, version);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.NotNull(await db.MaterialVersions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == version.Id));
            Assert.Empty(await db.AssessableTasks.AsNoTracking().ToListAsync());
            Assert.Empty(await db.AssessableTaskVersions.AsNoTracking().ToListAsync());
            Assert.Empty(await db.AssessableTaskVersionKnowledgeComponents
                .AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlProtectsLeafTargetsRequiredMappingAndPublishedSnapshot()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase65-integrity@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateMaterialVersion(material);
            var file = CreateCleanFile(material, version);
            var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5-EN", "2026");
            var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
            var parent = new KnowledgeComponent(
                Guid.NewGuid(), model, area, "Present Perfect", 0);
            var leaf = new KnowledgeComponent(
                Guid.NewGuid(), model, area, "Affirmative form", 0, parent);
            var task = new AssessableTask(Guid.NewGuid(), material, CreatedAt);
            var taskVersion = CreateTaskVersion(task, version);
            db.AddRange(owner, material, version, file, model, area, parent, leaf, task, taskVersion);
            await db.SaveChangesAsync();
            model.Publish();
            await db.SaveChangesAsync();

            var parentTarget = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [AssessableTaskVersionKnowledgeComponents]
                        ([AssessableTaskVersionId], [KnowledgeComponentId])
                    VALUES ({{taskVersion.Id}}, {{parent.Id}});
                    """));
            Assert.Equal(KnowledgeTargetViolation, parentTarget.Number);

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [AssessableTaskVersionKnowledgeComponents]
                    ([AssessableTaskVersionId], [KnowledgeComponentId])
                VALUES ({{taskVersion.Id}}, {{leaf.Id}});
                """);

            version = await db.MaterialVersions.SingleAsync(item => item.Id == version.Id);
            file = await db.MaterialFiles.SingleAsync(item => item.Id == file.Id);
            version.Activate(file, CreatedAt.AddMinutes(4));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var updateTask = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [AssessableTaskVersions]
                    SET [Prompt] = N'Changed prompt'
                    WHERE [Id] = {{taskVersion.Id}};
                    """));
            Assert.Equal(SnapshotMutationViolation, updateTask.Number);

            var removeTarget = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [AssessableTaskVersionKnowledgeComponents]
                    WHERE [AssessableTaskVersionId] = {{taskVersion.Id}}
                      AND [KnowledgeComponentId] = {{leaf.Id}};
                    """));
            Assert.Equal(SnapshotMutationViolation, removeTarget.Number);

            var secondMaterial = new Material(Guid.NewGuid(), owner.Id, CreatedAt.AddHours(1));
            var secondVersion = CreateMaterialVersion(secondMaterial, CreatedAt.AddHours(1));
            var secondFile = CreateCleanFile(secondMaterial, secondVersion, CreatedAt.AddHours(1));
            var secondTask = new AssessableTask(
                Guid.NewGuid(), secondMaterial, CreatedAt.AddHours(1));
            var unmappedVersion = CreateTaskVersion(
                secondTask, secondVersion, CreatedAt.AddHours(1));
            db.AddRange(secondMaterial, secondVersion, secondFile, secondTask, unmappedVersion);
            await db.SaveChangesAsync();
            secondVersion.Activate(secondFile, CreatedAt.AddHours(1).AddMinutes(4));

            var missingTarget = await Assert.ThrowsAsync<DbUpdateException>(() =>
                db.SaveChangesAsync());
            Assert.Equal(
                MissingKnowledgeTargetViolation,
                Assert.IsType<SqlException>(missingTarget.InnerException).Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static UserAccount CreateAccount(string email) =>
        new(Guid.NewGuid(), email, email.ToUpperInvariant(), CreatedAt);

    private static MaterialVersion CreateMaterialVersion(
        Material material,
        DateTimeOffset? createdAtUtc = null) =>
        new(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect worksheet",
            "WORKSHEET",
            createdAtUtc ?? CreatedAt,
            "Practice material",
            "English",
            "en");

    private static AssessableTaskVersion CreateTaskVersion(
        AssessableTask task,
        MaterialVersion version,
        DateTimeOffset? createdAtUtc = null) =>
        new(
            Guid.NewGuid(),
            task,
            version,
            1,
            0,
            "I ____ London twice.",
            "SINGLE_CHOICE",
            1,
            EvidenceType.Recognition,
            1,
            createdAtUtc ?? CreatedAt,
            correctAnswer: "have visited");

    private static MaterialFile CreateCleanFile(
        Material material,
        MaterialVersion version,
        DateTimeOffset? createdAtUtc = null)
    {
        var createdAt = createdAtUtc ?? CreatedAt;
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "worksheet.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            createdAt);
        file.MarkUploaded(100, new string('A', 64), createdAt.AddMinutes(1));
        file.StartScanning(createdAt.AddMinutes(2));
        file.MarkClean(createdAt.AddMinutes(3));
        return file;
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase65Test_" + Guid.NewGuid().ToString("N");
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
