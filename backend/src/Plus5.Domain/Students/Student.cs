namespace Plus5.Domain.Students;

public sealed class Student
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int NicknameMaxLength = 100;
    public const int SchoolNameMaxLength = 200;
    public const int GenderMaxLength = 64;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 32;

    private Student()
    {
    }

    public Student(
        Guid id,
        Guid teacherAccountId,
        Guid schoolGradeId,
        string firstName,
        string lastName,
        StudentStatus status,
        DateTimeOffset createdAtUtc,
        Guid? programId = null,
        DeliveryMode? deliveryMode = null,
        string? nickname = null,
        DateOnly? dateOfBirth = null,
        string? schoolName = null,
        string? gender = null,
        string? email = null,
        string? phone = null)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureIdentifier(schoolGradeId, nameof(schoolGradeId));
        EnsureDefinedStatus(status);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureOrganizationIsComplete(programId, deliveryMode);

        Id = id;
        TeacherAccountId = teacherAccountId;
        SchoolGradeId = schoolGradeId;
        FirstName = NormalizeRequiredText(firstName, FirstNameMaxLength, nameof(firstName));
        LastName = NormalizeRequiredText(lastName, LastNameMaxLength, nameof(lastName));
        Status = status;
        ProgramId = programId;
        DeliveryMode = deliveryMode;
        Nickname = NormalizeOptionalText(nickname, NicknameMaxLength, nameof(nickname));
        DateOfBirth = dateOfBirth;
        SchoolName = NormalizeOptionalText(schoolName, SchoolNameMaxLength, nameof(schoolName));
        Gender = NormalizeOptionalText(gender, GenderMaxLength, nameof(gender));
        Email = NormalizeOptionalText(email, EmailMaxLength, nameof(email));
        Phone = NormalizeOptionalText(phone, PhoneMaxLength, nameof(phone));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TeacherAccountId { get; private set; }

    public Guid SchoolGradeId { get; private set; }

    public Guid? ProgramId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Nickname { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public string? SchoolName { get; private set; }

    public string? Gender { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public DeliveryMode? DeliveryMode { get; private set; }

    public StudentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));

        if (archivedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(archivedAtUtc),
                "Archive timestamp cannot precede the last update.");
        }

        Status = StudentStatus.Inactive;
        ArchivedAtUtc = archivedAtUtc;
        UpdatedAtUtc = archivedAtUtc;
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsureDefinedStatus(StudentStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static void EnsureOrganizationIsComplete(
        Guid? programId,
        DeliveryMode? deliveryMode)
    {
        if (programId == Guid.Empty)
        {
            throw new ArgumentException("Program identifier cannot be empty.", nameof(programId));
        }

        if (deliveryMode.HasValue && !Enum.IsDefined(deliveryMode.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryMode));
        }

        if (programId.HasValue != deliveryMode.HasValue)
        {
            throw new ArgumentException(
                "Program and delivery mode must either both be provided or both be omitted.",
                nameof(programId));
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
