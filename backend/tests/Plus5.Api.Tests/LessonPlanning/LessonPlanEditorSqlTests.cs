using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonPlanEditorSqlTests
{
    private const string PreviousMigration = "20261031204323_AddLessonStructureTimeline";
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationAndAttachmentIntegrityTriggersAreEnforced()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var teacher = new UserAccount(Guid.NewGuid(), "phase96@plus5.local", "PHASE96@PLUS5.LOCAL", Now);
            var other = new UserAccount(Guid.NewGuid(), "foreign96@plus5.local", "FOREIGN96@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-96", "8. razred", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "Grammar", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            var model = new KnowledgeModel(Guid.NewGuid(), "ENG-G8-96", "2026");
            var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
            var component = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect", 0);
            db.AddRange(teacher, other, grade, program, student, model, area, component);
            await db.SaveChangesAsync();
            model.Publish();
            await db.SaveChangesAsync();

            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Razlikovati vremena.", Now);
            db.AddRange(plan, version,
                new LessonPlanVersionKnowledgeComponent(version.Id, component.Id));
            await db.SaveChangesAsync();

            var structure = new EfLessonStructureService(db, new FixedTimeProvider());
            var applied = await structure.ApplyBaselineAsync(teacher.Id, plan.Id,
                Convert.ToBase64String(version.RowVersion), CancellationToken.None);
            Assert.Equal(LessonStructureFailure.None, applied.Failure);
            var activityId = applied.View!.Activities[0].Id;

            var ownerMaterial = await AddActiveMaterialAsync(db, teacher.Id, "Owner material");
            var foreignMaterial = await AddActiveMaterialAsync(db, other.Id, "Private foreign material");

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [LessonActivityMaterialVersions] ([LessonActivityId], [MaterialVersionId])
                VALUES ({{activityId}}, {{ownerMaterial.Version.Id}});
                """);
            Assert.True(await db.LessonActivityMaterialVersions.AsNoTracking().AnyAsync(item =>
                item.LessonActivityId == activityId && item.MaterialVersionId == ownerMaterial.Version.Id));

            var inaccessible = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [LessonActivityMaterialVersions] ([LessonActivityId], [MaterialVersionId])
                    VALUES ({{activityId}}, {{foreignMaterial.Version.Id}});
                    """));
            Assert.Equal(51321, inaccessible.Number);

            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonPlanVersions] SET [Status] = 2 WHERE [Id] = {{version.Id}};
                """);
            var immutableDelete = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    DELETE FROM [LessonActivityMaterialVersions]
                    WHERE [LessonActivityId] = {{activityId}} AND [MaterialVersionId] = {{ownerMaterial.Version.Id}};
                    """));
            Assert.Equal(51320, immutableDelete.Number);

            var secondOwnerMaterial = await AddActiveMaterialAsync(db, teacher.Id, "Second owner material");
            var immutableInsert = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [LessonActivityMaterialVersions] ([LessonActivityId], [MaterialVersionId])
                    VALUES ({{activityId}}, {{secondOwnerMaterial.Version.Id}});
                    """));
            Assert.Equal(51320, immutableInsert.Number);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    private static async Task<MaterialFixture> AddActiveMaterialAsync(
        Plus5DbContext db,
        Guid teacherId,
        string title)
    {
        var material = new Material(Guid.NewGuid(), teacherId, Now);
        var version = new MaterialVersion(Guid.NewGuid(), material, 1, title, "WORKSHEET", Now);
        var file = new MaterialFile(Guid.NewGuid(), material, version, MaterialFileFormat.Pdf,
            "material.pdf", "application/pdf", 100, "R2", "quarantine", Now);
        db.AddRange(material, version, file);
        await db.SaveChangesAsync();
        file.MarkUploaded(100, new string('B', 64), Now.AddMinutes(1));
        await db.SaveChangesAsync();
        file.StartScanning(Now.AddMinutes(2));
        await db.SaveChangesAsync();
        file.MarkClean(Now.AddMinutes(3));
        await db.SaveChangesAsync();
        version.Activate(file, Now.AddMinutes(4));
        await db.SaveChangesAsync();
        return new(material, version);
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase96Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        return (new DbContextOptionsBuilder<Plus5DbContext>().UseSqlServer(connection.ConnectionString).Options, databaseName);
    }

    private static async Task DeleteDatabase(DbContextOptions<Plus5DbContext> options, string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }

    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => Now.AddMinutes(20); }
    private sealed record MaterialFixture(Material Material, MaterialVersion Version);
    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
                Skip = "Local disposable SQL credentials required.";
        }
    }
}
