using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonPlanConfirmationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DraftReadySupersededLifecycleIsOneWay()
    {
        var version = new LessonPlanVersion(Guid.NewGuid(), Guid.NewGuid(), 1, "Cilj sata", Now);
        version.MarkReady();
        Assert.Equal(LessonPlanVersionStatus.Ready, version.Status);
        Assert.Throws<InvalidOperationException>(version.MarkReady);
        version.MarkSuperseded();
        Assert.Equal(LessonPlanVersionStatus.Superseded, version.Status);
        Assert.Throws<InvalidOperationException>(version.MarkSuperseded);
    }

    [Fact]
    public void ActivePlanAcceptsOnlyNonEmptyReadyPointer()
    {
        var plan = new LessonPlan(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, Now);
        var readyId = Guid.NewGuid();
        plan.SetCurrentReadyVersion(readyId);
        Assert.Equal(readyId, plan.CurrentReadyVersionId);
        Assert.Throws<ArgumentException>(() => plan.SetCurrentReadyVersion(Guid.Empty));
    }

    [LocalSqlFact]
    public async Task ConfirmAndEditUseAtomicReadyPointerAndImmutableSnapshots()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var teacher = new UserAccount(Guid.NewGuid(), "phase99@plus5.local", "PHASE99@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-99", "8. razred", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "English", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            db.AddRange(teacher, grade, program, student);
            await db.SaveChangesAsync();

            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Primijeniti ciljano znanje.", Now, 45);
            var activity = new LessonActivity(Guid.NewGuid(), version.Id, null, 0, "Provjera razumijevanja",
                LessonActivityPurpose.CheckForUnderstanding, 45);
            db.AddRange(plan, version, activity);
            await db.SaveChangesAsync();

            var service = new EfLessonPlanConfirmationService(db);
            var confirmed = await service.ConfirmAsync(teacher.Id, plan.Id,
                Convert.ToBase64String(plan.RowVersion), Convert.ToBase64String(version.RowVersion), CancellationToken.None);
            Assert.Equal(LessonPlanConfirmationFailure.None, confirmed.Failure);
            Assert.Equal("Ready", confirmed.View!.Status);
            Assert.False(confirmed.View.CanStartBoard);

            db.ChangeTracker.Clear();
            var persistedPlan = await db.LessonPlans.AsNoTracking().SingleAsync(item => item.Id == plan.Id);
            Assert.Equal(version.Id, persistedPlan.CurrentReadyVersionId);

            var immutable = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonPlanVersions] SET [Goal] = N'Promijenjeni snapshot' WHERE [Id] = {{version.Id}};
                """));
            Assert.Equal(51303, immutable.Number);

            var editable = await service.CreateEditableDraftAsync(teacher.Id, plan.Id,
                confirmed.View.PlanRowVersion, CancellationToken.None);
            Assert.Equal(LessonPlanConfirmationFailure.None, editable.Failure);
            Assert.Equal("Draft", editable.View!.Status);
            Assert.Equal(2, editable.View.VersionNumber);
            Assert.Single(editable.View.Activities);
            Assert.Equal(version.Id, (await db.LessonPlans.AsNoTracking().SingleAsync(item => item.Id == plan.Id)).CurrentReadyVersionId);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    [LocalSqlFact]
    public async Task SqlRejectsInvalidReadyGraph()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var teacher = new UserAccount(Guid.NewGuid(), "phase99-invalid@plus5.local", "PHASE99-INVALID@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-99-I", "8. razred I", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "English", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Ana", "Marić",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Valjan cilj", Now);
            db.AddRange(teacher, grade, program, student, plan, version);
            await db.SaveChangesAsync();

            var invalid = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonPlanVersions] SET [Status] = 2 WHERE [Id] = {{version.Id}};
                """));
            Assert.Equal(51350, invalid.Number);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433"
            or "database,1433" or "host.docker.internal,1433");
        var databaseName = "Plus5_Phase99Test_" + Guid.NewGuid().ToString("N");
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
