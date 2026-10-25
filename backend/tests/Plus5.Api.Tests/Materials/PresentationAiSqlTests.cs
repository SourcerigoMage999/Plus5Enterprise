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

public sealed class PresentationAiSqlTests
{
    private const string PreviousMigration =
        "20261018164107_AddPresentationQuickChecks";
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationAndDatabaseProtectSuggestionLifecycleAndDocumentVersion()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var teacher = new UserAccount(
                Guid.NewGuid(),
                "phase79-owner@plus5.local",
                "PHASE79-OWNER@PLUS5.LOCAL",
                CreatedAt);
            var material = new Material(Guid.NewGuid(), teacher.Id, CreatedAt);
            var firstVersion = new MaterialVersion(
                Guid.NewGuid(),
                material,
                1,
                "AI assistance",
                "PRESENTATION",
                CreatedAt);
            var firstDocument = new PresentationDocument(firstVersion, CreatedAt);
            var firstSlide = new PresentationSlide(Guid.NewGuid(), firstDocument, 0);
            var secondVersion = new MaterialVersion(
                Guid.NewGuid(),
                material,
                2,
                "Other draft document",
                "PRESENTATION",
                CreatedAt);
            var secondDocument = new PresentationDocument(secondVersion, CreatedAt);
            db.AddRange(
                teacher,
                material,
                firstVersion,
                firstDocument,
                firstSlide,
                secondVersion,
                secondDocument);
            await db.SaveChangesAsync();

            var suggestion = new PresentationAiSuggestion(
                Guid.NewGuid(),
                teacher.Id,
                material.Id,
                firstVersion.Id,
                firstDocument.MaterialVersionId,
                PresentationAiSuggestionKinds.ElementContent,
                "OPENAI",
                PresentationAiOptions.CanonicalModel,
                "presentation-ai-v1",
                "{\"elementTypeCode\":\"TEXT\",\"content\":\"Primjer\"}",
                CreatedAt,
                CreatedAt.AddHours(24),
                firstSlide.Id);
            db.PresentationAiSuggestions.Add(suggestion);
            await db.SaveChangesAsync();

            suggestion.Resolve(
                PresentationAiSuggestionStatus.Accepted,
                CreatedAt.AddMinutes(1));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var saved = await db.PresentationAiSuggestions.AsNoTracking()
                .SingleAsync(item => item.Id == suggestion.Id);
            Assert.Equal(PresentationAiSuggestionStatus.Accepted, saved.Status);
            Assert.Null(saved.GeneratedContentJson);

            var mismatchedDocument = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [PresentationAiSuggestions]
                    ([Id], [TeacherAccountId], [MaterialId], [MaterialVersionId],
                     [PresentationDocumentId], [TargetSlideId], [TargetElementId],
                     [SuggestionKind], [Provider], [Model], [PromptTemplateVersion],
                     [GeneratedContentJson], [Status], [GeneratedAtUtc], [ExpiresAtUtc],
                     [ResolvedAtUtc])
                    VALUES
                    ({{Guid.NewGuid()}}, {{teacher.Id}}, {{material.Id}}, {{firstVersion.Id}},
                     {{secondDocument.MaterialVersionId}}, NULL, NULL,
                     N'ELEMENT_CONTENT', N'OPENAI', N'gpt-6.1-sol', N'presentation-ai-v1',
                     N'{"elementTypeCode":"TEXT","content":"Primjer"}', 1,
                     {{CreatedAt}}, {{CreatedAt.AddHours(24)}}, NULL);
                    """));
            Assert.NotEqual(0, mismatchedDocument.Number);

            var leakedResolvedContent = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [PresentationAiSuggestions]
                    SET [GeneratedContentJson] = N'{"elementTypeCode":"TEXT","content":"Ne smije ostati"}'
                    WHERE [Id] = {{suggestion.Id}};
                    """));
            Assert.NotEqual(0, leakedResolvedContent.Number);
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
        var databaseName = "Plus5_Phase79Test_" + Guid.NewGuid().ToString("N");
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
