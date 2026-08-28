using Plus5.Domain.Students;

namespace Plus5.Domain.Scheduling;

public sealed class Session
{
    public const int TitleMaxLength = 200;
    public const int NotesMaxLength = 2000;
    public const int TimeZoneIdMaxLength = RecurringSessionSeries.TimeZoneIdMaxLength;
    public const int OnlineMeetingUrlMaxLength = RecurringSessionSeries.OnlineMeetingUrlMaxLength;

    private Session()
    {
    }

    public Session(
        Guid id,
        Guid teacherAccountId,
        DeliveryMode deliveryMode,
        Guid contextId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string timeZoneId,
        DateTimeOffset createdAtUtc,
        string? title = null,
        string? notes = null,
        Guid? locationId = null,
        string? onlineMeetingUrl = null,
        Guid? recurringSessionSeriesId = null,
        DateOnly? seriesOccurrenceDate = null)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureIdentifier(contextId, nameof(contextId));
        EnsureDefinedDeliveryMode(deliveryMode);
        EnsureTimeRange(startsAtUtc, endsAtUtc);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureSeriesOccurrence(recurringSessionSeriesId, seriesOccurrenceDate);
        EnsureLocation(locationId, onlineMeetingUrl);

        Id = id;
        TeacherAccountId = teacherAccountId;
        DeliveryMode = deliveryMode;
        GroupId = deliveryMode == DeliveryMode.Group ? contextId : null;
        StudentId = deliveryMode == DeliveryMode.Individual ? contextId : null;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        TimeZoneId = NormalizeRequiredText(timeZoneId, TimeZoneIdMaxLength, nameof(timeZoneId));
        Title = NormalizeOptionalText(title, TitleMaxLength, nameof(title));
        Notes = NormalizeOptionalText(notes, NotesMaxLength, nameof(notes));
        LocationId = locationId;
        OnlineMeetingUrl = NormalizeOnlineMeetingUrl(onlineMeetingUrl);
        RecurringSessionSeriesId = recurringSessionSeriesId;
        SeriesOccurrenceDate = seriesOccurrenceDate;
        Status = SessionStatus.Scheduled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public DeliveryMode DeliveryMode { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid? StudentId { get; private set; }
    public Guid? RecurringSessionSeriesId { get; private set; }
    public DateOnly? SeriesOccurrenceDate { get; private set; }
    public string? Title { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public string TimeZoneId { get; private set; } = string.Empty;
    public Guid? LocationId { get; private set; }
    public string? OnlineMeetingUrl { get; private set; }
    public SessionStatus Status { get; private set; }
    public bool IsSeriesException { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Reschedule(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset updatedAtUtc)
    {
        EnsureScheduled();
        EnsureCanUpdate(updatedAtUtc);
        EnsureTimeRange(startsAtUtc, endsAtUtc);

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        IsSeriesException = RecurringSessionSeriesId.HasValue;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Start(DateTimeOffset updatedAtUtc)
    {
        EnsureScheduled();
        EnsureCanUpdate(updatedAtUtc);
        Status = SessionStatus.InProgress;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Complete(DateTimeOffset updatedAtUtc)
    {
        EnsureCanUpdate(updatedAtUtc);
        if (Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress session can be completed.");
        }

        Status = SessionStatus.Held;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureCanUpdate(cancelledAtUtc);
        if (Status is SessionStatus.Held or SessionStatus.Cancelled)
        {
            throw new InvalidOperationException("A held or cancelled session cannot be cancelled.");
        }

        Status = SessionStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        UpdatedAtUtc = cancelledAtUtc;
    }

    private void EnsureScheduled()
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a scheduled session can be changed this way.");
        }
    }

    private void EnsureCanUpdate(DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        ArgumentOutOfRangeException.ThrowIfLessThan(updatedAtUtc, UpdatedAtUtc);
    }

    private static void EnsureTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        EnsureUtc(start, nameof(start));
        EnsureUtc(end, nameof(end));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(end, start);
    }

    private static void EnsureSeriesOccurrence(Guid? seriesId, DateOnly? occurrenceDate)
    {
        if (seriesId == Guid.Empty || seriesId.HasValue != occurrenceDate.HasValue)
        {
            throw new ArgumentException("Series identifier and occurrence date must be supplied together.");
        }
    }

    private static void EnsureLocation(Guid? locationId, string? onlineMeetingUrl)
    {
        if (locationId == Guid.Empty)
        {
            throw new ArgumentException("Location identifier is invalid.", nameof(locationId));
        }

        if (locationId.HasValue && !string.IsNullOrWhiteSpace(onlineMeetingUrl))
        {
            throw new ArgumentException("A session cannot have both a physical location and an online meeting URL.");
        }
    }

    private static string? NormalizeOnlineMeetingUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > OnlineMeetingUrlMaxLength
            || !Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Online meeting URL must be an absolute HTTPS URL.", nameof(value));
        }

        return normalized;
    }

    private static void EnsureDefinedDeliveryMode(DeliveryMode deliveryMode)
    {
        if (!Enum.IsDefined(deliveryMode))
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryMode));
        }
    }

    private static string NormalizeRequiredText(string value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value is required and may contain at most {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value may contain at most {maxLength} characters.", parameterName);
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
