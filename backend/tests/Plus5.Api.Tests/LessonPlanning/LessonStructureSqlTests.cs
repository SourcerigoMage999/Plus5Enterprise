using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonStructureSqlTests
{
    private const string PreviousMigration = "20261031093106_AddLessonGoalSelection";
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationBackfillCatalogApplyAndTriggersAreEnforced()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);

            var teacher = new UserAccount(Guid.NewGuid(), "phase95@plus5.local", "PHASE95@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8", "8. razred", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "Grammar", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            var model = new KnowledgeModel(Guid.NewGuid(), "ENG-G8", "2026");
            var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
            var mapped = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect", 0);
            var unmapped = new KnowledgeComponent(Guid.NewGuid(), model, area, "Past Simple", 1);
            db.AddRange(teacher, grade, program, student, model, area, mapped, unmapped);
            await db.SaveChangesAsync();
            model.Publish();
            await db.SaveChangesAsync();

            var planId = Guid.NewGuid();
            var versionId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonPlans] ([Id], [TeacherAccountId], [StudentId], [GroupId], [SessionId],
                    [LifecycleStatus], [CurrentReadyVersionId], [CreatedAtUtc], [ArchivedAtUtc])
                VALUES ({{planId}}, {{teacher.Id}}, {{student.Id}}, NULL, NULL, 1, NULL, {{Now}}, NULL);
                INSERT INTO [LessonPlanVersions] ([Id], [LessonPlanId], [VersionNumber], [Status], [Goal], [CreatedAtUtc])
                VALUES ({{versionId}}, {{planId}}, 1, 1, N'Razlikovati vremena.', {{Now}});
                INSERT INTO [LessonPlanVersionKnowledgeComponents] ([LessonPlanVersionId], [KnowledgeComponentId])
                VALUES ({{versionId}}, {{mapped.Id}});
                """);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            Assert.Equal(25, await db.KnowledgeBlocks.CountAsync());
            Assert.Equal(5, await db.KnowledgeBlockVersions.CountAsync(item => item.Status == KnowledgeBlockVersionStatus.Published));
            Assert.Equal(20, await db.KnowledgeBlockVersions.CountAsync(item => item.Status == KnowledgeBlockVersionStatus.Draft));
            var upgraded = await db.LessonPlanVersions.AsNoTracking().SingleAsync(item => item.Id == versionId);
            Assert.Equal(60, upgraded.TargetDurationMinutes);
            Assert.Equal(upgraded.CreatedAtUtc, upgraded.UpdatedAtUtc);

            var service = new EfLessonStructureService(db, new FixedTimeProvider());
            var applied = await service.ApplyBaselineAsync(teacher.Id, planId,
                Convert.ToBase64String(upgraded.RowVersion), CancellationToken.None);
            Assert.Equal(LessonStructureFailure.None, applied.Failure);
            Assert.Equal(5, await db.LessonActivities.CountAsync(item => item.LessonPlanVersionId == versionId));
            Assert.Equal(4, await db.LessonActivityKnowledgeComponents.CountAsync());

            var draftKb = await (from block in db.KnowledgeBlocks
                                 join version in db.KnowledgeBlockVersions
                                 on block.Id equals version.KnowledgeBlockId
                                 where block.Code == "KB-006"
                                 select version.Id).SingleAsync();
            var draftReference = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivities] ([Id], [LessonPlanVersionId], [KnowledgeBlockVersionId], [SortOrder], [Name], [Purpose], [DurationMinutes])
                VALUES ({{Guid.NewGuid()}}, {{versionId}}, {{draftKb}}, 5, N'Invalid', 6, 5);
                """));
            Assert.Equal(51311, draftReference.Number);

            var publishedKb = await (from block in db.KnowledgeBlocks
                                     join version in db.KnowledgeBlockVersions
                                     on block.Id equals version.KnowledgeBlockId
                                     where block.Code == "KB-001"
                                     select version.Id).SingleAsync();
            var immutable = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [KnowledgeBlockVersions] SET [DisplayName] = N'Changed' WHERE [Id] = {{publishedKb}};
                """));
            Assert.Equal(51310, immutable.Number);

            var customActivity = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivities] ([Id], [LessonPlanVersionId], [KnowledgeBlockVersionId], [SortOrder], [Name], [Purpose], [DurationMinutes])
                VALUES ({{customActivity}}, {{versionId}}, NULL, 5, N'Custom', 6, 5);
                """);
            var outsidePlan = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivityKnowledgeComponents] ([LessonActivityId], [KnowledgeComponentId])
                VALUES ({{customActivity}}, {{unmapped.Id}});
                """));
            Assert.Equal(51312, outsidePlan.Number);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase95Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        return (new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options, databaseName);
    }
    private static async Task DeleteDatabase(DbContextOptions<Plus5DbContext> options, string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }
    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => Now.AddMinutes(1); }
    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
                Skip = "Local disposable SQL credentials required.";
        }
    }
}
