namespace Plus5.Domain.Scheduling;

public sealed class ScheduleMaterializationIssue
{
    private ScheduleMaterializationIssue()
    {
    }

    public ScheduleMaterializationIssue(
        Guid id,
        Guid recurringSessionSeriesId,
        DateOnly occurrenceLocalDate,
        ScheduleMaterializationIssueType issueType,
        DateTimeOffset observedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(id));
        }

        if (recurringSessionSeriesId == Guid.Empty)
        {
            throw new ArgumentException("Series identifier is required.", nameof(recurringSessionSeriesId));
        }

        if (occurrenceLocalDate == default)
        {
            throw new ArgumentException("Occurrence date is required.", nameof(occurrenceLocalDate));
        }

        EnsureDefinedType(issueType);
        EnsureUtc(observedAtUtc, nameof(observedAtUtc));

        Id = id;
        RecurringSessionSeriesId = recurringSessionSeriesId;
        OccurrenceLocalDate = occurrenceLocalDate;
        IssueType = issueType;
        FirstSeenAtUtc = observedAtUtc;
        LastSeenAtUtc = observedAtUtc;
        AttemptCount = 1;
    }

    public Guid Id { get; private set; }
    public Guid RecurringSessionSeriesId { get; private set; }
    public DateOnly OccurrenceLocalDate { get; private set; }
    public ScheduleMaterializationIssueType IssueType { get; private set; }
    public DateTimeOffset FirstSeenAtUtc { get; private set; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public void RecordAgain(DateTimeOffset observedAtUtc)
    {
        EnsureUtc(observedAtUtc, nameof(observedAtUtc));
        ArgumentOutOfRangeException.ThrowIfLessThan(observedAtUtc, LastSeenAtUtc);

        LastSeenAtUtc = observedAtUtc;
        AttemptCount = checked(AttemptCount + 1);
        ResolvedAtUtc = null;
    }

    public bool Resolve(DateTimeOffset resolvedAtUtc)
    {
        EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        ArgumentOutOfRangeException.ThrowIfLessThan(resolvedAtUtc, LastSeenAtUtc);

        if (ResolvedAtUtc.HasValue)
        {
            return false;
        }

        ResolvedAtUtc = resolvedAtUtc;
        return true;
    }

    private static void EnsureDefinedType(ScheduleMaterializationIssueType issueType)
    {
        if (!Enum.IsDefined(issueType))
        {
            throw new ArgumentOutOfRangeException(nameof(issueType));
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
