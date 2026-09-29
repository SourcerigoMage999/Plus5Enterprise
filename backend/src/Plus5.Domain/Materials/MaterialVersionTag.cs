namespace Plus5.Domain.Materials;

public sealed class MaterialVersionTag
{
    public const int NameMaxLength = 64;

    private MaterialVersionTag()
    {
    }

    public MaterialVersionTag(MaterialVersion materialVersion, string name)
    {
        ArgumentNullException.ThrowIfNull(materialVersion);
        materialVersion.EnsureDraft();

        Name = MaterialGuard.RequiredText(name, NameMaxLength, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        MaterialVersionId = materialVersion.Id;
    }

    public Guid MaterialVersionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;
}
