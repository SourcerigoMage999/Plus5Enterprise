using Plus5.Domain.Evidence;
using Plus5.Domain.Readiness;

namespace Plus5.Api.Tests.Readiness;

public sealed class MasteryReadinessCalculatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WeightCatalogAndNinetyDayDecayMatchLockedV1Contract()
    {
        Assert.Equal(1m, Weight(
            3,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Assessment,
            Now));
        Assert.Equal(0.5m, Weight(
            3,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Assessment,
            Now.AddDays(-90)));
        Assert.Equal(0.10m, Weight(
            3,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Assessment,
            Now.AddDays(-900)));
        Assert.Equal(0.8m * 0.75m * 0.85m * 0.85m, Weight(
            1,
            EvidenceType.Recognition,
            AssistanceLevel.MinorAssistance,
            EvidenceContext.Homework,
            Now));
        Assert.Equal(1.2m * 1.15m * 0.6m * 0.9m, Weight(
            5,
            EvidenceType.Production,
            AssistanceLevel.SignificantAssistance,
            EvidenceContext.Lesson,
            Now));
        Assert.Equal(0.75m, Weight(
            3,
            EvidenceType.Application,
            AssistanceLevel.NotObserved,
            EvidenceContext.Assessment,
            Now));
    }

    [Fact]
    public void LeafUsesWeightedAverageAndSeparatesScoreConfidenceAndReadiness()
    {
        var oneEvidence = MasteryReadinessCalculator.CalculateLeaf(
            [Input(0.95m)],
            Now);
        Assert.Equal(0.95m, oneEvidence.Score);
        Assert.Equal(ReadinessConfidence.Low, oneEvidence.Confidence);
        Assert.Equal(ReadinessStatus.InsufficientData, oneEvidence.Readiness);

        var twoEvidence = MasteryReadinessCalculator.CalculateLeaf(
            [Input(0.80m), Input(0.70m)],
            Now);
        Assert.Equal(0.75m, twoEvidence.Score);
        Assert.Equal(2m, twoEvidence.EffectiveEvidenceWeight);
        Assert.Equal(ReadinessConfidence.Medium, twoEvidence.Confidence);
        Assert.Equal(ReadinessStatus.Ready, twoEvidence.Readiness);

        var noEvidence = MasteryReadinessCalculator.CalculateLeaf([], Now);
        Assert.Null(noEvidence.Score);
        Assert.Equal(ReadinessConfidence.NoData, noEvidence.Confidence);
        Assert.Equal(ReadinessStatus.InsufficientData, noEvidence.Readiness);
    }

    [Theory]
    [InlineData(0.59, ReadinessStatus.NeedsWork)]
    [InlineData(0.60, ReadinessStatus.Developing)]
    [InlineData(0.74, ReadinessStatus.Developing)]
    [InlineData(0.75, ReadinessStatus.Ready)]
    [InlineData(0.89, ReadinessStatus.Ready)]
    [InlineData(0.90, ReadinessStatus.Strong)]
    public void LeafReadinessUsesLockedThresholds(
        decimal score,
        ReadinessStatus expected)
    {
        var result = MasteryReadinessCalculator.CalculateLeaf(
            [Input(score), Input(score), Input(score), Input(score)],
            Now);

        Assert.Equal(ReadinessConfidence.High, result.Confidence);
        Assert.Equal(expected, result.Readiness);
    }

    [Fact]
    public void ParentUsesImmediateChildrenCoverageAndCapsConfidence()
    {
        var sufficient = new MasteryCalculation(
            0.80m,
            ReadinessConfidence.High,
            ReadinessStatus.Ready,
            4,
            4m,
            Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToHashSet());
        var insufficient = new MasteryCalculation(
            0.90m,
            ReadinessConfidence.Low,
            ReadinessStatus.InsufficientData,
            1,
            1m,
            new HashSet<Guid> { Guid.NewGuid() });

        var belowCoverage = MasteryReadinessCalculator.AggregateChildren(
            Enumerable.Repeat(sufficient, 6)
                .Concat(Enumerable.Repeat(insufficient, 4))
                .ToArray());
        Assert.Equal(ReadinessStatus.InsufficientData, belowCoverage.Readiness);

        var mediumCoverage = MasteryReadinessCalculator.AggregateChildren(
            Enumerable.Repeat(sufficient, 8)
                .Concat(Enumerable.Repeat(insufficient, 2))
                .ToArray());
        Assert.Equal(ReadinessConfidence.Medium, mediumCoverage.Confidence);
        Assert.Equal(ReadinessStatus.Ready, mediumCoverage.Readiness);

        var highCoverage = MasteryReadinessCalculator.AggregateChildren(
            Enumerable.Repeat(sufficient, 9)
                .Concat([insufficient])
                .ToArray());
        Assert.Equal(ReadinessConfidence.High, highCoverage.Confidence);
    }

    [Fact]
    public void AggregateCountsAChainOnlyOnceWhenOneEventTargetsMultipleLeaves()
    {
        var sharedChainId = Guid.NewGuid();
        var child = new MasteryCalculation(
            0.90m,
            ReadinessConfidence.Low,
            ReadinessStatus.InsufficientData,
            1,
            2m,
            new HashSet<Guid> { sharedChainId });

        var aggregate = MasteryReadinessCalculator.AggregateChildren([child, child, child]);

        Assert.Equal(1, aggregate.EvidenceCount);
        Assert.Equal(ReadinessConfidence.Medium, aggregate.Confidence);
        Assert.Equal(ReadinessStatus.InsufficientData, aggregate.Readiness);
    }

    private static MasteryEvidenceInput Input(decimal score) =>
        new(
            Guid.NewGuid(),
            score,
            3,
            EvidenceType.Application,
            AssistanceLevel.Independent,
            EvidenceContext.Assessment,
            Now);

    private static decimal Weight(
        int difficulty,
        EvidenceType evidenceType,
        AssistanceLevel assistance,
        EvidenceContext context,
        DateTimeOffset occurredAtUtc) =>
        MasteryReadinessCalculator.CalculateWeight(
            new(
                Guid.NewGuid(),
                1m,
                difficulty,
                evidenceType,
                assistance,
                context,
                occurredAtUtc),
            Now);
}
