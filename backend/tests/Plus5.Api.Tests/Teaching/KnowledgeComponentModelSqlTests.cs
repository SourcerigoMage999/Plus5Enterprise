using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Teaching;

public sealed class KnowledgeComponentModelSqlTests
{
    private const string PreviousMigration =
        "20260916193850_AddCurriculumOutcomeHierarchy";
    private static readonly int[] DuplicateKeyErrorNumbers = [2601, 2627];

    [LocalSqlFact]
    public async Task MigrationUpgradePreservesOutcomeAndStartsWithEmptyKnowledgeCatalog()
    {
        var (options, databaseName) = CreateDatabase();
        var curriculum = new Curriculum(Guid.NewGuid(), "MAT-7", "Mathematics", "2026");
        var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            db.AddRange(curriculum, outcome);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.True(await db.CurriculumOutcomes.AsNoTracking()
                .AnyAsync(item => item.Id == outcome.Id));
            Assert.Empty(await db.KnowledgeModels.AsNoTracking().ToListAsync());
            Assert.Empty(await db.KnowledgeAreas.AsNoTracking().ToListAsync());
            Assert.Empty(await db.KnowledgeComponents.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task TreeTraversalLeafQueryLineageAndMappingAreReproducible()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();

            var curriculum = new Curriculum(Guid.NewGuid(), "MAT-7", "Mathematics", "2026");
            var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);
            var previousModel = CreateModel("PLUS5_CORE", "V1");
            var currentModel = CreateModel("PLUS5_CORE", "V2");
            var previousArea = CreateArea(previousModel, "Area", 0);
            var currentArea = CreateArea(currentModel, "Area", 0);
            var previous = CreateComponent(previousModel, previousArea, "Previous", 0);
            var root = CreateComponent(
                currentModel,
                currentArea,
                "Root",
                0,
                supersedes: previous,
                supersededModel: previousModel);
            var branch = CreateComponent(currentModel, currentArea, "Branch", 0, root);
            var leaf = CreateComponent(currentModel, currentArea, "Leaf", 0, branch);
            var mapping = new CurriculumOutcomeKnowledgeComponent(outcome, leaf);
            db.AddRange(
                curriculum,
                outcome,
                previousModel,
                currentModel,
                previousArea,
                currentArea,
                previous,
                root,
                branch,
                leaf,
                mapping);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var orderedChildren = await db.KnowledgeComponents.AsNoTracking()
                .Where(component => component.KnowledgeModelId == currentModel.Id
                    && component.KnowledgeAreaId == currentArea.Id
                    && component.ParentComponentId == root.Id)
                .OrderBy(component => component.SortOrder)
                .ThenBy(component => component.Id)
                .Select(component => component.Id)
                .ToListAsync();
            var leaves = await db.KnowledgeComponents.AsNoTracking()
                .Where(component => component.KnowledgeModelId == currentModel.Id)
                .Where(component => !db.KnowledgeComponents
                    .Any(child => child.ParentComponentId == component.Id))
                .Select(component => component.Id)
                .ToListAsync();

            Assert.Equal([branch.Id], orderedChildren);
            Assert.Equal([leaf.Id], leaves);
            Assert.Equal(previous.Id, (await db.KnowledgeComponents.AsNoTracking()
                .SingleAsync(component => component.Id == root.Id))
                .SupersedesKnowledgeComponentId);
            Assert.True(await db.CurriculumOutcomeKnowledgeComponents.AsNoTracking()
                .AnyAsync(item => item.CurriculumOutcomeId == outcome.Id
                    && item.KnowledgeComponentId == leaf.Id));

            await using var duplicateMapping = new Plus5DbContext(options);
            var duplicateException = await Assert.ThrowsAsync<SqlException>(() =>
                duplicateMapping.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [CurriculumOutcomeKnowledgeComponents]
                        ([CurriculumOutcomeId], [KnowledgeComponentId])
                    VALUES ({{outcome.Id}}, {{leaf.Id}});
                    """));
            Assert.Contains(duplicateException.Number, DuplicateKeyErrorNumbers);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task CurriculumMappingMutationsFollowKnowledgeModelLifecycle()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            Guid modelId;
            Guid outcomeId;
            Guid primaryComponentId;
            Guid secondaryComponentId;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                var curriculum = new Curriculum(
                    Guid.NewGuid(),
                    "MAT-7",
                    "Mathematics",
                    "2026");
                var outcome = new CurriculumOutcome(
                    Guid.NewGuid(),
                    curriculum,
                    "Outcome",
                    0);
                var model = CreateModel("PLUS5_CORE", "V1");
                var area = CreateArea(model, "Area", 0);
                var primaryComponent = CreateComponent(model, area, "Primary", 0);
                var secondaryComponent = CreateComponent(model, area, "Secondary", 1);
                modelId = model.Id;
                outcomeId = outcome.Id;
                primaryComponentId = primaryComponent.Id;
                secondaryComponentId = secondaryComponent.Id;
                seed.AddRange(
                    curriculum,
                    outcome,
                    model,
                    area,
                    primaryComponent,
                    secondaryComponent);
                await seed.SaveChangesAsync();
            }

            await using (var draft = new Plus5DbContext(options))
            {
                await draft.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [CurriculumOutcomeKnowledgeComponents]
                        ([CurriculumOutcomeId], [KnowledgeComponentId])
                    VALUES ({{outcomeId}}, {{primaryComponentId}});
                    """);
                Assert.True(await draft.CurriculumOutcomeKnowledgeComponents.AsNoTracking()
                    .AnyAsync(mapping => mapping.CurriculumOutcomeId == outcomeId
                        && mapping.KnowledgeComponentId == primaryComponentId));

                await draft.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [CurriculumOutcomeKnowledgeComponents]
                    WHERE [CurriculumOutcomeId] = {{outcomeId}}
                        AND [KnowledgeComponentId] = {{primaryComponentId}};
                    """);
                Assert.False(await draft.CurriculumOutcomeKnowledgeComponents.AsNoTracking()
                    .AnyAsync(mapping => mapping.CurriculumOutcomeId == outcomeId
                        && mapping.KnowledgeComponentId == primaryComponentId));

                await draft.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [CurriculumOutcomeKnowledgeComponents]
                        ([CurriculumOutcomeId], [KnowledgeComponentId])
                    VALUES ({{outcomeId}}, {{primaryComponentId}});
                    """);
            }

            await using (var publish = new Plus5DbContext(options))
            {
                var model = await publish.KnowledgeModels.SingleAsync(item => item.Id == modelId);
                model.Publish();
                await publish.SaveChangesAsync();
            }

            await using (var publishedAdd = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    publishedAdd.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [CurriculumOutcomeKnowledgeComponents]
                            ([CurriculumOutcomeId], [KnowledgeComponentId])
                        VALUES ({{outcomeId}}, {{secondaryComponentId}});
                        """));
                Assert.Equal(51107, exception.Number);
            }

            await using (var publishedRemove = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    publishedRemove.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [CurriculumOutcomeKnowledgeComponents]
                        WHERE [CurriculumOutcomeId] = {{outcomeId}}
                            AND [KnowledgeComponentId] = {{primaryComponentId}};
                        """));
                Assert.Equal(51107, exception.Number);
            }

            await using (var publishedUpdate = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    publishedUpdate.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [CurriculumOutcomeKnowledgeComponents]
                        SET [KnowledgeComponentId] = {{secondaryComponentId}}
                        WHERE [CurriculumOutcomeId] = {{outcomeId}}
                            AND [KnowledgeComponentId] = {{primaryComponentId}};
                        """));
                Assert.Equal(51107, exception.Number);
            }

            await using (var retire = new Plus5DbContext(options))
            {
                var model = await retire.KnowledgeModels.SingleAsync(item => item.Id == modelId);
                model.Retire();
                await retire.SaveChangesAsync();
            }

            await using (var retiredMutation = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    retiredMutation.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [CurriculumOutcomeKnowledgeComponents]
                        WHERE [CurriculumOutcomeId] = {{outcomeId}}
                            AND [KnowledgeComponentId] = {{primaryComponentId}};
                        """));
                Assert.Equal(51107, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task CompositeForeignKeysRejectCrossModelAndCrossAreaParents()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            Guid modelId;
            Guid areaId;
            Guid otherAreaId;
            Guid parentId;
            Guid otherModelId;
            Guid otherModelAreaId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var model = CreateModel("PLUS5_CORE", "V1");
                var otherModel = CreateModel("OTHER", "V1");
                var area = CreateArea(model, "Area", 0);
                var otherArea = CreateArea(model, "Other area", 1);
                var otherModelArea = CreateArea(otherModel, "Area", 0);
                var parent = CreateComponent(model, area, "Parent", 0);
                modelId = model.Id;
                areaId = area.Id;
                otherAreaId = otherArea.Id;
                parentId = parent.Id;
                otherModelId = otherModel.Id;
                otherModelAreaId = otherModelArea.Id;
                db.AddRange(model, otherModel, area, otherArea, otherModelArea, parent);
                await db.SaveChangesAsync();
            }

            await using (var crossArea = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    crossArea.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [KnowledgeComponents]
                            ([Id], [KnowledgeModelId], [KnowledgeAreaId], [ParentComponentId],
                             [SupersedesKnowledgeComponentId], [Name], [Description], [SortOrder], [Status])
                        VALUES
                            ({{Guid.NewGuid()}}, {{modelId}}, {{otherAreaId}}, {{parentId}},
                             NULL, N'Invalid area child', NULL, 0, 1);
                        """));
                Assert.Equal(547, exception.Number);
            }

            await using (var crossModel = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    crossModel.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [KnowledgeComponents]
                            ([Id], [KnowledgeModelId], [KnowledgeAreaId], [ParentComponentId],
                             [SupersedesKnowledgeComponentId], [Name], [Description], [SortOrder], [Status])
                        VALUES
                            ({{Guid.NewGuid()}}, {{otherModelId}}, {{otherModelAreaId}}, {{parentId}},
                             NULL, N'Invalid model child', NULL, 0, 1);
                        """));
                Assert.Equal(547, exception.Number);
            }

            Assert.NotEqual(areaId, otherAreaId);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlTriggerRejectsArbitraryHierarchyCycle()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            var model = CreateModel("PLUS5_CORE", "V1");
            var area = CreateArea(model, "Area", 0);
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                db.AddRange(model, area);
                await db.SaveChangesAsync();
            }

            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            await using var cycle = new Plus5DbContext(options);
            var exception = await Assert.ThrowsAsync<SqlException>(() =>
                cycle.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [KnowledgeComponents]
                        ([Id], [KnowledgeModelId], [KnowledgeAreaId], [ParentComponentId],
                         [SupersedesKnowledgeComponentId], [Name], [Description], [SortOrder], [Status])
                    VALUES
                        ({{firstId}}, {{model.Id}}, {{area.Id}}, {{secondId}},
                         NULL, N'First', NULL, 0, 1),
                        ({{secondId}}, {{model.Id}}, {{area.Id}}, {{firstId}},
                         NULL, N'Second', NULL, 1, 1);
                    """));
            Assert.Equal(51106, exception.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlTriggerRejectsSameVersionAndCrossFamilyLineage()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            Guid currentId;
            Guid sameVersionId;
            Guid otherFamilyId;
            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                var currentModel = CreateModel("PLUS5_CORE", "V2");
                var otherModel = CreateModel("OTHER", "V1");
                var currentArea = CreateArea(currentModel, "Area", 0);
                var otherArea = CreateArea(otherModel, "Area", 0);
                var current = CreateComponent(currentModel, currentArea, "Current", 0);
                var sameVersion = CreateComponent(currentModel, currentArea, "Same version", 1);
                var otherFamily = CreateComponent(otherModel, otherArea, "Other family", 0);
                currentId = current.Id;
                sameVersionId = sameVersion.Id;
                otherFamilyId = otherFamily.Id;
                db.AddRange(
                    currentModel,
                    otherModel,
                    currentArea,
                    otherArea,
                    current,
                    sameVersion,
                    otherFamily);
                await db.SaveChangesAsync();
            }

            await using (var sameVersion = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    sameVersion.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [KnowledgeComponents]
                        SET [SupersedesKnowledgeComponentId] = {{sameVersionId}}
                        WHERE [Id] = {{currentId}};
                        """));
                Assert.Equal(51105, exception.Number);
            }

            await using (var crossFamily = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    crossFamily.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [KnowledgeComponents]
                        SET [SupersedesKnowledgeComponentId] = {{otherFamilyId}}
                        WHERE [Id] = {{currentId}};
                        """));
                Assert.Equal(51105, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task PublishedStructureAndModelHistoryCannotBeMutatedOrDeleted()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            var model = CreateModel("PLUS5_CORE", "V1");
            var area = CreateArea(model, "Area", 0);
            var component = CreateComponent(model, area, "Component", 0);
            var emptyPublishedModel = CreateModel("EMPTY", "V1");
            model.Publish();
            emptyPublishedModel.Publish();

            await using (var db = new Plus5DbContext(options))
            {
                await db.Database.MigrateAsync();
                db.AddRange(model, area, component, emptyPublishedModel);
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }

            await using (var seed = new Plus5DbContext(options))
            {
                var draftModel = CreateModel("PLUS5_CORE", "V1");
                var draftArea = CreateArea(draftModel, "Area", 0);
                var draftComponent = CreateComponent(draftModel, draftArea, "Component", 0);
                var removable = CreateModel("EMPTY", "V1");
                seed.AddRange(draftModel, draftArea, draftComponent, removable);
                await seed.SaveChangesAsync();
                draftModel.Publish();
                removable.Publish();
                await seed.SaveChangesAsync();
                model = draftModel;
                component = draftComponent;
                emptyPublishedModel = removable;
            }

            await using (var immutableComponent = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    immutableComponent.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [KnowledgeComponents]
                        SET [Name] = N'Changed'
                        WHERE [Id] = {{component.Id}};
                        """));
                Assert.Equal(51104, exception.Number);
            }

            await using (var hardDeleteComponent = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    hardDeleteComponent.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [KnowledgeComponents]
                        WHERE [Id] = {{component.Id}};
                        """));
                Assert.Equal(51104, exception.Number);
            }

            await using (var hardDelete = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    hardDelete.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [KnowledgeModels]
                        WHERE [Id] = {{emptyPublishedModel.Id}};
                        """));
                Assert.Equal(51100, exception.Number);
            }

            await using (var invalidTransition = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    invalidTransition.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [KnowledgeModels]
                        SET [Status] = 1
                        WHERE [Id] = {{model.Id}};
                        """));
                Assert.Equal(51101, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static KnowledgeModel CreateModel(string code, string version) =>
        new(Guid.NewGuid(), code, version);

    private static KnowledgeArea CreateArea(
        KnowledgeModel model,
        string name,
        int sortOrder) =>
        new(Guid.NewGuid(), model, name, sortOrder);

    private static KnowledgeComponent CreateComponent(
        KnowledgeModel model,
        KnowledgeArea area,
        string name,
        int sortOrder,
        KnowledgeComponent? parent = null,
        KnowledgeComponent? supersedes = null,
        KnowledgeModel? supersededModel = null) =>
        new(
            Guid.NewGuid(),
            model,
            area,
            name,
            sortOrder,
            parent,
            supersedes,
            supersededModel);

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase52Test_" + Guid.NewGuid().ToString("N");
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
