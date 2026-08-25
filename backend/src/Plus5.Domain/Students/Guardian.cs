namespace Plus5.Domain.Students;

public sealed class Guardian
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int RelationshipMaxLength = 100;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 32;

    private Guardian()
    {
    }

    public Guardian(
        Guid id,
        Guid studentId,
        string firstName,
        string lastName,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        string? relationship = null,
        string? email = null,
        string? phone = null)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(studentId, nameof(studentId));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        StudentId = studentId;
        FirstName = NormalizeRequiredText(firstName, FirstNameMaxLength, nameof(firstName));
        LastName = NormalizeRequiredText(lastName, LastNameMaxLength, nameof(lastName));
        Relationship = NormalizeOptionalText(
            relationship,
            RelationshipMaxLength,
            nameof(relationship));
        Email = NormalizeOptionalText(email, EmailMaxLength, nameof(email));
        Phone = NormalizeOptionalText(phone, PhoneMaxLength, nameof(phone));
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Relationship { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public bool IsPrimary { get; private set; }

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
}
