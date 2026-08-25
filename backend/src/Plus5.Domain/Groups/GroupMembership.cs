namespace Plus5.Domain.Groups;

public sealed class GroupMembership
{
    private GroupMembership()
    {
    }

    public GroupMembership(
        Guid id,
        Guid teacherAccountId,
        Guid groupId,
        Guid studentId,
        DateTimeOffset joinedAtUtc)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureIdentifier(groupId, nameof(groupId));
        EnsureIdentifier(studentId, nameof(studentId));
        EnsureUtc(joinedAtUtc, nameof(joinedAtUtc));

        Id = id;
        TeacherAccountId = teacherAccountId;
        GroupId = groupId;
        StudentId = studentId;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TeacherAccountId { get; private set; }

    public Guid GroupId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public DateTimeOffset? LeftAtUtc { get; private set; }

    public bool IsActive => !LeftAtUtc.HasValue;

    public void End(DateTimeOffset leftAtUtc)
    {
        EnsureUtc(leftAtUtc, nameof(leftAtUtc));

        if (LeftAtUtc.HasValue)
        {
            throw new InvalidOperationException("Membership has already ended.");
        }

        if (leftAtUtc < JoinedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leftAtUtc),
                "Membership cannot end before it starts.");
        }

        LeftAtUtc = leftAtUtc;
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
