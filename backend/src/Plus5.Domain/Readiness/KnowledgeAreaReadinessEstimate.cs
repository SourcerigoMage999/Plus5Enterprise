namespace Plus5.Domain.Readiness;

public sealed class KnowledgeAreaReadinessEstimate
{
    private KnowledgeAreaReadinessEstimate()
    {
    }

    public KnowledgeAreaReadinessEstimate(
        Guid studentId,
        Guid knowledgeAreaId,
        MasteryCalculation calculation,
        DateTimeOffset calculatedAtUtc,
        string algorithmVersion)
    {
        if (studentId == Guid.Empty || knowledgeAreaId == Guid.Empty)
        {
            throw new ArgumentException("Student and knowledge area identifiers are required.");
        }

        StudentId = studentId;
        KnowledgeAreaId = knowledgeAreaId;
        Refresh(calculation, calculatedAtUtc, algorithmVersion);
    }

    public Guid StudentId { get; private set; }
    public Guid KnowledgeAreaId { get; private set; }
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
}
