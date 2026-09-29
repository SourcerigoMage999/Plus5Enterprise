namespace Plus5.Domain.Materials;

public sealed class MaterialShare
{
    private MaterialShare()
    {
    }

    public MaterialShare(
        Material material,
        Guid sharedWithTeacherId,
        MaterialShareAccess permission,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(material);
        MaterialGuard.Identifier(sharedWithTeacherId, nameof(sharedWithTeacherId));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        EnsurePermission(permission);
        if (material.OwnerTeacherId == sharedWithTeacherId)
        {
            throw new ArgumentException(
                "A material owner cannot share the material with themselves.",
                nameof(sharedWithTeacherId));
        }

        if (material.Visibility != MaterialVisibility.Shared)
        {
            throw new InvalidOperationException(
                "A share grant can be created only for a shared material.");
        }

        MaterialId = material.Id;
        SharedWithTeacherId = sharedWithTeacherId;
        Permission = permission;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid MaterialId { get; private set; }

    public Guid SharedWithTeacherId { get; private set; }

    public MaterialShareAccess Permission { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ChangePermission(
        MaterialShareAccess permission,
        DateTimeOffset updatedAtUtc)
    {
        EnsurePermission(permission);
        MaterialGuard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "Update timestamp cannot precede the previous update.");
        }

        Permission = permission;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void EnsurePermission(MaterialShareAccess permission)
    {
        if (!Enum.IsDefined(permission))
        {
            throw new ArgumentOutOfRangeException(nameof(permission));
        }
    }
}
