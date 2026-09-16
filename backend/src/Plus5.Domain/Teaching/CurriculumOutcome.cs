namespace Plus5.Domain.Teaching;

public sealed class CurriculumOutcome
{
    public const int OfficialCodeMaxLength = 128;
    public const int SourceAuthorityMaxLength = 200;
    public const int SourceReferenceMaxLength = 1024;
    public const int TitleMaxLength = 500;
    public const int DescriptionMaxLength = 4000;

    private CurriculumOutcome()
    {
    }

    public CurriculumOutcome(
        Guid id,
        Curriculum curriculum,
        string title,
        int sortOrder,
        CurriculumOutcome? parentOutcome = null,
        CurriculumOutcome? supersedesOutcome = null,
        Curriculum? supersededOutcomeCurriculum = null,
        string? officialCode = null,
        string? sourceAuthority = null,
        string? sourceReference = null,
        string? description = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(curriculum);

        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        if (parentOutcome is not null && parentOutcome.CurriculumId != curriculum.Id)
        {
            throw new ArgumentException(
                "Parent outcome must belong to the same curriculum version.",
                nameof(parentOutcome));
        }

        if (supersedesOutcome is not null)
        {
            ArgumentNullException.ThrowIfNull(supersededOutcomeCurriculum);

            if (supersedesOutcome.Id == id)
            {
                throw new ArgumentException(
                    "An outcome cannot supersede itself.",
                    nameof(supersedesOutcome));
            }

            if (supersededOutcomeCurriculum.Id != supersedesOutcome.CurriculumId)
            {
                throw new ArgumentException(
                    "The predecessor curriculum must own the superseded outcome.",
                    nameof(supersededOutcomeCurriculum));
            }

            if (!string.Equals(
                    curriculum.Code,
                    supersededOutcomeCurriculum.Code,
                    StringComparison.Ordinal)
                || string.Equals(
                    curriculum.Version,
                    supersededOutcomeCurriculum.Version,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Supersession lineage must stay in the same curriculum family and cross versions.",
                    nameof(supersedesOutcome));
            }
        }
        else if (supersededOutcomeCurriculum is not null)
        {
            throw new ArgumentException(
                "A predecessor curriculum requires a superseded outcome.",
                nameof(supersededOutcomeCurriculum));
        }

        var normalizedOfficialCode = NormalizeOfficialCode(
            officialCode,
            nameof(officialCode));
        var normalizedSourceAuthority = NormalizeOptionalText(
            sourceAuthority,
            SourceAuthorityMaxLength,
            nameof(sourceAuthority));
        var normalizedSourceReference = NormalizeOptionalText(
            sourceReference,
            SourceReferenceMaxLength,
            nameof(sourceReference));

        if (normalizedOfficialCode is not null
            && (normalizedSourceAuthority is null || normalizedSourceReference is null))
        {
            throw new ArgumentException(
                "An official code requires its source authority and source reference.",
                nameof(officialCode));
        }

        Id = id;
        CurriculumId = curriculum.Id;
        ParentOutcomeId = parentOutcome?.Id;
        SupersedesOutcomeId = supersedesOutcome?.Id;
        OfficialCode = normalizedOfficialCode;
        SourceAuthority = normalizedSourceAuthority;
        SourceReference = normalizedSourceReference;
        Title = NormalizeRequiredText(title, TitleMaxLength, nameof(title));
        Description = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }

    public Guid CurriculumId { get; private set; }

    public Guid? ParentOutcomeId { get; private set; }

    public Guid? SupersedesOutcomeId { get; private set; }

    public string? OfficialCode { get; private set; }

    public string? SourceAuthority { get; private set; }

    public string? SourceReference { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value is required and may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOfficialCode(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value)
            || value.Length > OfficialCodeMaxLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Official code must be published verbatim, without surrounding whitespace, and may contain at most {OfficialCodeMaxLength} characters.",
                parameterName);
        }

        return value;
    }
}
