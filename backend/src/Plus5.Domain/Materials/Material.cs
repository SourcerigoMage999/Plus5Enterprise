namespace Plus5.Domain.Materials;

public sealed class Material
{
    private Material()
    {
    }

    public Material(Guid id, Guid ownerTeacherId, DateTimeOffset createdAtUtc)
    {
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Identifier(ownerTeacherId, nameof(ownerTeacherId));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        OwnerTeacherId = ownerTeacherId;
        Status = MaterialStatus.Active;
        Visibility = MaterialVisibility.Private;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OwnerTeacherId { get; private set; }

    public MaterialStatus Status { get; private set; }

    public MaterialVisibility Visibility { get; private set; }

    public Guid? CurrentVersionId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void SetCurrentVersion(MaterialVersion version, DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(version);
        EnsureMutable(updatedAtUtc);

        if (version.MaterialId != Id || version.Status != MaterialVersionStatus.Active)
        {
            throw new ArgumentException(
                "The current version must be an active version of this material.",
                nameof(version));
        }

        CurrentVersionId = version.Id;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetVisibility(MaterialVisibility visibility, DateTimeOffset updatedAtUtc)
    {
        EnsureMutable(updatedAtUtc);
        if (!Enum.IsDefined(visibility))
        {
            throw new ArgumentOutOfRangeException(nameof(visibility));
        }

        Visibility = visibility;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureMutable(archivedAtUtc);
        Status = MaterialStatus.Archived;
        ArchivedAtUtc = archivedAtUtc;
        UpdatedAtUtc = archivedAtUtc;
    }

    private void EnsureMutable(DateTimeOffset updatedAtUtc)
    {
        MaterialGuard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        if (Status == MaterialStatus.Archived)
        {
            throw new InvalidOperationException("An archived material cannot be changed.");
        }

        if (updatedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "Update timestamp cannot precede the last update.");
        }
    }
}
