using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialMetadataMappingSqlTests
{
    private const string PreviousMigration = "20260929143813_AddMaterialStorageFoundation";
    private const int SnapshotMutationViolation = 51224;
    private const int DraftKnowledgeModelViolation = 51225;
    private const int MaterialVersionLifecycleViolation = 51221;
    private const int ReferentialIntegrityViolation = 547;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 29, 18, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradePreservesMaterialRowsAndStartsWithEmptyMappings()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var owner = CreateAccount("phase62-upgrade@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material);
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [UserAccounts]
                    ([Id], [Email], [NormalizedEmail], [PasswordHash], [Status],
                     [SecurityStamp], [CreatedAtUtc], [UpdatedAtUtc])
                VALUES
                    ({{owner.Id}}, {{owner.Email}}, {{owner.NormalizedEmail}}, N'NOT_A_LOGIN_HASH', 1,
                     {{Guid.NewGuid()}}, {{CreatedAt}}, {{CreatedAt}});

                INSERT INTO [Materials]
                    ([Id], [OwnerTeacherId], [Status], [Visibility], [CurrentVersionId],
                     [CreatedAtUtc], [UpdatedAtUtc], [ArchivedAtUtc])
                VALUES
                    ({{material.Id}}, {{owner.Id}}, 1, 1, NULL,
                     {{CreatedAt}}, {{CreatedAt}}, NULL);

                INSERT INTO [MaterialVersions]
                    ([Id], [MaterialId], [VersionNumber], [CreatedByTeacherId], [Title],
                     [Description], [MaterialTypeCode], [Subject], [LanguageCode], [Status],
                     [CreatedAtUtc], [ActivatedAtUtc], [SupersededAtUtc])
                VALUES
                    ({{version.Id}}, {{material.Id}}, 1, {{owner.Id}}, N'Present Perfect worksheet',
                     N'Practice material', N'WORKSHEET', N'English', N'en', 1,
                     {{CreatedAt}}, NULL, NULL);
                """);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            var stored = await db.MaterialVersions.AsNoTracking()
                .SingleAsync(item => item.Id == version.Id);
            Assert.Null(stored.ProficiencyLevelId);
            Assert.Empty(await db.MaterialVersionCurriculumOutcomes.AsNoTracking().ToListAsync());
            Assert.Empty(await db.MaterialVersionKnowledgeComponents.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task DraftMappingsAreMutableButPublishedSnapshotRejectsEveryMutation()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase62-lifecycle@plus5.local");
            var firstLevel = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", 3);
            var secondLevel = new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B2", "B2", 4);
            var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var program = new Plus5.Domain.Teaching.Program(
                Guid.NewGuid(), owner.Id, "Grammar Focus", CreatedAt);
            var version = CreateVersion(
                material,
                program,
                grade,
                firstLevel,
                "Use Present Perfect.");
            var file = CreateCleanFile(material, version);
            var curriculum = new Curriculum(Guid.NewGuid(), "HR-EJ", "English", "2026");
            var firstOutcome = new CurriculumOutcome(
                Guid.NewGuid(), curriculum, "First outcome", 0);
            var secondOutcome = new CurriculumOutcome(
                Guid.NewGuid(), curriculum, "Second outcome", 1);
            var model = new KnowledgeModel(Guid.NewGuid(), "PLUS5_CORE", "V1");
            var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
            var firstComponent = new KnowledgeComponent(
                Guid.NewGuid(), model, area, "Present Perfect", 0);
            var secondComponent = new KnowledgeComponent(
                Guid.NewGuid(), model, area, "Past Simple", 1);
            db.AddRange(
                owner,
                firstLevel,
                secondLevel,
                grade,
                program,
                material,
                version,
                file,
                curriculum,
                firstOutcome,
                secondOutcome,
                model,
                area,
                firstComponent,
                secondComponent);
            await db.SaveChangesAsync();
            model.Publish();
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [MaterialVersionCurriculumOutcomes]
                    ([MaterialVersionId], [CurriculumOutcomeId])
                VALUES ({{version.Id}}, {{firstOutcome.Id}});
                DELETE FROM [MaterialVersionCurriculumOutcomes]
                WHERE [MaterialVersionId] = {{version.Id}}
                  AND [CurriculumOutcomeId] = {{firstOutcome.Id}};
                INSERT INTO [MaterialVersionCurriculumOutcomes]
                    ([MaterialVersionId], [CurriculumOutcomeId])
                VALUES ({{version.Id}}, {{firstOutcome.Id}});

                INSERT INTO [MaterialVersionKnowledgeComponents]
                    ([MaterialVersionId], [KnowledgeComponentId])
                VALUES ({{version.Id}}, {{firstComponent.Id}});
                DELETE FROM [MaterialVersionKnowledgeComponents]
                WHERE [MaterialVersionId] = {{version.Id}}
                  AND [KnowledgeComponentId] = {{firstComponent.Id}};
                INSERT INTO [MaterialVersionKnowledgeComponents]
                    ([MaterialVersionId], [KnowledgeComponentId])
                VALUES ({{version.Id}}, {{firstComponent.Id}});

                UPDATE [MaterialVersions]
                SET [ProficiencyLevelId] = {{secondLevel.Id}}
                WHERE [Id] = {{version.Id}};

                INSERT INTO [MaterialVersionTags]
                    ([MaterialVersionId], [NormalizedName], [Name])
                VALUES ({{version.Id}}, N'PRESENT PERFECT', N'present perfect');
                DELETE FROM [MaterialVersionTags]
                WHERE [MaterialVersionId] = {{version.Id}}
                  AND [NormalizedName] = N'PRESENT PERFECT';
                INSERT INTO [MaterialVersionTags]
                    ([MaterialVersionId], [NormalizedName], [Name])
                VALUES ({{version.Id}}, N'PRESENT PERFECT', N'present perfect');
                """);

            version = await db.MaterialVersions.SingleAsync(item => item.Id == version.Id);
            file = await db.MaterialFiles.SingleAsync(item => item.Id == file.Id);
            version.Activate(file, CreatedAt.AddMinutes(4));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var addOutcome = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialVersionCurriculumOutcomes]
                        ([MaterialVersionId], [CurriculumOutcomeId])
                    VALUES ({{version.Id}}, {{secondOutcome.Id}});
                    """));
            Assert.Equal(SnapshotMutationViolation, addOutcome.Number);

            var removeOutcome = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [MaterialVersionCurriculumOutcomes]
                    WHERE [MaterialVersionId] = {{version.Id}}
                      AND [CurriculumOutcomeId] = {{firstOutcome.Id}};
                    """));
            Assert.Equal(SnapshotMutationViolation, removeOutcome.Number);

            var updateOutcome = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersionCurriculumOutcomes]
                    SET [CurriculumOutcomeId] = {{secondOutcome.Id}}
                    WHERE [MaterialVersionId] = {{version.Id}}
                      AND [CurriculumOutcomeId] = {{firstOutcome.Id}};
                    """));
            Assert.Equal(SnapshotMutationViolation, updateOutcome.Number);

            var addComponent = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialVersionKnowledgeComponents]
                        ([MaterialVersionId], [KnowledgeComponentId])
                    VALUES ({{version.Id}}, {{secondComponent.Id}});
                    """));
            Assert.Equal(SnapshotMutationViolation, addComponent.Number);

            var removeComponent = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [MaterialVersionKnowledgeComponents]
                    WHERE [MaterialVersionId] = {{version.Id}}
                      AND [KnowledgeComponentId] = {{firstComponent.Id}};
                    """));
            Assert.Equal(SnapshotMutationViolation, removeComponent.Number);

            var changeLevel = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersions]
                    SET [ProficiencyLevelId] = {{firstLevel.Id}}
                    WHERE [Id] = {{version.Id}};
                    """));
            Assert.Equal(MaterialVersionLifecycleViolation, changeLevel.Number);

            var addTag = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialVersionTags]
                        ([MaterialVersionId], [NormalizedName], [Name])
                    VALUES ({{version.Id}}, N'GRAMMAR', N'grammar');
                    """));
            Assert.Equal(SnapshotMutationViolation, addTag.Number);

            var updateTag = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersionTags]
                    SET [Name] = N'changed'
                    WHERE [MaterialVersionId] = {{version.Id}}
                      AND [NormalizedName] = N'PRESENT PERFECT';
                    """));
            Assert.Equal(SnapshotMutationViolation, updateTag.Number);

            var removeTag = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [MaterialVersionTags]
                    WHERE [MaterialVersionId] = {{version.Id}}
                      AND [NormalizedName] = N'PRESENT PERFECT';
                    """));
            Assert.Equal(SnapshotMutationViolation, removeTag.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task KnowledgeMappingRejectsDraftModelAndAcceptsPublishedOrRetiredVersion()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase62-model@plus5.local");
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material);
            var draftModel = new KnowledgeModel(Guid.NewGuid(), "DRAFT_MODEL", "V1");
            var draftArea = new KnowledgeArea(Guid.NewGuid(), draftModel, "Draft area", 0);
            var draftComponent = new KnowledgeComponent(
                Guid.NewGuid(), draftModel, draftArea, "Draft component", 0);
            var stableModel = new KnowledgeModel(Guid.NewGuid(), "STABLE_MODEL", "V1");
            var stableArea = new KnowledgeArea(Guid.NewGuid(), stableModel, "Stable area", 0);
            var publishedComponent = new KnowledgeComponent(
                Guid.NewGuid(), stableModel, stableArea, "Published target", 0);
            var retiredComponent = new KnowledgeComponent(
                Guid.NewGuid(), stableModel, stableArea, "Historical target", 1);
            db.AddRange(
                owner,
                material,
                version,
                draftModel,
                draftArea,
                draftComponent,
                stableModel,
                stableArea,
                publishedComponent,
                retiredComponent);
            await db.SaveChangesAsync();
            stableModel.Publish();
            await db.SaveChangesAsync();

            var draftTarget = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [MaterialVersionKnowledgeComponents]
                        ([MaterialVersionId], [KnowledgeComponentId])
                    VALUES ({{version.Id}}, {{draftComponent.Id}});
                    """));
            Assert.Equal(DraftKnowledgeModelViolation, draftTarget.Number);

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [MaterialVersionKnowledgeComponents]
                    ([MaterialVersionId], [KnowledgeComponentId])
                VALUES ({{version.Id}}, {{publishedComponent.Id}});
                """);

            stableModel = await db.KnowledgeModels.SingleAsync(item => item.Id == stableModel.Id);
            stableModel.Retire();
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [MaterialVersionKnowledgeComponents]
                    ([MaterialVersionId], [KnowledgeComponentId])
                VALUES ({{version.Id}}, {{retiredComponent.Id}});
                """);
            Assert.Equal(
                2,
                await db.MaterialVersionKnowledgeComponents.AsNoTracking()
                    .CountAsync(mapping => mapping.MaterialVersionId == version.Id));
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task ProgramMappingCannotCrossTeacherOwnership()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase62-owner@plus5.local");
            var foreignOwner = CreateAccount("phase62-foreign@plus5.local");
            var ownProgram = new Plus5.Domain.Teaching.Program(
                Guid.NewGuid(), owner.Id, "Grammar Focus", CreatedAt);
            var foreignProgram = new Plus5.Domain.Teaching.Program(
                Guid.NewGuid(), foreignOwner.Id, "Foreign Program", CreatedAt);
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = CreateVersion(material, ownProgram);
            db.AddRange(
                owner,
                foreignOwner,
                ownProgram,
                foreignProgram,
                material,
                version);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MaterialVersions]
                    SET [ProgramId] = {{foreignProgram.Id}}
                    WHERE [Id] = {{version.Id}};
                    """));
            Assert.Equal(ReferentialIntegrityViolation, exception.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static UserAccount CreateAccount(string email) =>
        new(Guid.NewGuid(), email, email.ToUpperInvariant(), CreatedAt);

    private static MaterialVersion CreateVersion(
        Material material,
        Plus5.Domain.Teaching.Program? program = null,
        SchoolGrade? grade = null,
        ProficiencyLevel? level = null,
        string? learningGoal = null) =>
        new(
            Guid.NewGuid(),
            material,
            1,
            "Present Perfect worksheet",
            "WORKSHEET",
            CreatedAt,
            "Practice material",
            "English",
            "en",
            program,
            grade,
            level,
            learningGoal);

    private static MaterialFile CreateCleanFile(Material material, MaterialVersion version)
    {
        var fileId = Guid.NewGuid();
        var file = new MaterialFile(
            fileId,
            material,
            version,
            MaterialFileFormat.Pdf,
            "worksheet.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt);
        file.MarkUploaded(100, new string('A', 64), CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        return file;
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase62Test_" + Guid.NewGuid().ToString("N");
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
