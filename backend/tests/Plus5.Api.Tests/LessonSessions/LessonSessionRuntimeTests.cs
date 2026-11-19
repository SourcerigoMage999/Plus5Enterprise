using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Application.LessonSessions;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.LessonSessions;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.LessonSessions;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonSessions;

public sealed class LessonSessionRuntimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RuntimeSessionRequiresOneParticipantAndRevisionSafeMutations()
    {
        var session = new LessonSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, Guid.NewGuid(), null, Now);

        Assert.Equal(LessonSessionStatus.InProgress, session.Status);
        Assert.Equal(1, session.Revision);

        session.CommitMutation(1, Now.AddMinutes(1));
        Assert.Equal(2, session.Revision);
        Assert.Throws<InvalidOperationException>(() => session.CommitMutation(1, Now.AddMinutes(2)));
        Assert.Throws<ArgumentException>(() => new LessonSession(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, Now));
    }

    [LocalSqlFact]
    public async Task StartRetryReorderAndRecoveryAreAtomicAndRevisionSafe()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var teacher = new UserAccount(Guid.NewGuid(), "phase101@plus5.local", "PHASE101@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-101", "8. razred 10.1", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "English", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Lana", "Kovač",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            db.AddRange(teacher, grade, program, student);
            await db.SaveChangesAsync();

            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1,
                "Primijeniti ciljano znanje u stvarnom satu.", Now, 45);
            var first = new LessonActivity(Guid.NewGuid(), version.Id, null, 0, "Uvod",
                LessonActivityPurpose.Engagement, 10);
            var second = new LessonActivity(Guid.NewGuid(), version.Id, null, 1, "Vježba",
                LessonActivityPurpose.Practice, 35);
            db.AddRange(plan, version, first, second);
            await db.SaveChangesAsync();

            var confirmation = new EfLessonPlanConfirmationService(db);
            var ready = await confirmation.ConfirmAsync(teacher.Id, plan.Id,
                Convert.ToBase64String(plan.RowVersion), Convert.ToBase64String(version.RowVersion),
                CancellationToken.None);
            Assert.Equal(LessonPlanConfirmationFailure.None, ready.Failure);
            Assert.True(ready.View!.CanStartBoard);

            var service = new EfLessonSessionService(db);
            var operationId = Guid.NewGuid();
            var started = await service.StartAsync(teacher.Id, plan.Id, version.Id, null,
                operationId, CancellationToken.None);
            Assert.Equal(LessonSessionFailure.None, started.Failure);
            Assert.True(started.Created);
            Assert.Equal(1, started.View!.Revision);
            Assert.Equal([first.Id, second.Id], started.View.Activities.Select(item => item.SourceLessonActivityId));
            Assert.All(started.View.Activities, item => Assert.Equal("Pending", item.Status));

            var retried = await service.StartAsync(teacher.Id, plan.Id, version.Id, null,
                operationId, CancellationToken.None);
            Assert.Equal(started.View.Id, retried.View!.Id);
            Assert.False(retried.Created);
            Assert.Single(await db.LessonSessions.AsNoTracking().ToListAsync());

            var mismatch = await service.StartAsync(teacher.Id, plan.Id, version.Id,
                Guid.NewGuid(), operationId, CancellationToken.None);
            Assert.Equal(LessonSessionFailure.OperationMismatch, mismatch.Failure);

            var reversed = started.View.Activities.Reverse().Select(item => item.Id).ToArray();
            var reorderOperation = Guid.NewGuid();
            var reordered = await service.ReorderTimelineAsync(teacher.Id, started.View.Id,
                reorderOperation, 1, reversed, CancellationToken.None);
            Assert.Equal(LessonSessionFailure.None, reordered.Failure);
            Assert.Equal(2, reordered.View!.Revision);
            Assert.Equal(reversed, reordered.View.Activities.Select(item => item.Id));

            var reorderRetry = await service.ReorderTimelineAsync(teacher.Id, started.View.Id,
                reorderOperation, 1, reversed, CancellationToken.None);
            Assert.Equal(2, reorderRetry.View!.Revision);

            var stale = await service.ReorderTimelineAsync(teacher.Id, started.View.Id,
                Guid.NewGuid(), 1, reversed, CancellationToken.None);
            Assert.Equal(LessonSessionFailure.ConcurrencyConflict, stale.Failure);

            db.ChangeTracker.Clear();
            var recovered = await service.GetActiveAsync(teacher.Id, CancellationToken.None);
            Assert.Single(recovered);
            Assert.Equal(started.View.Id, recovered[0].Id);
            Assert.Equal(2, recovered[0].Revision);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    [LocalSqlFact]
    public async Task SqlGuardsProtectRuntimeAnchorsSnapshotsAndAppendOnlyLedgers()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var teacher = new UserAccount(Guid.NewGuid(), "phase101-guards@plus5.local",
                "PHASE101-GUARDS@PLUS5.LOCAL", Now);
            var grade = new SchoolGrade(Guid.NewGuid(), "G8-101-G", "8. razred 10.1 G", 8);
            var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "English", Now);
            var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Ema", "Barić",
                StudentStatus.Active, Now, program.Id, DeliveryMode.Individual);
            db.AddRange(teacher, grade, program, student);
            await db.SaveChangesAsync();
            var plan = new LessonPlan(Guid.NewGuid(), teacher.Id, student.Id, null, null, Now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "Sigurna izvedba.", Now);
            var activity = new LessonActivity(Guid.NewGuid(), version.Id, null, 0, "Aktivnost",
                LessonActivityPurpose.Practice, 45);
            db.AddRange(plan, version, activity);
            await db.SaveChangesAsync();
            var confirmation = new EfLessonPlanConfirmationService(db);
            await confirmation.ConfirmAsync(teacher.Id, plan.Id, Convert.ToBase64String(plan.RowVersion),
                Convert.ToBase64String(version.RowVersion), CancellationToken.None);
            var started = await new EfLessonSessionService(db).StartAsync(teacher.Id, plan.Id,
                version.Id, null, Guid.NewGuid(), CancellationToken.None);
            Assert.Equal(LessonSessionFailure.None, started.Failure);
            var sessionView = Assert.IsType<LessonSessionView>(started.View);

            var anchor = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonSessions] SET [StartedAtUtc] = DATEADD(MINUTE, 1, [StartedAtUtc]), [Revision] = [Revision] + 1
                WHERE [Id] = {{sessionView.Id}};
                """));
            Assert.Equal(51201, anchor.Number);

            var snapshot = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                UPDATE [LessonSessionActivities] SET [Name] = N'Retroaktivna promjena'
                WHERE [LessonSessionId] = {{sessionView.Id}};
                """));
            Assert.Equal(51204, snapshot.Number);

            var operationId = await db.LessonSessionOperations.AsNoTracking()
                .Where(item => item.LessonSessionId == sessionView.Id)
                .Select(item => item.Id).SingleAsync();
            var ledger = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($$"""
                DELETE FROM [LessonSessionOperations] WHERE [Id] = {{operationId}};
                """));
            Assert.Equal(51209, ledger.Number);
        }
        finally { await DeleteDatabase(options, databaseName); }
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is "localhost,1433" or "127.0.0.1,1433"
            or "database,1433" or "host.docker.internal,1433");
        var databaseName = "Plus5_Phase101Test_" + Guid.NewGuid().ToString("N");
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
