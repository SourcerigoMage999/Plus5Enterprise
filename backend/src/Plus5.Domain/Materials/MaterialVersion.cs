namespace Plus5.Domain.Materials;

public sealed class MaterialVersion
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MaterialTypeCodeMaxLength = 64;
    public const int SubjectMaxLength = 160;
    public const int LanguageCodeMaxLength = 32;

    private MaterialVersion()
    {
    }

    public MaterialVersion(
        Guid id,
        Material material,
        int versionNumber,
        string title,
        string materialTypeCode,
        DateTimeOffset createdAtUtc,
        string? description = null,
        string? subject = null,
        string? languageCode = null)
    {
        ArgumentNullException.ThrowIfNull(material);
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        if (material.Status != MaterialStatus.Active)
        {
            throw new InvalidOperationException(
                "A draft version cannot be created for an archived material.");
        }

        Id = id;
        MaterialId = material.Id;
        VersionNumber = versionNumber;
        CreatedByTeacherId = material.OwnerTeacherId;
        Title = MaterialGuard.RequiredText(title, TitleMaxLength, nameof(title));
        Description = MaterialGuard.OptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        MaterialTypeCode = MaterialGuard.Code(
            materialTypeCode,
            MaterialTypeCodeMaxLength,
            nameof(materialTypeCode));
        Subject = MaterialGuard.OptionalText(subject, SubjectMaxLength, nameof(subject));
        LanguageCode = MaterialGuard.OptionalText(
            languageCode,
            LanguageCodeMaxLength,
            nameof(languageCode));
        Status = MaterialVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialId { get; private set; }

    public int VersionNumber { get; private set; }

    public Guid CreatedByTeacherId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string MaterialTypeCode { get; private set; } = string.Empty;

    public string? Subject { get; private set; }

    public string? LanguageCode { get; private set; }

    public MaterialVersionStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public DateTimeOffset? SupersededAtUtc { get; private set; }

    public void UpdateDraft(
        string title,
        string materialTypeCode,
        string? description = null,
        string? subject = null,
        string? languageCode = null)
    {
        EnsureDraft();
        Title = MaterialGuard.RequiredText(title, TitleMaxLength, nameof(title));
        Description = MaterialGuard.OptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        MaterialTypeCode = MaterialGuard.Code(
            materialTypeCode,
            MaterialTypeCodeMaxLength,
            nameof(materialTypeCode));
        Subject = MaterialGuard.OptionalText(subject, SubjectMaxLength, nameof(subject));
        LanguageCode = MaterialGuard.OptionalText(
            languageCode,
            LanguageCodeMaxLength,
            nameof(languageCode));
    }

    public void Activate(MaterialFile file, DateTimeOffset activatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(file);
        EnsureDraft();
        MaterialGuard.Utc(activatedAtUtc, nameof(activatedAtUtc));
        if (activatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activatedAtUtc),
                "Activation cannot precede version creation.");
        }

        if (file.MaterialVersionId != Id || file.Status != MaterialFileStatus.Clean)
        {
            throw new InvalidOperationException(
                "A version can be activated only with its own clean file.");
        }

        Status = MaterialVersionStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
    }

    public void Supersede(DateTimeOffset supersededAtUtc)
    {
        MaterialGuard.Utc(supersededAtUtc, nameof(supersededAtUtc));
        if (Status != MaterialVersionStatus.Active)
        {
            throw new InvalidOperationException("Only an active version can be superseded.");
        }

        if (supersededAtUtc < ActivatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(supersededAtUtc),
                "Supersession cannot precede activation.");
        }

        Status = MaterialVersionStatus.Superseded;
        SupersededAtUtc = supersededAtUtc;
    }

    public MaterialVersion CreateRestoredDraft(
        Guid newVersionId,
        Material material,
        int newVersionNumber,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (material.Id != MaterialId)
        {
            throw new ArgumentException(
                "A restored version must belong to the same material.",
                nameof(material));
        }

        if (newVersionNumber <= VersionNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newVersionNumber),
                "A restore creates a new, later version number.");
        }

        return new MaterialVersion(
            newVersionId,
            material,
            newVersionNumber,
            Title,
            MaterialTypeCode,
            createdAtUtc,
            Description,
            Subject,
            LanguageCode);
    }

    private void EnsureDraft()
    {
        if (Status != MaterialVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "An active or superseded material version is immutable.");
        }
    }
}
