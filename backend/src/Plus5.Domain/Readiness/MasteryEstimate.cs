namespace Plus5.Domain.Readiness;

public sealed class MasteryEstimate
{
    public const int AlgorithmVersionMaxLength = 32;

    private MasteryEstimate()
    {
    }

    public MasteryEstimate(
        Guid studentId,
        Guid knowledgeComponentId,
        MasteryCalculation calculation,
        DateTimeOffset calculatedAtUtc,
        string algorithmVersion)
    {
        StudentId = EnsureIdentifier(studentId, nameof(studentId));
        KnowledgeComponentId = EnsureIdentifier(knowledgeComponentId, nameof(knowledgeComponentId));
        Refresh(calculation, calculatedAtUtc, algorithmVersion);
    }

    public Guid StudentId { get; private set; }
    public Guid KnowledgeComponentId { get; private set; }
    public decimal? Score { get; private set; }
    public ReadinessConfidence Confidence { get; private set; }
    public ReadinessStatus Readiness { get; private set; }
    public int EvidenceCount { get; private set; }
    public decimal EffectiveEvidenceWeight { get; private set; }
    public DateTimeOffset CalculatedAtUtc { get; private set; }
    public string AlgorithmVersion { get; private set; } = string.Empty;

    public void Refresh(
        MasteryCalculation calculation,
        DateTimeOffset calculatedAtUtc,
        string algorithmVersion)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        EstimateValidation.Validate(calculation, calculatedAtUtc, algorithmVersion);
        Score = calculation.Score;
        Confidence = calculation.Confidence;
        Readiness = calculation.Readiness;
        EvidenceCount = calculation.EvidenceCount;
        EffectiveEvidenceWeight = calculation.EffectiveEvidenceWeight;
        CalculatedAtUtc = calculatedAtUtc;
        AlgorithmVersion = algorithmVersion;
    }

    private static Guid EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return value;
    }
}

internal static class EstimateValidation
{
    public static void Validate(
        MasteryCalculation calculation,
        DateTimeOffset calculatedAtUtc,
        string algorithmVersion)
    {
        if (calculation.Score is < 0m or > 1m
            || calculation.EvidenceCount < 0
            || calculation.EffectiveEvidenceWeight < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(calculation));
        }

        if (calculation.EvidenceChainIds is null
            || calculation.EvidenceChainIds.Count != calculation.EvidenceCount
            || calculation.EvidenceChainIds.Contains(Guid.Empty))
        {
            throw new ArgumentException(
                "Estimate evidence-chain identities are inconsistent.",
                nameof(calculation));
        }

        var isNoData = calculation.Score is null
            && calculation.Confidence == ReadinessConfidence.NoData
            && calculation.Readiness == ReadinessStatus.InsufficientData
            && calculation.EvidenceCount == 0
            && calculation.EffectiveEvidenceWeight == 0m;
        var hasData = calculation.Score is not null
            && calculation.Confidence != ReadinessConfidence.NoData;
        if (!isNoData && !hasData)
        {
            throw new ArgumentException(
                "Estimate score, confidence, readiness, count, and weight are inconsistent.",
                nameof(calculation));
        }

        if (calculatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(calculatedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(algorithmVersion)
            || algorithmVersion.Length > MasteryEstimate.AlgorithmVersionMaxLength)
        {
            throw new ArgumentException("Algorithm version is required.", nameof(algorithmVersion));
        }
    }
}
