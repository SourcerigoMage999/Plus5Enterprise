using System.Text;
using Plus5.Domain.Evidence;

namespace Plus5.Domain.Materials;

public sealed class AssessableTaskVersion
{
    public const int PromptMaxLength = 4000;
    public const int TaskTypeCodeMaxLength = 64;
    public const int AnswerMaxLength = 4000;
    public const int EvaluationCriterionMaxLength = 4000;
    public const int AuthoringPayloadMaxBytes = 16 * 1024;
    public const string SingleChoiceQuickCheckTypeCode = "SINGLE_CHOICE_QUICK_CHECK";
    public const decimal MaximumPoints = 99999999999999.9999m;

    private AssessableTaskVersion()
    {
    }

    public AssessableTaskVersion(
        Guid id,
        AssessableTask task,
        MaterialVersion materialVersion,
        int versionNumber,
        int sortOrder,
        string prompt,
        string taskTypeCode,
        int difficulty,
        EvidenceType evidenceType,
        decimal maxPoints,
        DateTimeOffset createdAtUtc,
        string? correctAnswer = null,
        string? evaluationCriterion = null,
        string? authoringPayloadJson = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(materialVersion);
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        materialVersion.EnsureDraft();

        if (task.MaterialId != materialVersion.MaterialId)
        {
            throw new ArgumentException(
                "The task and material version must belong to the same material.",
                nameof(materialVersion));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        if (difficulty is < EvidenceMetadata.MinimumDifficulty
            or > EvidenceMetadata.MaximumDifficulty)
        {
            throw new ArgumentOutOfRangeException(
                nameof(difficulty),
                $"Difficulty must be between {EvidenceMetadata.MinimumDifficulty} and {EvidenceMetadata.MaximumDifficulty}.");
        }

        if (!Enum.IsDefined(evidenceType))
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceType));
        }

        if (maxPoints <= 0 || maxPoints > MaximumPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPoints),
                $"Maximum points must be greater than zero and no greater than {MaximumPoints}.");
        }

        var normalizedAnswer = MaterialGuard.OptionalText(
            correctAnswer,
            AnswerMaxLength,
            nameof(correctAnswer));
        var normalizedCriterion = MaterialGuard.OptionalText(
            evaluationCriterion,
            EvaluationCriterionMaxLength,
            nameof(evaluationCriterion));
        if (normalizedAnswer is null && normalizedCriterion is null)
        {
            throw new ArgumentException(
                "An assessable task requires a correct answer or an evaluation criterion.",
                nameof(evaluationCriterion));
        }

        Id = id;
        MaterialId = task.MaterialId;
        AssessableTaskId = task.Id;
        MaterialVersionId = materialVersion.Id;
        VersionNumber = versionNumber;
        SortOrder = sortOrder;
        Prompt = MaterialGuard.RequiredText(prompt, PromptMaxLength, nameof(prompt));
        TaskTypeCode = MaterialGuard.Code(
            taskTypeCode,
            TaskTypeCodeMaxLength,
            nameof(taskTypeCode));
        Difficulty = difficulty;
        EvidenceType = evidenceType;
        CorrectAnswer = normalizedAnswer;
        EvaluationCriterion = normalizedCriterion;
        MaxPoints = maxPoints;
        AuthoringPayloadJson = NormalizeAuthoringPayload(taskTypeCode, authoringPayloadJson);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialId { get; private set; }

    public Guid AssessableTaskId { get; private set; }

    public Guid MaterialVersionId { get; private set; }

    public int VersionNumber { get; private set; }

    public int SortOrder { get; private set; }

    public string Prompt { get; private set; } = string.Empty;

    public string TaskTypeCode { get; private set; } = string.Empty;

    public int Difficulty { get; private set; }

    public EvidenceType EvidenceType { get; private set; }

    public string? CorrectAnswer { get; private set; }

    public string? EvaluationCriterion { get; private set; }

    public decimal MaxPoints { get; private set; }

    public string? AuthoringPayloadJson { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void UpdateSingleChoiceQuickCheck(
        MaterialVersion materialVersion,
        string prompt,
        int difficulty,
        EvidenceType evidenceType,
        string correctAnswer,
        string authoringPayloadJson)
    {
        ArgumentNullException.ThrowIfNull(materialVersion);
        materialVersion.EnsureDraft();
        if (materialVersion.Id != MaterialVersionId
            || TaskTypeCode != SingleChoiceQuickCheckTypeCode)
        {
            throw new InvalidOperationException("Only the owning Draft quick check can be updated.");
        }

        if (difficulty is < EvidenceMetadata.MinimumDifficulty
            or > EvidenceMetadata.MaximumDifficulty)
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        if (!Enum.IsDefined(evidenceType))
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceType));
        }

        Prompt = MaterialGuard.RequiredText(prompt, PromptMaxLength, nameof(prompt));
        Difficulty = difficulty;
        EvidenceType = evidenceType;
        CorrectAnswer = MaterialGuard.RequiredText(
            correctAnswer,
            AnswerMaxLength,
            nameof(correctAnswer));
        EvaluationCriterion = null;
        MaxPoints = 1m;
        AuthoringPayloadJson = NormalizeAuthoringPayload(TaskTypeCode, authoringPayloadJson);
    }

    private static string? NormalizeAuthoringPayload(string taskTypeCode, string? value)
    {
        if (taskTypeCode == SingleChoiceQuickCheckTypeCode)
        {
            var payload = MaterialGuard.RequiredText(
                value!,
                AuthoringPayloadMaxBytes,
                nameof(value));
            if (Encoding.UTF8.GetByteCount(payload) > AuthoringPayloadMaxBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return payload;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Only the single-choice quick check has an authoring payload.",
                nameof(value));
        }

        return null;
    }
}
