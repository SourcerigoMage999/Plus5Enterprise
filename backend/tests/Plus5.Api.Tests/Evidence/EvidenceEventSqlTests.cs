using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Plus5.Application.Evidence;
using Plus5.Domain.Evidence;
using Plus5.Domain.Identity;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Evidence;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Evidence;

public sealed class EvidenceEventSqlTests
{
    private const string PreviousMigration =
        "20260917224652_AddKnowledgeMappingLifecycleGuard";
    private static readonly DateTimeOffset Now =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly int[] DuplicateKeyErrorNumbers = [2601, 2627];

    [LocalSqlFact]
    public async Task MigrationUpgradePreservesExistingDataAndStartsWithoutEvidence()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            var fixture = await SeedAsync(db, publishModel: true);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.True(await db.Students.AsNoTracking()
                .AnyAsync(student => student.Id == fixture.StudentId));
            Assert.True(await db.KnowledgeComponents.AsNoTracking()
                .AnyAsync(component => component.Id == fixture.LeafId));
            Assert.Empty(await db.EvidenceEvents.AsNoTracking().ToListAsync());
            Assert.Empty(await db.EvidenceEventKnowledgeComponents.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task EmissionServiceCreatesOwnedLinearAuditableChain()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            EvidenceFixture fixture;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed, publishModel: true);
            }

            var sourceId = Guid.NewGuid();
            Guid observationId;
            await using (var observationContext = new Plus5DbContext(options))
            {
                var service = CreateService(observationContext);
                var result = await service.RecordObservationAsync(
                    fixture.OwnerId,
                    new(
                        fixture.StudentId,
                        "STUDENT_ATTEMPT",
                        sourceId,
                        Now.AddMinutes(-15),
                        [fixture.LeafId]),
                    CancellationToken.None);

                Assert.Equal(EvidenceWriteFailure.None, result.Failure);
                observationId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var duplicateContext = new Plus5DbContext(options))
            {
                var duplicate = await CreateService(duplicateContext).RecordObservationAsync(
                    fixture.OwnerId,
                    new(
                        fixture.StudentId,
                        "student_attempt",
                        sourceId,
                        Now.AddMinutes(-15),
                        [fixture.LeafId]),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.DuplicateSource, duplicate.Failure);
            }

            await using (var ownershipContext = new Plus5DbContext(options))
            {
                var hidden = await CreateService(ownershipContext).CorrectAsync(
                    Guid.NewGuid(),
                    new(
                        observationId,
                        Now.AddMinutes(-14),
                        "MAPPING_ERROR",
                        [fixture.SecondLeafId]),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.NotFound, hidden.Failure);
            }

            await using (var targetCardinalityContext = new Plus5DbContext(options))
            {
                var missingTarget = await CreateService(targetCardinalityContext)
                    .RecordObservationAsync(
                        fixture.OwnerId,
                        new(
                            fixture.StudentId,
                            "TEACHER_ASSESSMENT",
                            Guid.NewGuid(),
                            Now.AddMinutes(-15),
                            []),
                        CancellationToken.None);
                Assert.Equal(
                    EvidenceWriteFailure.InvalidKnowledgeTarget,
                    missingTarget.Failure);
                Assert.Equal(
                    1,
                    await targetCardinalityContext.EvidenceEvents.CountAsync());
                Assert.Equal(
                    1,
                    await targetCardinalityContext.EvidenceEventKnowledgeComponents.CountAsync());
            }

            Guid correctionId;
            await using (var correctionContext = new Plus5DbContext(options))
            {
                var corrected = await CreateService(correctionContext).CorrectAsync(
                    fixture.OwnerId,
                    new(
                        observationId,
                        Now.AddMinutes(-14),
                        "MAPPING_ERROR",
                        [fixture.SecondLeafId]),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.None, corrected.Failure);
                correctionId = Assert.IsType<Guid>(corrected.EvidenceEventId);
            }

            await using (var forkContext = new Plus5DbContext(options))
            {
                var fork = await CreateService(forkContext).CorrectAsync(
                    fixture.OwnerId,
                    new(
                        observationId,
                        Now.AddMinutes(-13),
                        "SECOND_CORRECTION",
                        [fixture.LeafId]),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.Conflict, fork.Failure);
            }

            Guid invalidationId;
            await using (var invalidationContext = new Plus5DbContext(options))
            {
                var invalidated = await CreateService(invalidationContext).InvalidateAsync(
                    fixture.OwnerId,
                    new(correctionId, "SOURCE_VOIDED"),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.None, invalidated.Failure);
                invalidationId = Assert.IsType<Guid>(invalidated.EvidenceEventId);
            }

            await using (var terminalContext = new Plus5DbContext(options))
            {
                var terminal = await CreateService(terminalContext).CorrectAsync(
                    fixture.OwnerId,
                    new(
                        invalidationId,
                        Now,
                        "NOT_ALLOWED",
                        [fixture.LeafId]),
                    CancellationToken.None);
                Assert.Equal(EvidenceWriteFailure.Conflict, terminal.Failure);
            }

            await using var verify = new Plus5DbContext(options);
            var chain = await verify.EvidenceEvents.AsNoTracking()
                .Where(evidence => evidence.StudentId == fixture.StudentId
                    && evidence.SourceId == sourceId)
                .ToListAsync();
            Assert.Equal(3, chain.Count);
            var observation = Assert.Single(
                chain,
                evidence => evidence.Kind == EvidenceEventKind.Observation);
            var correction = Assert.Single(
                chain,
                evidence => evidence.Kind == EvidenceEventKind.Correction);
            var invalidation = Assert.Single(
                chain,
                evidence => evidence.Kind == EvidenceEventKind.Invalidation);
            Assert.Equal(observation.Id, correction.SupersedesEvidenceEventId);
            Assert.Equal(correction.Id, invalidation.SupersedesEvidenceEventId);
            Assert.Equal(correction.OccurredAtUtc, invalidation.OccurredAtUtc);
            Assert.Equal(2, await verify.EvidenceEventKnowledgeComponents.CountAsync());
            Assert.False(await verify.EvidenceEventKnowledgeComponents
                .AnyAsync(mapping => mapping.EvidenceEventId == invalidationId));
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlRejectsDraftParentAndInvalidationKnowledgeTargets()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            EvidenceFixture published;
            EvidenceFixture draft;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                published = await SeedAsync(seed, publishModel: true, suffix: "P");
                draft = await SeedAsync(seed, publishModel: false, suffix: "D");
            }

            Guid observationId;
            await using (var create = new Plus5DbContext(options))
            {
                var result = await CreateService(create).RecordObservationAsync(
                    published.OwnerId,
                    new(
                        published.StudentId,
                        "TEACHER_ASSESSMENT",
                        Guid.NewGuid(),
                        Now,
                        [published.LeafId]),
                    CancellationToken.None);
                observationId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var parentTarget = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    parentTarget.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEventKnowledgeComponents]
                            ([EvidenceEventId], [KnowledgeComponentId])
                        VALUES ({{observationId}}, {{published.ParentId}});
                        """));
                Assert.Equal(51116, exception.Number);
            }

            await using (var draftTarget = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    draftTarget.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEventKnowledgeComponents]
                            ([EvidenceEventId], [KnowledgeComponentId])
                        VALUES ({{observationId}}, {{draft.LeafId}});
                        """));
                Assert.Equal(51115, exception.Number);
            }

            Guid invalidationId;
            await using (var invalidate = new Plus5DbContext(options))
            {
                var result = await CreateService(invalidate).InvalidateAsync(
                    published.OwnerId,
                    new(observationId, "VOIDED"),
                    CancellationToken.None);
                invalidationId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var invalidationTarget = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    invalidationTarget.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEventKnowledgeComponents]
                            ([EvidenceEventId], [KnowledgeComponentId])
                        VALUES ({{invalidationId}}, {{published.LeafId}});
                        """));
                Assert.Equal(51114, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlKeepsEventsAndMappingsAppendOnlyAndPreventsForks()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            EvidenceFixture fixture;
            Guid observationId;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed, publishModel: true);
            }

            await using (var create = new Plus5DbContext(options))
            {
                var result = await CreateService(create).RecordObservationAsync(
                    fixture.OwnerId,
                    new(
                        fixture.StudentId,
                        "STUDENT_ATTEMPT",
                        Guid.NewGuid(),
                        Now,
                        [fixture.LeafId]),
                    CancellationToken.None);
                observationId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var updateEvent = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    updateEvent.Database.ExecuteSqlInterpolatedAsync($$"""
                        UPDATE [EvidenceEvents]
                        SET [OccurredAtUtc] = {{Now.AddMinutes(1)}}
                        WHERE [Id] = {{observationId}};
                        """));
                Assert.Equal(51110, exception.Number);
            }

            await using (var deleteMapping = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    deleteMapping.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [EvidenceEventKnowledgeComponents]
                        WHERE [EvidenceEventId] = {{observationId}}
                            AND [KnowledgeComponentId] = {{fixture.LeafId}};
                        """));
                Assert.Equal(51113, exception.Number);
            }

            await using (var deleteEvent = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    deleteEvent.Database.ExecuteSqlInterpolatedAsync($$"""
                        DELETE FROM [EvidenceEvents]
                        WHERE [Id] = {{observationId}};
                        """));
                Assert.Equal(51110, exception.Number);
            }

            Guid correctionId;
            await using (var correct = new Plus5DbContext(options))
            {
                var result = await CreateService(correct).CorrectAsync(
                    fixture.OwnerId,
                    new(observationId, Now, "FIX", [fixture.SecondLeafId]),
                    CancellationToken.None);
                correctionId = Assert.IsType<Guid>(result.EvidenceEventId);
            }

            await using (var fork = new Plus5DbContext(options))
            {
                var predecessor = await fork.EvidenceEvents.AsNoTracking()
                    .SingleAsync(evidence => evidence.Id == observationId);
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    fork.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEvents]
                            ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                             [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                             [ReasonCode])
                        VALUES
                            ({{Guid.NewGuid()}}, {{predecessor.StudentId}}, 2,
                             {{predecessor.SourceKind}}, {{predecessor.SourceId}},
                             {{Now}}, {{Now.AddMinutes(2)}}, {{observationId}}, N'FORK');
                        """));
                Assert.Contains(exception.Number, DuplicateKeyErrorNumbers);
            }

            Assert.NotEqual(observationId, correctionId);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task SqlEnforcesSourceIdentityTerminalInvalidationAndNoCycles()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            EvidenceFixture first;
            EvidenceFixture second;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                first = await SeedAsync(seed, publishModel: true, suffix: "A");
                second = await SeedAsync(seed, publishModel: true, suffix: "B");
            }

            Guid observationId;
            Guid invalidationId;
            Guid ownershipRootId;
            await using (var create = new Plus5DbContext(options))
            {
                var service = CreateService(create);
                var observation = await service.RecordObservationAsync(
                    first.OwnerId,
                    new(
                        first.StudentId,
                        "STUDENT_ATTEMPT",
                        Guid.NewGuid(),
                        Now,
                        [first.LeafId]),
                    CancellationToken.None);
                observationId = Assert.IsType<Guid>(observation.EvidenceEventId);

                var ownershipRoot = await service.RecordObservationAsync(
                    first.OwnerId,
                    new(
                        first.StudentId,
                        "TEACHER_ASSESSMENT",
                        Guid.NewGuid(),
                        Now,
                        [first.LeafId]),
                    CancellationToken.None);
                ownershipRootId = Assert.IsType<Guid>(ownershipRoot.EvidenceEventId);
            }

            await using (var invalidate = new Plus5DbContext(options))
            {
                var invalidation = await CreateService(invalidate).InvalidateAsync(
                    first.OwnerId,
                    new(observationId, "VOIDED"),
                    CancellationToken.None);
                invalidationId = Assert.IsType<Guid>(invalidation.EvidenceEventId);
            }

            await using (var terminal = new Plus5DbContext(options))
            {
                var invalidation = await terminal.EvidenceEvents.AsNoTracking()
                    .SingleAsync(evidence => evidence.Id == invalidationId);
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    terminal.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEvents]
                            ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                             [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                             [ReasonCode])
                        VALUES
                            ({{Guid.NewGuid()}}, {{invalidation.StudentId}}, 2,
                             {{invalidation.SourceKind}}, {{invalidation.SourceId}},
                             {{Now}}, {{Now.AddMinutes(3)}}, {{invalidationId}}, N'LATE_FIX');
                        """));
                Assert.Equal(51111, exception.Number);
            }

            await using (var crossStudent = new Plus5DbContext(options))
            {
                var root = await crossStudent.EvidenceEvents.AsNoTracking()
                    .SingleAsync(evidence => evidence.Id == ownershipRootId);
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    crossStudent.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEvents]
                            ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                             [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                             [ReasonCode])
                        VALUES
                            ({{Guid.NewGuid()}}, {{second.StudentId}}, 2,
                             {{root.SourceKind}}, {{root.SourceId}},
                             {{Now}}, {{Now.AddMinutes(4)}}, {{ownershipRootId}}, N'WRONG_STUDENT');
                        """));
                Assert.Equal(547, exception.Number);
            }

            var sourceId = Guid.NewGuid();
            var firstCycleId = Guid.NewGuid();
            var secondCycleId = Guid.NewGuid();
            await using (var cycle = new Plus5DbContext(options))
            {
                var exception = await Assert.ThrowsAsync<SqlException>(() =>
                    cycle.Database.ExecuteSqlInterpolatedAsync($$"""
                        INSERT INTO [EvidenceEvents]
                            ([Id], [StudentId], [Kind], [SourceKind], [SourceId],
                             [OccurredAtUtc], [RecordedAtUtc], [SupersedesEvidenceEventId],
                             [ReasonCode])
                        VALUES
                            ({{firstCycleId}}, {{first.StudentId}}, 2,
                             N'TEACHER_ASSESSMENT', {{sourceId}},
                             {{Now}}, {{Now}}, {{secondCycleId}}, N'CYCLE'),
                            ({{secondCycleId}}, {{first.StudentId}}, 2,
                             N'TEACHER_ASSESSMENT', {{sourceId}},
                             {{Now}}, {{Now}}, {{firstCycleId}}, N'CYCLE');
                        """));
                Assert.Equal(51112, exception.Number);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task RetiredModelRemainsAValidHistoricalTarget()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            EvidenceFixture fixture;
            await using (var seed = new Plus5DbContext(options))
            {
                await seed.Database.MigrateAsync();
                fixture = await SeedAsync(seed, publishModel: true);
                var model = await seed.KnowledgeModels.SingleAsync(
                    item => item.Id == fixture.ModelId);
                model.Retire();
                await seed.SaveChangesAsync();
            }

            await using var create = new Plus5DbContext(options);
            var result = await CreateService(create).RecordObservationAsync(
                fixture.OwnerId,
                new(
                    fixture.StudentId,
                    "VERSIONED_SOURCE",
                    Guid.NewGuid(),
                    Now,
                    [fixture.LeafId]),
                CancellationToken.None);

            Assert.Equal(EvidenceWriteFailure.None, result.Failure);
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static EfEvidenceEmissionService CreateService(Plus5DbContext db) =>
        new(db, new FixedClock());

    private static async Task<EvidenceFixture> SeedAsync(
        Plus5DbContext db,
        bool publishModel,
        string suffix = "")
    {
        var owner = new UserAccount(
            Guid.NewGuid(),
            $"teacher{suffix}@example.test",
            $"TEACHER{suffix}@EXAMPLE.TEST",
            Now.AddDays(-1));
        owner.SetPasswordHash("test-hash", Now.AddDays(-1));
        var grade = new SchoolGrade(
            Guid.NewGuid(),
            $"G{suffix}",
            $"Grade {suffix}",
            0);
        var student = new Student(
            Guid.NewGuid(),
            owner.Id,
            grade.Id,
            "Ana",
            $"Student {suffix}",
            StudentStatus.Active,
            Now.AddDays(-1));
        var model = new KnowledgeModel(
            Guid.NewGuid(),
            $"MODEL{suffix}",
            "V1");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Area", 0);
        var parent = new KnowledgeComponent(
            Guid.NewGuid(),
            model,
            area,
            "Parent",
            0);
        var leaf = new KnowledgeComponent(
            Guid.NewGuid(),
            model,
            area,
            "Leaf",
            0,
            parent);
        var secondLeaf = new KnowledgeComponent(
            Guid.NewGuid(),
            model,
            area,
            "Second leaf",
            1,
            parent);

        db.AddRange(owner, grade, student, model, area, parent, leaf, secondLeaf);
        await db.SaveChangesAsync();
        if (publishModel)
        {
            model.Publish();
            await db.SaveChangesAsync();
        }

        db.ChangeTracker.Clear();
        return new(
            owner.Id,
            student.Id,
            model.Id,
            parent.Id,
            leaf.Id,
            secondLeaf.Id);
    }

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName)
        CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase53Test_" + Guid.NewGuid().ToString("N");
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

    private sealed record EvidenceFixture(
        Guid OwnerId,
        Guid StudentId,
        Guid ModelId,
        Guid ParentId,
        Guid LeafId,
        Guid SecondLeafId);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
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
