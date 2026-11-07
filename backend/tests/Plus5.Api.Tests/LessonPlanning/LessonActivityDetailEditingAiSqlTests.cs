using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonActivityDetailEditingAiSqlTests
{
    private const string PreviousMigration = "20261103175217_AddLessonMaterialAttachments";
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationBackfillsFieldsAndTriggerRejectsInvalidSuggestionTarget()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var teacher = new UserAccount(Guid.NewGuid(), "phase98@plus5.local", "PHASE98@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-98", "8. razred", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "English", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Primijeniti vrijeme.", Now);
            db.AddRange(teacher, grade, program, student, plan, version);
            await db.SaveChangesAsync();
            var activityId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivities]
                    ([Id], [LessonPlanVersionId], [KnowledgeBlockVersionId], [SortOrder], [Name], [Purpose], [DurationMinutes])
                VALUES ({{activityId}}, {{version.Id}}, NULL, 0, N'Mini dijalog', 4, 15);
                """);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            var migrated = await db.LessonActivities.AsNoTracking().SingleAsync(item => item.Id == activityId);
            Assert.Equal(LessonActivityWorkMode.Individual, migrated.WorkMode);
            Assert.True(migrated.ShowTeacherInstructionsOnBoard);
            Assert.True(migrated.ShowStudentInstructionsOnBoard);

            var suggestion = new LessonActivityAiSuggestion(Guid.NewGuid(), teacher.Id, plan.Id,
                version.Id, activityId, version.RowVersion, "OPENAI", "gpt-6.1-sol", "v1",
                "{\"name\":\"Prijedlog\",\"purpose\":null,\"description\":null,\"teacherInstructions\":null,\"studentInstructions\":null,\"workMode\":\"Pair\"}",
                Now.AddMinutes(1), Now.AddHours(24));
            db.LessonActivityAiSuggestions.Add(suggestion);
            await db.SaveChangesAsync();

            var invalid = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivityAiSuggestions]
                    ([Id],[TeacherAccountId],[LessonPlanId],[LessonPlanVersionId],[LessonActivityId],[BasedOnRowVersion],[Status],[Provider],[Model],[PromptTemplateVersion],[ContentJson],[GeneratedAtUtc],[ExpiresAtUtc],[ResolvedAtUtc],[ReplacesSuggestionId])
                VALUES ({{Guid.NewGuid()}},{{teacher.Id}},{{plan.Id}},{{version.Id}},{{Guid.NewGuid()}},{{version.RowVersion}},1,N'OPENAI',N'gpt-6.1-sol',N'v1',N'{"name":"X"}',{{Now.AddMinutes(2)}},{{Now.AddHours(24)}},NULL,NULL);
                """));
            Assert.Equal(51340, invalid.Number);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var name = "Plus5_Phase98Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        return (new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options, name);
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
