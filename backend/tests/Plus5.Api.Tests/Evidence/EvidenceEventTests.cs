using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Evidence;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Evidence;

public sealed class EvidenceEventTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 9, 24, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset RecordedAtUtc =
        new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ObservationRequiresPublishedOrRetiredLeafTargets()
    {
        var studentId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var (publishedModel, _, publishedLeaf) = CreatePublishedTree("CORE", "V1");

        var observation = EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            studentId,
            " student_attempt ",
            sourceId,
            OccurredAtUtc,
            RecordedAtUtc,
            CreateMetadata(),
            0.80m,
            [new EvidenceKnowledgeTarget(publishedLeaf, publishedModel, isLeaf: true)]);

        Assert.Equal(EvidenceEventKind.Observation, observation.Kind);
        Assert.Equal("STUDENT_ATTEMPT", observation.SourceKind);
        Assert.Equal(studentId, observation.StudentId);
        Assert.Equal(sourceId, observation.SourceId);
        Assert.Null(observation.SupersedesEvidenceEventId);
        Assert.Null(observation.ReasonCode);
        Assert.Equal(0.80m, observation.PerformanceScore);
        Assert.Equal(2, observation.Difficulty);
        Assert.Equal(EvidenceType.Application, observation.EvidenceType);
        Assert.Equal(AssistanceLevel.NotObserved, observation.AssistanceLevel);
        Assert.Equal(EvidenceContext.Assessment, observation.EvidenceContext);
        Assert.Equal(
            publishedLeaf.Id,
            Assert.Single(observation.KnowledgeComponents).KnowledgeComponentId);

        Assert.Throws<ArgumentException>(() => EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            studentId,
            "STUDENT_ATTEMPT",
            sourceId,
            OccurredAtUtc,
            RecordedAtUtc,
            CreateMetadata(),
            0.80m,
            []));

        var draftModel = new KnowledgeModel(Guid.NewGuid(), "CORE", "V2");
        var draftArea = new KnowledgeArea(Guid.NewGuid(), draftModel, "Area", 0);
        var draftLeaf = new KnowledgeComponent(
            Guid.NewGuid(),
            draftModel,
            draftArea,
            "Leaf",
            0);
        Assert.Throws<ArgumentException>(() =>
            new EvidenceKnowledgeTarget(draftLeaf, draftModel, isLeaf: true));

        var (parentModel, publishedParent, _) = CreatePublishedTree("CORE", "V3");
        Assert.Throws<ArgumentException>(() =>
            new EvidenceKnowledgeTarget(publishedParent, parentModel, isLeaf: false));
    }

    [Fact]
    public void CorrectionReplacesEffectivePayloadWithoutRewritingOriginal()
    {
        var (model, _, firstLeaf) = CreatePublishedTree("CORE", "V1");
        var observation = CreateObservation(model, firstLeaf);
        var (correctedModel, _, correctedLeaf) = CreatePublishedTree("CORE", "V2");
        var correctedOccurrence = OccurredAtUtc.AddMinutes(5);
        var correctedMetadata = new EvidenceMetadata(
            4,
            EvidenceType.Production,
            AssistanceLevel.Independent,
            EvidenceContext.Lesson);

        var correction = EvidenceEvent.CreateCorrection(
            Guid.NewGuid(),
            observation,
            correctedOccurrence,
            RecordedAtUtc.AddMinutes(10),
            " mapping_error ",
            correctedMetadata,
            0.90m,
            [new EvidenceKnowledgeTarget(correctedLeaf, correctedModel, true)]);

        Assert.Equal(EvidenceEventKind.Correction, correction.Kind);
        Assert.Equal(observation.Id, correction.SupersedesEvidenceEventId);
        Assert.Equal(observation.StudentId, correction.StudentId);
        Assert.Equal(observation.SourceKind, correction.SourceKind);
        Assert.Equal(observation.SourceId, correction.SourceId);
        Assert.Equal(correctedOccurrence, correction.OccurredAtUtc);
        Assert.Equal("MAPPING_ERROR", correction.ReasonCode);
        Assert.Equal(0.80m, observation.PerformanceScore);
        Assert.Equal(0.90m, correction.PerformanceScore);
        Assert.Equal(2, observation.Difficulty);
        Assert.Equal(EvidenceType.Application, observation.EvidenceType);
        Assert.Equal(4, correction.Difficulty);
        Assert.Equal(EvidenceType.Production, correction.EvidenceType);
        Assert.Equal(AssistanceLevel.Independent, correction.AssistanceLevel);
        Assert.Equal(EvidenceContext.Lesson, correction.EvidenceContext);
        Assert.Equal(firstLeaf.Id, Assert.Single(observation.KnowledgeComponents).KnowledgeComponentId);
        Assert.Equal(correctedLeaf.Id, Assert.Single(correction.KnowledgeComponents).KnowledgeComponentId);
    }

    [Fact]
    public void InvalidationIsTerminalAndCarriesNoKnowledgeTarget()
    {
        var (model, _, leaf) = CreatePublishedTree("CORE", "V1");
        var observation = CreateObservation(model, leaf);
        var invalidation = EvidenceEvent.CreateInvalidation(
            Guid.NewGuid(),
            observation,
            RecordedAtUtc.AddHours(1),
            "source_voided");

        Assert.Equal(EvidenceEventKind.Invalidation, invalidation.Kind);
        Assert.Equal(observation.Id, invalidation.SupersedesEvidenceEventId);
        Assert.Equal(observation.OccurredAtUtc, invalidation.OccurredAtUtc);
        Assert.Equal("SOURCE_VOIDED", invalidation.ReasonCode);
        Assert.Null(invalidation.PerformanceScore);
        Assert.Null(invalidation.Difficulty);
        Assert.Null(invalidation.EvidenceType);
        Assert.Null(invalidation.AssistanceLevel);
        Assert.Null(invalidation.EvidenceContext);
        Assert.Empty(invalidation.KnowledgeComponents);
        Assert.Throws<InvalidOperationException>(() => EvidenceEvent.CreateCorrection(
            Guid.NewGuid(),
            invalidation,
            OccurredAtUtc,
            RecordedAtUtc.AddHours(2),
            "INVALID",
            CreateMetadata(),
            0.80m,
            [new EvidenceKnowledgeTarget(leaf, model, true)]));
    }

    [Fact]
    public void CodesAndUtcTimestampsAreBounded()
    {
        var (model, _, leaf) = CreatePublishedTree("CORE", "V1");
        var target = new EvidenceKnowledgeTarget(leaf, model, true);

        Assert.Throws<ArgumentException>(() => EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "not allowed!",
            Guid.NewGuid(),
            OccurredAtUtc,
            RecordedAtUtc,
            CreateMetadata(),
            0.80m,
            [target]));
        Assert.Throws<ArgumentException>(() => EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOURCE",
            Guid.NewGuid(),
            OccurredAtUtc.ToOffset(TimeSpan.FromHours(2)),
            RecordedAtUtc,
            CreateMetadata(),
            0.80m,
            [target]));
    }

    [Fact]
    public void MetadataUsesOnlyLockedCanonicalValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceMetadata(
            0,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Lesson));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceMetadata(
            6,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Lesson));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceMetadata(
            3,
            (EvidenceType)999,
            AssistanceLevel.Independent,
            EvidenceContext.Lesson));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceMetadata(
            3,
            EvidenceType.Application,
            (AssistanceLevel)999,
            EvidenceContext.Lesson));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceMetadata(
            3,
            EvidenceType.Application,
            AssistanceLevel.NotObserved,
            (EvidenceContext)999));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void ObservationRejectsPerformanceScoreOutsideNormalizedRange(decimal score)
    {
        var (model, _, leaf) = CreatePublishedTree("CORE", "V1");

        Assert.Throws<ArgumentOutOfRangeException>(() => EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "STUDENT_ATTEMPT",
            Guid.NewGuid(),
            OccurredAtUtc,
            RecordedAtUtc,
            CreateMetadata(),
            score,
            [new EvidenceKnowledgeTarget(leaf, model, true)]));
    }

    [Fact]
    public void EfModelKeepsEvidenceStudentSpecificAppendOnlyAndNormalized()
    {
        using var db = CreateDbContext();
        var evidence = db.Model.FindEntityType(typeof(EvidenceEvent))!;
        var mapping = db.Model.FindEntityType(typeof(EvidenceEventKnowledgeComponent))!;

        Assert.DoesNotContain(evidence.GetProperties(), property =>
            property.Name is "TeacherAccountId" or "MasteryScore" or "ReadinessScore"
                or "Confidence" or "Weight");
        Assert.Contains(evidence.GetProperties(), property =>
            property.Name == nameof(EvidenceEvent.Difficulty));
        Assert.Equal(
            16,
            evidence.FindProperty(nameof(EvidenceEvent.EvidenceType))!.GetMaxLength());
        Assert.Equal(
            24,
            evidence.FindProperty(nameof(EvidenceEvent.AssistanceLevel))!.GetMaxLength());
        Assert.Equal(
            24,
            evidence.FindProperty(nameof(EvidenceEvent.EvidenceContext))!.GetMaxLength());
        Assert.Equal(2, evidence.GetForeignKeys().Count());
        Assert.All(evidence.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(evidence.GetIndexes(), index =>
            index.IsUnique
            && index.GetFilter() == "[SupersedesEvidenceEventId] IS NOT NULL");
        Assert.Contains(evidence.GetIndexes(), index =>
            index.IsUnique
            && index.GetFilter() == "[SupersedesEvidenceEventId] IS NULL");
        Assert.Equal(
            [
                nameof(EvidenceEventKnowledgeComponent.EvidenceEventId),
                nameof(EvidenceEventKnowledgeComponent.KnowledgeComponentId),
            ],
            mapping.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.All(mapping.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static EvidenceEvent CreateObservation(
        KnowledgeModel model,
        KnowledgeComponent leaf) =>
        EvidenceEvent.CreateObservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "STUDENT_ATTEMPT",
            Guid.NewGuid(),
            OccurredAtUtc,
            RecordedAtUtc,
            CreateMetadata(),
            0.80m,
            [new EvidenceKnowledgeTarget(leaf, model, true)]);

    private static EvidenceMetadata CreateMetadata() =>
        new(
            2,
            EvidenceType.Application,
            AssistanceLevel.NotObserved,
            EvidenceContext.Assessment);

    private static (KnowledgeModel Model, KnowledgeComponent Parent, KnowledgeComponent Leaf)
        CreatePublishedTree(string code, string version)
    {
        var model = new KnowledgeModel(Guid.NewGuid(), code, version);
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Area", 0);
        var parent = new KnowledgeComponent(Guid.NewGuid(), model, area, "Parent", 0);
        var leaf = new KnowledgeComponent(Guid.NewGuid(), model, area, "Leaf", 0, parent);
        model.Publish();
        return (model, parent, leaf);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
