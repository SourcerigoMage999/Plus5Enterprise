using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;
using Plus5.Domain.Evidence;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationQuickCheckSqlTests
{
    private const string PreviousMigration =
        "20261016200658_AddPresentationTeachingElements";
    private const int QuickCheckPayloadViolation = 51246;
    private const int QuickCheckLinkViolation = 51247;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationAndOwnerCommandPersistAtomicVersionBoundQuickCheck()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var owner = new UserAccount(
                Guid.NewGuid(),
                "phase75-owner@plus5.local",
                "PHASE75-OWNER@PLUS5.LOCAL",
                CreatedAt);
            var foreign = new UserAccount(
                Guid.NewGuid(),
                "phase75-foreign@plus5.local",
                "PHASE75-FOREIGN@PLUS5.LOCAL",
                CreatedAt);
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = new MaterialVersion(
                Guid.NewGuid(),
                material,
                1,
                "Present Continuous",
                "PRESENTATION",
                CreatedAt);
            var document = new PresentationDocument(version, CreatedAt);
            var slide = new PresentationSlide(Guid.NewGuid(), document, 0);
            db.AddRange(owner, foreign, material, version, document, slide);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var query = new EfPresentationEditorQuery(db);
            var initialWorkspace = await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None);
            Assert.NotNull(initialWorkspace?.Document);

            var service = new EfPresentationElementService(
                db,
                null!,
                null!,
                null!,
                Options.Create(new MaterialStorageOptions()),
                TimeProvider.System);
            var command = new UpsertPresentationQuickCheckCommand(
                initialWorkspace!.Document!.RowVersion,
                0,
                430,
                260,
                740,
                390,
                "Koje vrijeme opisuje radnju koja se događa sada?",
                ["Present Simple", "Present Continuous", "Past Simple"],
                1,
                2,
                EvidenceType.Recognition);

            var created = await service.CreateQuickCheckAsync(
                owner.Id,
                material.Id,
                slide.Id,
                command,
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.Success, created.Outcome);

            db.ChangeTracker.Clear();
            var element = await db.PresentationElements.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == created.ElementId);
            var taskVersion = await db.AssessableTaskVersions.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == element.AssessableTaskVersionId);
            Assert.Equal("QUICK_CHECK", element.ElementTypeCode);
            Assert.Equal(version.Id, taskVersion.MaterialVersionId);
            Assert.Equal(AssessableTaskVersion.SingleChoiceQuickCheckTypeCode, taskVersion.TaskTypeCode);
            Assert.Equal("B", taskVersion.CorrectAnswer);
            Assert.Equal(1m, taskVersion.MaxPoints);
            Assert.NotNull(taskVersion.AuthoringPayloadJson);

            var workspace = await query.GetAsync(
                owner.Id,
                material.Id,
                CancellationToken.None);
            var view = Assert.Single(Assert.Single(workspace!.Document!.Slides).Elements);
            Assert.Equal("Present Continuous", Assert.Single(
                view.QuickCheck!.Options,
                option => option.Key == view.QuickCheck.CorrectOptionKey).Text);
            Assert.True(view.QuickCheck.KnowledgeMappingPending);

            var latestRowVersion = workspace.Document.RowVersion;
            var foreignWrite = await service.CreateQuickCheckAsync(
                foreign.Id,
                material.Id,
                slide.Id,
                command with { ExpectedDocumentRowVersion = latestRowVersion },
                CancellationToken.None);
            Assert.Equal(PresentationMutationOutcome.NotFound, foreignWrite.Outcome);

            var invalidPayload = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [AssessableTaskVersions]
                    SET [AuthoringPayloadJson] = N'{"schemaVersion":1,"options":[{"key":"A","text":"Jedan"}]}'
                    WHERE [Id] = {{taskVersion.Id}};
                    """));
            Assert.Equal(QuickCheckPayloadViolation, invalidPayload.Number);

            var otherTask = new AssessableTask(Guid.NewGuid(), material, CreatedAt);
            var otherVersion = new AssessableTaskVersion(
                Guid.NewGuid(),
                otherTask,
                version,
                1,
                1,
                "Drugi zadatak",
                "MULTIPLE_CHOICE",
                2,
                EvidenceType.Recognition,
                1m,
                CreatedAt,
                "A");
            db.AddRange(otherTask, otherVersion);
            await db.SaveChangesAsync();

            var invalidLink = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationElements]
                    SET [AssessableTaskVersionId] = {{otherVersion.Id}}
                    WHERE [Id] = {{element.Id}};
                    """));
            Assert.Equal(QuickCheckLinkViolation, invalidLink.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName)
        CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase75Test_" + Guid.NewGuid().ToString("N");
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
