namespace Plus5.Domain.Evidence;

public enum EvidenceType
{
    Recognition = 1,
    Understanding = 2,
    Application = 3,
    Production = 4,
}

public enum AssistanceLevel
{
    Independent = 1,
    MinorAssistance = 2,
    SignificantAssistance = 3,
    NotObserved = 4,
}

public enum EvidenceContext
{
    Lesson = 1,
    Homework = 2,
    Assessment = 3,
    IndependentPractice = 4,
}

public sealed class EvidenceMetadata
{
    public const int MinimumDifficulty = 1;
    public const int MaximumDifficulty = 5;

    public EvidenceMetadata(
        int difficulty,
        EvidenceType evidenceType,
        AssistanceLevel assistanceLevel,
        EvidenceContext evidenceContext)
    {
        if (difficulty is < MinimumDifficulty or > MaximumDifficulty)
        {
            throw new ArgumentOutOfRangeException(
                nameof(difficulty),
                $"Difficulty must be between {MinimumDifficulty} and {MaximumDifficulty}.");
        }

        EnsureDefined(evidenceType, nameof(evidenceType));
        EnsureDefined(assistanceLevel, nameof(assistanceLevel));
        EnsureDefined(evidenceContext, nameof(evidenceContext));

        Difficulty = difficulty;
        EvidenceType = evidenceType;
        AssistanceLevel = assistanceLevel;
        EvidenceContext = evidenceContext;
    }

    public int Difficulty { get; }

    public EvidenceType EvidenceType { get; }

    public AssistanceLevel AssistanceLevel { get; }

    public EvidenceContext EvidenceContext { get; }

    private static void EnsureDefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Unknown metadata code.");
        }
    }
}
