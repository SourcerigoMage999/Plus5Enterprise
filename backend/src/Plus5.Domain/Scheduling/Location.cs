namespace Plus5.Domain.Scheduling;

public sealed class Location
{
    public const int NameMaxLength = 160;

    private Location()
    {
    }

    public Location(
        Guid id,
        Guid teacherAccountId,
        string name,
        DateTimeOffset createdAtUtc)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        TeacherAccountId = teacherAccountId;
        Name = NormalizeName(name);
        NormalizedName = Name.ToUpperInvariant();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TeacherAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));

        if (ArchivedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Location is already archived.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(archivedAtUtc, CreatedAtUtc);

        ArchivedAtUtc = archivedAtUtc;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Location name is required and may contain at most {NameMaxLength} characters.",
                nameof(value));
        }

        return normalized;
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
