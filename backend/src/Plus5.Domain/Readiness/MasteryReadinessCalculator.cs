using Plus5.Domain.Evidence;

namespace Plus5.Domain.Readiness;

public sealed record MasteryEvidenceInput(
    Guid ChainId,
    decimal PerformanceScore,
    int Difficulty,
    EvidenceType EvidenceType,
    AssistanceLevel AssistanceLevel,
    EvidenceContext EvidenceContext,
    DateTimeOffset OccurredAtUtc);

public sealed record MasteryCalculation(
    decimal? Score,
    ReadinessConfidence Confidence,
    ReadinessStatus Readiness,
    int EvidenceCount,
    decimal EffectiveEvidenceWeight,
    IReadOnlySet<Guid> EvidenceChainIds);

public static class MasteryReadinessCalculator
{
    public const string AlgorithmVersion = "readiness-v1";
    public const double HalfLifeDays = 90d;
    public const decimal RecencyFloor = 0.10m;
    public const decimal MinimumReadinessWeight = 2.0m;
    public const int MinimumReadinessChains = 2;
    public const decimal MinimumCoverage = 0.70m;
    public const decimal HighCoverage = 0.85m;

    public static MasteryCalculation CalculateLeaf(
        IReadOnlyCollection<MasteryEvidenceInput> evidence,
        DateTimeOffset calculatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        if (evidence.Count == 0)
        {
            return NoData();
        }

        var distinctEvidence = evidence
            .GroupBy(item => item.ChainId)
            .Select(group => group.Single())
            .ToArray();
        var weightedScore = 0m;
        var effectiveWeight = 0m;
        foreach (var item in distinctEvidence)
        {
            ValidateEvidence(item);
            var weight = CalculateWeight(item, calculatedAtUtc);
            weightedScore += item.PerformanceScore * weight;
            effectiveWeight += weight;
        }

        var score = weightedScore / effectiveWeight;
        var confidence = CalculateConfidence(effectiveWeight, distinctEvidence.Length);
        return new(
            score,
            confidence,
            Classify(score, confidence, effectiveWeight, distinctEvidence.Length),
            distinctEvidence.Length,
            effectiveWeight,
            distinctEvidence.Select(item => item.ChainId).ToHashSet());
    }

    public static MasteryCalculation AggregateChildren(
        IReadOnlyCollection<MasteryCalculation> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            return NoData();
        }

        var withScore = children.Where(child => child.Score.HasValue).ToArray();
        if (withScore.Length == 0)
        {
            return NoData();
        }

        var sufficientlyCovered = children.Count(child =>
            child.Readiness != ReadinessStatus.InsufficientData);
        var coverage = (decimal)sufficientlyCovered / children.Count;
        var score = withScore.Average(child => child.Score!.Value);
        var evidenceChainIds = withScore
            .SelectMany(child => child.EvidenceChainIds)
            .ToHashSet();
        var evidenceCount = evidenceChainIds.Count;
        var effectiveWeight = withScore.Sum(child => child.EffectiveEvidenceWeight);
        var confidence = CalculateConfidence(effectiveWeight, evidenceCount);
        if (coverage < HighCoverage && confidence == ReadinessConfidence.High)
        {
            confidence = ReadinessConfidence.Medium;
        }

        var readiness = coverage < MinimumCoverage
            ? ReadinessStatus.InsufficientData
            : Classify(score, confidence, effectiveWeight, evidenceCount);
        return new(
            score,
            confidence,
            readiness,
            evidenceCount,
            effectiveWeight,
            evidenceChainIds);
    }

    public static decimal CalculateWeight(
        MasteryEvidenceInput evidence,
        DateTimeOffset calculatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        ValidateEvidence(evidence);

        var ageDays = (calculatedAtUtc - evidence.OccurredAtUtc).TotalDays;
        var recency = Math.Max(
            (double)RecencyFloor,
            Math.Pow(0.5d, ageDays / HalfLifeDays));
        return DifficultyWeight(evidence.Difficulty)
            * EvidenceTypeWeight(evidence.EvidenceType)
            * AssistanceWeight(evidence.AssistanceLevel)
            * ContextWeight(evidence.EvidenceContext)
            * (decimal)recency;
    }

    private static ReadinessConfidence CalculateConfidence(decimal weight, int evidenceCount)
    {
        if (evidenceCount == 0)
        {
            return ReadinessConfidence.NoData;
        }

        if (weight < 1m)
        {
            return ReadinessConfidence.VeryLow;
        }

        if (weight < 2m)
        {
            return ReadinessConfidence.Low;
        }

        if (weight < 4m || evidenceCount < 3)
        {
            return ReadinessConfidence.Medium;
        }

        return ReadinessConfidence.High;
    }

    private static ReadinessStatus Classify(
        decimal score,
        ReadinessConfidence confidence,
        decimal effectiveWeight,
        int evidenceCount)
    {
        if (effectiveWeight < MinimumReadinessWeight
            || evidenceCount < MinimumReadinessChains)
        {
            return ReadinessStatus.InsufficientData;
        }

        if (score < 0.60m)
        {
            return ReadinessStatus.NeedsWork;
        }

        if (score < 0.75m)
        {
            return ReadinessStatus.Developing;
        }

        if (confidence < ReadinessConfidence.Medium)
        {
            return ReadinessStatus.InsufficientData;
        }

        return score < 0.90m
            ? ReadinessStatus.Ready
            : ReadinessStatus.Strong;
    }

    private static decimal DifficultyWeight(int difficulty) => difficulty switch
    {
        1 => 0.80m,
        2 => 0.90m,
        3 => 1.00m,
        4 => 1.10m,
        5 => 1.20m,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    private static decimal EvidenceTypeWeight(EvidenceType evidenceType) => evidenceType switch
    {
        EvidenceType.Recognition => 0.75m,
        EvidenceType.Understanding => 0.90m,
        EvidenceType.Application => 1.00m,
        EvidenceType.Production => 1.15m,
        _ => throw new ArgumentOutOfRangeException(nameof(evidenceType)),
    };

    private static decimal AssistanceWeight(AssistanceLevel assistanceLevel) =>
        assistanceLevel switch
        {
            AssistanceLevel.Independent => 1.00m,
            AssistanceLevel.MinorAssistance => 0.85m,
            AssistanceLevel.SignificantAssistance => 0.60m,
            AssistanceLevel.NotObserved => 0.75m,
            _ => throw new ArgumentOutOfRangeException(nameof(assistanceLevel)),
        };

    private static decimal ContextWeight(EvidenceContext evidenceContext) =>
        evidenceContext switch
        {
            EvidenceContext.Lesson => 0.90m,
            EvidenceContext.Homework => 0.85m,
            EvidenceContext.Assessment => 1.00m,
            EvidenceContext.IndependentPractice => 0.90m,
            _ => throw new ArgumentOutOfRangeException(nameof(evidenceContext)),
        };

    private static void ValidateEvidence(MasteryEvidenceInput evidence)
    {
        if (evidence.ChainId == Guid.Empty)
        {
            throw new ArgumentException("Evidence chain identifier is required.", nameof(evidence));
        }

        if (evidence.PerformanceScore is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(evidence));
        }

        EnsureUtc(evidence.OccurredAtUtc, nameof(evidence));
    }

    private static MasteryCalculation NoData() =>
        new(
            null,
            ReadinessConfidence.NoData,
            ReadinessStatus.InsufficientData,
            0,
            0m,
            new HashSet<Guid>());

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
