using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Teaching;

public sealed class CurriculumOutcomeSqlTests
{
    private const string PreviousMigration =
        "20260916153606_AddScheduleMaterializationReplenishment";
    private static readonly int[] DuplicateKeyErrorNumbers = [2601, 2627];

    [LocalSqlFact]
    public async Task MigrationUpgradePreservesCurriculumAndIsIdempotent()
    {
        var (options, databaseName) = CreateDatabase();
        var curriculum = CreateCurriculum("HR-EJ", "2026");

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            db.Curricula.Add(curriculum);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.True(await db.Curricula.AsNoTracking()
                .AnyAsync(item => item.Id == curriculum.Id));
            Assert.Empty(await db.CurriculumOutcomes.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task HierarchySupportsArbitraryDepthOrderAndCrossVersionLineage()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var oldCurriculum = CreateCurriculum("MAT-7", "2025");
            var newCurriculum = CreateCurriculum("MAT-7", "2026");
            var oldRoot = CreateOutcome(oldCurriculum, "Old root", 0);
            var newRoot = CreateOutcome(newCurriculum, "New root", 0,
                supersedes: oldRoot,
                supersededCurriculum: oldCurriculum);
            var second = CreateOutcome(newCurriculum, "Second", 20, newRoot);
            var first = CreateOutcome(newCurriculum, "First", 10, newRoot);
            var grandchild = CreateOutcome(newCurriculum, "Grandchild", 0, first);
            db.AddRange(oldCurriculum, newCurriculum, oldRoot, newRoot, second, first, grandchild);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var children = await db.CurriculumOutcomes.AsNoTracking()
                .Where(outcome => outcome.CurriculumId == newCurriculum.Id
                    && outcome.ParentOutcomeId == newRoot.Id)
                .OrderBy(outcome => outcome.SortOrder)
                .ThenBy(outcome => outcome.Id)
                .Select(outcome => outcome.Title)
                .ToListAsync();

            Assert.Equal(["First", "Second"], children);
            Assert.Equal(first.Id, (await db.CurriculumOutcomes.AsNoTracking()
                .SingleAsync(outcome => outcome.Id == grandchild.Id)).ParentOutcomeId);
            Assert.Equal(oldRoot.Id, (await db.CurriculumOutcomes.AsNoTracking()
                .SingleAsync(outcome => outcome.Id == newRoot.Id)).SupersedesOutcomeId);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SameCurriculumParentAndOfficialCodeConstraintsAreEnforced()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            Guid firstCurriculumId;
            Guid secondCurriculumId;
            Guid parentId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var first = CreateCurriculum("HR-EJ", "2026");
                var second = CreateCurriculum("HR-EJ", "2027");
                var parent = CreateOutcome(first, "Parent", 0, officialCode: "A.1");
                firstCurriculumId = first.Id;
                secondCurriculumId = second.Id;
                parentId = parent.Id;
                db.AddRange(first, second, parent);
                await db.SaveChangesAsync();
            }

            await using (var crossParent = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    crossParent.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [CurriculumOutcomes]
                            ([Id], [CurriculumId], [ParentOutcomeId], [SupersedesOutcomeId],
                             [OfficialCode], [SourceAuthority], [SourceReference], [Title],
                             [Description], [SortOrder])
                        VALUES
                            ({{Guid.NewGuid()}}, {{secondCurriculumId}}, {{parentId}}, NULL,
                             NULL, NULL, NULL, N'Cross curriculum', NULL, 0);
                        """));
                Assert.Equal(547, exception.Number);
            }

            await using (var duplicateCode = new Plus5DbContext(options))
            {
                duplicateCode.CurriculumOutcomes.Add(CreateOutcome(
                    new Curriculum(firstCurriculumId, "HR-EJ", "Curriculum 2026", "2026"),
                    "Duplicate official code",
                    1,
                    officialCode: "A.1"));
                var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                    duplicateCode.SaveChangesAsync());
                Assert.Contains(
                    Assert.IsType<SqlException>(exception.InnerException).Number,
                    DuplicateKeyErrorNumbers);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlTriggerRejectsHierarchyCycleAndSameVersionSupersession()
    {
        var (options, databaseName) = CreateDatabase();
        var curriculum = CreateCurriculum("HR-EJ", "2026");

        try
        {
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Curricula.Add(curriculum);
                await db.SaveChangesAsync();
            }

            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            await using (var cycle = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    cycle.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [CurriculumOutcomes]
                            ([Id], [CurriculumId], [ParentOutcomeId], [SupersedesOutcomeId],
                             [OfficialCode], [SourceAuthority], [SourceReference], [Title],
                             [Description], [SortOrder])
                        VALUES
                            ({{firstId}}, {{curriculum.Id}}, {{secondId}}, NULL,
                             NULL, NULL, NULL, N'First', NULL, 0),
                            ({{secondId}}, {{curriculum.Id}}, {{firstId}}, NULL,
                             NULL, NULL, NULL, N'Second', NULL, 1);
                        """));
                Assert.Equal(51001, exception.Number);
            }

            Guid oldId;
            Guid replacementId;
            await using (var seed = new Plus5DbContext(options))
            {
                var old = CreateOutcome(curriculum, "Old", 0);
                var replacement = CreateOutcome(curriculum, "Replacement", 1);
                oldId = old.Id;
                replacementId = replacement.Id;
                seed.AddRange(old, replacement);
                await seed.SaveChangesAsync();
            }

            await using (var sameVersion = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    sameVersion.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [CurriculumOutcomes]
                        SET [SupersedesOutcomeId] = {{oldId}}
                        WHERE [Id] = {{replacementId}};
                        """));
                Assert.Equal(51000, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlTriggerRejectsSupersessionAcrossCurriculumFamilies()
    {
        var (options, databaseName) = CreateDatabase();
        var mathematics = CreateCurriculum("MAT-7", "2026");
        var english = CreateCurriculum("ENG", "2025");

        try
        {
            Guid mathematicsOutcomeId;
            Guid englishOutcomeId;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                var mathematicsOutcome = CreateOutcome(mathematics, "Mathematics", 0);
                var englishOutcome = CreateOutcome(english, "English", 0);
                mathematicsOutcomeId = mathematicsOutcome.Id;
                englishOutcomeId = englishOutcome.Id;
                seed.AddRange(mathematics, english, mathematicsOutcome, englishOutcome);
                await seed.SaveChangesAsync();
            }

            await using var crossFamily = new Plus5DbContext(options);
            var exception = await Assert.ThrowsAsync<SqlException>(() =>
                crossFamily.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [CurriculumOutcomes]
                    SET [SupersedesOutcomeId] = {{englishOutcomeId}}
                    WHERE [Id] = {{mathematicsOutcomeId}};
                    """));
            Assert.Equal(51000, exception.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static Curriculum CreateCurriculum(string code, string version) => new(
        Guid.NewGuid(),
        code,
        $"Curriculum {version}",
        version);

    private static CurriculumOutcome CreateOutcome(
        Curriculum curriculum,
        string title,
        int sortOrder,
        CurriculumOutcome? parent = null,
        CurriculumOutcome? supersedes = null,
        Curriculum? supersededCurriculum = null,
        string? officialCode = null) => new(
        Guid.NewGuid(),
        curriculum,
        title,
        sortOrder,
        parent,
        supersedes,
        supersededCurriculum,
        officialCode,
        officialCode is null ? null : "Ministarstvo",
        officialCode is null ? null : "Kurikulum/2026");

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase51Test_" + Guid.NewGuid().ToString("N");
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
