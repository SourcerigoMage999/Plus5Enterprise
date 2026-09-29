using Plus5.Domain.Teaching;

namespace Plus5.Domain.Materials;

public sealed class MaterialVersionCurriculumOutcome
{
    private MaterialVersionCurriculumOutcome()
    {
    }

    public MaterialVersionCurriculumOutcome(
        MaterialVersion materialVersion,
        CurriculumOutcome curriculumOutcome)
    {
        ArgumentNullException.ThrowIfNull(materialVersion);
        ArgumentNullException.ThrowIfNull(curriculumOutcome);
        materialVersion.EnsureDraft();

        MaterialVersionId = materialVersion.Id;
        CurriculumOutcomeId = curriculumOutcome.Id;
    }

    public Guid MaterialVersionId { get; private set; }

    public Guid CurriculumOutcomeId { get; private set; }
}
