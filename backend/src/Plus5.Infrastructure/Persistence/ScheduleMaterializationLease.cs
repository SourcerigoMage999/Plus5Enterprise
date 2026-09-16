namespace Plus5.Infrastructure.Persistence;

internal sealed class ScheduleMaterializationLease
{
    public const int NameMaxLength = 100;
    public const int OwnerIdMaxLength = 100;

    private ScheduleMaterializationLease()
    {
    }

    public ScheduleMaterializationLease(
        string name,
        string ownerId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Name = Normalize(name, NameMaxLength, nameof(name));
        Acquire(ownerId, expiresAtUtc, updatedAtUtc);
    }

    public string Name { get; private set; } = string.Empty;
    public string? OwnerId { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool TryAcquire(
        string ownerId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        if (OwnerId is not null && ExpiresAtUtc > updatedAtUtc)
        {
            return false;
        }

        Acquire(ownerId, expiresAtUtc, updatedAtUtc);
        return true;
    }

    public bool TryRenew(
        string ownerId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        if (!string.Equals(OwnerId, ownerId, StringComparison.Ordinal)
            || ExpiresAtUtc <= updatedAtUtc)
        {
            return false;
        }

        Acquire(ownerId, expiresAtUtc, updatedAtUtc);
        return true;
    }

    public bool TryRelease(string ownerId, DateTimeOffset releasedAtUtc)
    {
        EnsureUtc(releasedAtUtc, nameof(releasedAtUtc));

        if (!string.Equals(OwnerId, ownerId, StringComparison.Ordinal))
        {
            return false;
        }

        OwnerId = null;
        ExpiresAtUtc = releasedAtUtc;
        UpdatedAtUtc = releasedAtUtc;
        return true;
    }

    private void Acquire(
        string ownerId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, updatedAtUtc);

        OwnerId = Normalize(ownerId, OwnerIdMaxLength, nameof(ownerId));
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string Normalize(string value, int maxLength, string parameterName)
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

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
