using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonGoalSelectionSqlTests
{
    private const string PreviousMigration = "20261024122849_AddPresentationAiAssistance";
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 9, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationAndDatabaseProtectContextDraftAndKnowledgeMapping()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var teacher = new UserAccount(Guid.NewGuid(), "phase94@plus5.local", "PHASE94@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8", "8. razred", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "Grammar Focus", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            var group = new Group(Guid.NewGuid(), teacher.Id, program.Id, grade.Id, "Grammar 8A", 8,
                GroupStatus.Active, Now);
            var session = new Session(Guid.NewGuid(), teacher.Id, DeliveryMode.Individual, student.Id,
                Now.AddDays(1), Now.AddDays(1).AddMinutes(45), "Europe/Zagreb", Now);
            var model = new KnowledgeModel(Guid.NewGuid(), "ENG-G8", "2026");
            var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
            var component = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect — use", 0);
            db.AddRange(teacher, grade, program, student, group, session, model, area, component);
            await db.SaveChangesAsync();
            model.Publish();
            await db.SaveChangesAsync();

            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, session.Id, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Razlikovati vremena.", Now);
            db.AddRange(plan, version, new LessonPlanVersionKnowledgeComponent(version.Id, component.Id));
            await db.SaveChangesAsync();

            var wrongContext = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [LessonPlans]
                    ([Id], [TeacherAccountId], [StudentId], [GroupId], [SessionId],
                     [LifecycleStatus], [CurrentReadyVersionId], [CreatedAtUtc], [ArchivedAtUtc])
                    VALUES ({{Guid.NewGuid()}}, {{teacher.Id}}, NULL, {{group.Id}}, {{session.Id}},
                            1, NULL, {{Now}}, NULL);
                    """));
            Assert.Equal(51301, wrongContext.Number);

            var duplicateDraft = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [LessonPlanVersions]
                    ([Id], [LessonPlanId], [VersionNumber], [Status], [Goal], [CreatedAtUtc])
                    VALUES ({{Guid.NewGuid()}}, {{plan.Id}}, 2, 1, N'Drugi draft', {{Now}});
                    """));
            Assert.True(duplicateDraft.Number is 2601 or 2627);

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonPlanVersions] SET [Status] = 2 WHERE [Id] = {{version.Id}};
                """);
            var immutableMapping = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [LessonPlanVersionKnowledgeComponents]
                    WHERE [LessonPlanVersionId] = {{version.Id}} AND [KnowledgeComponentId] = {{component.Id}};
                    """));
            Assert.Equal(51304, immutableMapping.Number);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase94Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        return (new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options, databaseName);
    }

    private static async Task DeleteDatabase(DbContextOptions<Plus5DbContext> options, string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
                Skip = "Local disposable SQL credentials required.";
        }
    }
}
