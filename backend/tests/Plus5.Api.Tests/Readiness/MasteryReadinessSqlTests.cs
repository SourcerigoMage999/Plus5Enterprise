using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Plus5.Application.Evidence;
using Plus5.Application.Readiness;
using Plus5.Domain.Evidence;
using Plus5.Domain.Identity;
using Plus5.Domain.Readiness;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Evidence;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Readiness;

namespace Plus5.Api.Tests.Readiness;

public sealed class MasteryReadinessSqlTests
{
    private const string PreviousMigration = "20260924205126_AddEvidenceMetadata";
    private static readonly DateTimeOffset Now =
        new(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task MigrationUpgradeCreatesEmptyRebuildableProjectionSchema()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var fixture = await SeedAsync(db);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.True(await db.Students.AsNoTracking()
                .AnyAsync(student => student.Id == fixture.StudentId));
            Assert.Empty(await db.EvidenceEvents.AsNoTracking().ToListAsync());
            Assert.Empty(await db.MasteryEstimates.AsNoTracking().ToListAsync());
            Assert.Empty(await db.KnowledgeAreaReadinessEstimates.AsNoTracking().ToListAsync());
            Assert.Empty(await db.CurriculumOutcomeReadinessEstimates.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task EventDrivenProjectionUsesOnlyLatestValidChainAndAggregatesHierarchy()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            ReadinessFixture fixture;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed);
            }

            var firstLeafOne = await ObserveAsync(options, fixture, fixture.LeafOneId, 0.80m, "A");
            await ObserveAsync(options, fixture, fixture.LeafOneId, 0.60m, "B");
            await ObserveAsync(options, fixture, fixture.LeafTwoId, 0.90m, "C");
            await ObserveAsync(options, fixture, fixture.LeafTwoId, 1.00m, "D");

            await using (var initial = new Plus5DbContext(options))
            {
                var leafOne = await EstimateAsync(initial, fixture.StudentId, fixture.LeafOneId);
                var leafTwo = await EstimateAsync(initial, fixture.StudentId, fixture.LeafTwoId);
                var parent = await EstimateAsync(initial, fixture.StudentId, fixture.ParentId);
                var area = await initial.KnowledgeAreaReadinessEstimates.AsNoTracking()
                    .SingleAsync(item => item.StudentId == fixture.StudentId
                        && item.KnowledgeAreaId == fixture.AreaId);
                var outcome = await initial.CurriculumOutcomeReadinessEstimates.AsNoTracking()
                    .SingleAsync(item => item.StudentId == fixture.StudentId
                        && item.CurriculumOutcomeId == fixture.OutcomeId);

                Assert.Equal(0.70m, leafOne.Score);
                Assert.Equal(ReadinessStatus.Developing, leafOne.Readiness);
                Assert.Equal(0.95m, leafTwo.Score);
                Assert.Equal(ReadinessStatus.Strong, leafTwo.Readiness);
                Assert.Equal(0.825m, parent.Score);
                Assert.Equal(ReadinessStatus.Ready, parent.Readiness);
                Assert.Equal(parent.Score, area.Score);
                Assert.Equal(parent.Score, outcome.Score);
                Assert.Equal(4, parent.EvidenceCount);
                Assert.All(
                    new[] { leafOne, leafTwo, parent },
                    item => Assert.Equal(MasteryReadinessCalculator.AlgorithmVersion, item.AlgorithmVersion));
            }

            Guid correctionId;
            await using (var correctionDb = new Plus5DbContext(options))
            {
                var result = await CreateEvidenceService(correctionDb, new MutableClock(Now))
                    .CorrectAsync(
                        fixture.OwnerId,
                        new(
                            firstLeafOne,
                            Now,
                            "RECLASSIFIED",
                            3,
                            EvidenceType.Application,
                            AssistanceLevel.Independent,
                            EvidenceContext.Assessment,
                            0.20m,
                            [fixture.LeafTwoId]),
                        CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.None, result.Failure);
                correctionId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var corrected = new Plus5DbContext(options))
            {
                var leafOne = await EstimateAsync(corrected, fixture.StudentId, fixture.LeafOneId);
                var leafTwo = await EstimateAsync(corrected, fixture.StudentId, fixture.LeafTwoId);
                var parent = await EstimateAsync(corrected, fixture.StudentId, fixture.ParentId);
                Assert.Equal(1, leafOne.EvidenceCount);
                Assert.Equal(0.60m, leafOne.Score);
                Assert.Equal(ReadinessStatus.InsufficientData, leafOne.Readiness);
                Assert.Equal(3, leafTwo.EvidenceCount);
                Assert.Equal(0.70m, leafTwo.Score);
                Assert.Equal(ReadinessStatus.Developing, leafTwo.Readiness);
                Assert.Equal(ReadinessStatus.InsufficientData, parent.Readiness);
            }

            await using (var invalidationDb = new Plus5DbContext(options))
            {
                var result = await CreateEvidenceService(invalidationDb, new MutableClock(Now))
                    .InvalidateAsync(
                        fixture.OwnerId,
                        new(correctionId, "VOIDED"),
                        CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.None, result.Failure);
            }

            await using (var invalidated = new Plus5DbContext(options))
            {
                var leafTwo = await EstimateAsync(invalidated, fixture.StudentId, fixture.LeafTwoId);
                Assert.Equal(2, leafTwo.EvidenceCount);
                Assert.Equal(0.95m, leafTwo.Score);
                Assert.Equal(ReadinessStatus.Strong, leafTwo.Readiness);
                Assert.Equal(
                    4,
                    await invalidated.EvidenceEvents.AsNoTracking()
                        .CountAsync(item => item.Kind == EvidenceEventKind.Observation));
                Assert.Equal(
                    1,
                    await invalidated.EvidenceEvents.AsNoTracking()
                        .CountAsync(item => item.Kind == EvidenceEventKind.Correction));
                Assert.Equal(
                    1,
                    await invalidated.EvidenceEvents.AsNoTracking()
                        .CountAsync(item => item.Kind == EvidenceEventKind.Invalidation));
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlRejectsInvalidPerformanceAndProjectionValues()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            ReadinessFixture fixture;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed);
            }

            await AssertPerformanceRejectedAsync(options, fixture.StudentId, -0.01m);
            await AssertPerformanceRejectedAsync(options, fixture.StudentId, 1.01m);
            await AssertMissingPerformanceRejectedAsync(options, fixture.StudentId);
            await ObserveAsync(options, fixture, fixture.LeafOneId, 0.80m, "VALID");

            await using var db = new Plus5DbContext(options);
            var invalidScore = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MasteryEstimates]
                    SET [Score] = 2
                    WHERE [StudentId] = {{fixture.StudentId}}
                        AND [KnowledgeComponentId] = {{fixture.LeafOneId}};
                    """));
            Assert.Equal(547, invalidScore.Number);
            Assert.Contains("CK_MasteryEstimates_Score", invalidScore.Message);

            var invalidCode = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [MasteryEstimates]
                    SET [Confidence] = N'Certain'
                    WHERE [StudentId] = {{fixture.StudentId}}
                        AND [KnowledgeComponentId] = {{fixture.LeafOneId}};
                    """));
            Assert.Equal(547, invalidCode.Number);
            Assert.Contains("CK_MasteryEstimates_Confidence", invalidCode.Message);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task DailyRefreshAppliesDecayAndSqlLeasePreventsConcurrentRun()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            ReadinessFixture fixture;
            var clock = new MutableClock(Now);
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed);
            }

            await ObserveAsync(options, fixture, fixture.LeafOneId, 0.80m, "A", clock);
            await ObserveAsync(options, fixture, fixture.LeafOneId, 0.80m, "B", clock);

            clock.Advance(TimeSpan.FromDays(90));
            var service = new EfReadinessRefreshService(
                options,
                clock,
                NullLogger<EfReadinessRefreshService>.Instance);
            var refreshed = await service.RunAsync("worker-one", CancellationToken.None);
            Assert.Equal(ReadinessRefreshRunStatus.Completed, refreshed.Status);
            Assert.Equal(1, refreshed.StudentsRecalculated);

            await using (var verify = new Plus5DbContext(options))
            {
                var estimate = await EstimateAsync(verify, fixture.StudentId, fixture.LeafOneId);
                Assert.Equal(1m, estimate.EffectiveEvidenceWeight);
                Assert.Equal(ReadinessConfidence.Low, estimate.Confidence);
                Assert.Equal(ReadinessStatus.InsufficientData, estimate.Readiness);
                Assert.Equal(clock.GetUtcNow(), estimate.CalculatedAtUtc);
            }

            var lease = new SqlReadinessRefreshLeaseManager(options, clock);
            Assert.True(await lease.TryAcquireAsync("worker-one", CancellationToken.None));
            var skipped = await service.RunAsync("worker-two", CancellationToken.None);
            Assert.Equal(ReadinessRefreshRunStatus.LeaseSkipped, skipped.Status);
            await lease.ReleaseAsync("worker-one", CancellationToken.None);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static async Task<Guid> ObserveAsync(
        DbContextOptions<Plus5DbContext> options,
        ReadinessFixture fixture,
        Guid componentId,
        decimal score,
        string suffix,
        MutableClock? clock = null)
    {
        clock ??= new MutableClock(Now);
        await using var db = new Plus5DbContext(options);
        var result = await CreateEvidenceService(db, clock).RecordObservationAsync(
            fixture.OwnerId,
            new(
                fixture.StudentId,
                "READINESS_TEST_" + suffix,
                Guid.NewGuid(),
                Now,
                3,
                EvidenceType.Application,
                AssistanceLevel.Independent,
                EvidenceContext.Assessment,
                score,
                [componentId]),
            CancellationToken.None);
        Assert.Equal(EvidenceWriteFailure.None, result.Failure);
        return Assert.IsType<Guid>(result.EvidenceEventId);
    }

    private static EfEvidenceEmissionService CreateEvidenceService(
        Plus5DbContext db,
        TimeProvider clock) =>
        new(db, clock, new EfMasteryReadinessProjectionService(db, clock));

    private static Task<MasteryEstimate> EstimateAsync(
        Plus5DbContext db,
        Guid studentId,
        Guid componentId) =>
        db.MasteryEstimates.AsNoTracking().SingleAsync(item =>
            item.StudentId == studentId && item.KnowledgeComponentId == componentId);

    private static async Task AssertPerformanceRejectedAsync(
        DbContextOptions<Plus5DbContext> options,
        Guid studentId,
        decimal performanceScore)
    {
        await using var db = new Plus5DbContext(options);
        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [EvidenceEvents]
                    ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                     [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                     [ReasonCode], [PerformanceScore], [Difficulty], [EvidenceType],
                     [AssistanceLevel], [EvidenceContext])
                VALUES
                    ({{Guid.NewGuid()}}, {{studentId}}, 1, N'PERFORMANCE_TEST', {{Guid.NewGuid()}},
                     {{Now}}, {{Now}}, NULL, NULL, {{performanceScore}}, 3, N'Application',
                     N'Independent', N'Assessment');
                """));
        Assert.Equal(547, exception.Number);
        Assert.Contains("CK_EvidenceEvents_PerformanceScore", exception.Message);
    }

    private static async Task AssertMissingPerformanceRejectedAsync(
        DbContextOptions<Plus5DbContext> options,
        Guid studentId)
    {
        await using var db = new Plus5DbContext(options);
        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO [EvidenceEvents]
                    ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                     [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                     [ReasonCode], [PerformanceScore], [Difficulty], [EvidenceType],
                     [AssistanceLevel], [EvidenceContext])
                VALUES
                    ({{Guid.NewGuid()}}, {{studentId}}, 1, N'PERFORMANCE_TEST', {{Guid.NewGuid()}},
                     {{Now}}, {{Now}}, NULL, NULL, NULL, 3, N'Application',
                     N'Independent', N'Assessment');
                """));
        Assert.Equal(547, exception.Number);
        Assert.Contains("CK_EvidenceEvents_MetadataShape", exception.Message);
    }

    private static async Task<ReadinessFixture> SeedAsync(Plus5DbContext db)
    {
        var accountToken = Guid.NewGuid().ToString("N");
        var owner = new UserAccount(
            Guid.NewGuid(),
            $"readiness-{accountToken}@example.test",
            $"READINESS-{accountToken.ToUpperInvariant()}@EXAMPLE.TEST",
            Now.AddDays(-1));
        owner.SetPasswordHash("test-hash", Now.AddDays(-1));
        var gradeCode = Guid.NewGuid().ToString("N")[..20];
        var grade = new SchoolGrade(Guid.NewGuid(), $"R-{gradeCode}", "Readiness grade", 0);
        var student = new Student(
            Guid.NewGuid(),
            owner.Id,
            grade.Id,
            "Ana",
            "Readiness",
            StudentStatus.Active,
            Now.AddDays(-1));
        var curriculum = new Curriculum(Guid.NewGuid(), $"CUR-{Guid.NewGuid():N}", "Curriculum", "V1");
        var outcome = new CurriculumOutcome(Guid.NewGuid(), curriculum, "Outcome", 0);
        var model = new KnowledgeModel(Guid.NewGuid(), $"MODEL-{Guid.NewGuid():N}", "V1");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Area", 0);
        var parent = new KnowledgeComponent(Guid.NewGuid(), model, area, "Parent", 0);
        var leafOne = new KnowledgeComponent(Guid.NewGuid(), model, area, "Leaf one", 0, parent);
        var leafTwo = new KnowledgeComponent(Guid.NewGuid(), model, area, "Leaf two", 1, parent);
        var mapping = new CurriculumOutcomeKnowledgeComponent(outcome, parent);

        db.AddRange(
            owner,
            grade,
            student,
            curriculum,
            outcome,
            model,
            area,
            parent,
            leafOne,
            leafTwo,
            mapping);
        await db.SaveChangesAsync();
        model.Publish();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return new(
            owner.Id,
            student.Id,
            area.Id,
            parent.Id,
            leafOne.Id,
            leafTwo.Id,
            outcome.Id);
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase55Test_" + Guid.NewGuid().ToString("N");
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

    private sealed record ReadinessFixture(
        Guid OwnerId,
        Guid StudentId,
        Guid AreaId,
        Guid ParentId,
        Guid LeafOneId,
        Guid LeafTwoId,
        Guid OutcomeId);

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan value) => now = now.Add(value);
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
