namespace Plus5.Domain.Teaching;

public sealed class Program
{
    public const int NameMaxLength = 160;

    private Program()
    {
    }

    public Program(
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
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TeacherAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

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

    private static string NormalizeRequiredText(string value, int maxLength, string parameterName)
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
}
