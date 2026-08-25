namespace Plus5.Domain.Groups;

public sealed class Group
{
    public const int NameMaxLength = 160;
    public const int DescriptionMaxLength = 1000;

    private Group()
    {
    }

    public Group(
        Guid id,
        Guid teacherAccountId,
        Guid programId,
        Guid schoolGradeId,
        string name,
        int capacity,
        GroupStatus status,
        DateTimeOffset createdAtUtc,
        string? description = null)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureIdentifier(programId, nameof(programId));
        EnsureIdentifier(schoolGradeId, nameof(schoolGradeId));
        EnsureCapacity(capacity, 0);
        EnsureDefinedStatus(status);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        TeacherAccountId = teacherAccountId;
        ProgramId = programId;
        SchoolGradeId = schoolGradeId;
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeOptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        Capacity = capacity;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TeacherAccountId { get; private set; }

    public Guid ProgramId { get; private set; }

    public Guid SchoolGradeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int Capacity { get; private set; }

    public GroupStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void ChangeCapacity(
        int capacity,
        int activeMemberCount,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanUpdate(updatedAtUtc);
        EnsureCapacity(capacity, activeMemberCount);

        Capacity = capacity;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RecordMembershipChange(
        int activeMemberCountAfterChange,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanUpdate(updatedAtUtc);
        EnsureCapacity(Capacity, activeMemberCountAfterChange);

        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(int activeMemberCount, DateTimeOffset archivedAtUtc)
    {
        EnsureCanUpdate(archivedAtUtc);

        if (activeMemberCount != 0)
        {
            throw new InvalidOperationException(
                "A group with active memberships cannot be archived.");
        }

        Status = GroupStatus.Inactive;
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

    private static void EnsureCapacity(int capacity, int activeMemberCount)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Capacity must be greater than zero.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(activeMemberCount);

        if (capacity < activeMemberCount)
        {
            throw new InvalidOperationException(
                "Capacity cannot be lower than the active member count.");
        }
    }

    private static void EnsureDefinedStatus(GroupStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private void EnsureCanUpdate(DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        if (ArchivedAtUtc.HasValue)
        {
            throw new InvalidOperationException("An archived group cannot be changed.");
        }

        if (updatedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "Update timestamp cannot precede the last update.");
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
