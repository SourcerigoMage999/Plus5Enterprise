namespace Plus5.Domain.Scheduling;

public sealed class RecurringSessionSeries
{
    public const int TimeZoneIdMaxLength = 100;
    public const int OnlineMeetingUrlMaxLength = 2048;

    private RecurringSessionSeries()
    {
    }

    public RecurringSessionSeries(
        Guid id,
        Guid teacherAccountId,
        RecurringSessionSeriesKind kind,
        Guid contextId,
        DayOfWeek dayOfWeek,
        DateOnly startsOn,
        DateOnly endsOn,
        TimeOnly localStartTime,
        TimeOnly localEndTime,
        string timeZoneId,
        DateTimeOffset createdAtUtc,
        Guid? locationId = null,
        string? onlineMeetingUrl = null,
        Guid? previousSeriesId = null)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(teacherAccountId, nameof(teacherAccountId));
        EnsureIdentifier(contextId, nameof(contextId));
        EnsureDefinedKind(kind);
        EnsureDayOfWeek(dayOfWeek);
        EnsureDateRange(startsOn, endsOn);
        EnsureTimeRange(localStartTime, localEndTime);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureLocation(locationId, onlineMeetingUrl);

        if (previousSeriesId == Guid.Empty || previousSeriesId == id)
        {
            throw new ArgumentException("Previous series identifier is invalid.", nameof(previousSeriesId));
        }

        Id = id;
        TeacherAccountId = teacherAccountId;
        Kind = kind;
        GroupId = kind == RecurringSessionSeriesKind.RegularGroupSchedule ? contextId : null;
        StudentId = kind == RecurringSessionSeriesKind.IndividualRecurrence ? contextId : null;
        DayOfWeek = dayOfWeek;
        StartsOn = startsOn;
        EndsOn = endsOn;
        LocalStartTime = localStartTime;
        LocalEndTime = localEndTime;
        TimeZoneId = NormalizeRequiredText(timeZoneId, TimeZoneIdMaxLength, nameof(timeZoneId));
        LocationId = locationId;
        OnlineMeetingUrl = NormalizeOnlineMeetingUrl(onlineMeetingUrl);
        PreviousSeriesId = previousSeriesId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TeacherAccountId { get; private set; }
    public RecurringSessionSeriesKind Kind { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid? StudentId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public TimeOnly LocalStartTime { get; private set; }
    public TimeOnly LocalEndTime { get; private set; }
    public string TimeZoneId { get; private set; } = string.Empty;
    public Guid? LocationId { get; private set; }
    public string? OnlineMeetingUrl { get; private set; }
    public Guid? PreviousSeriesId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SupersededAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Supersede(DateOnly finalEffectiveDate, DateTimeOffset supersededAtUtc)
    {
        EnsureUtc(supersededAtUtc, nameof(supersededAtUtc));

        if (SupersededAtUtc.HasValue)
        {
            throw new InvalidOperationException("Series has already been superseded.");
        }

        if (finalEffectiveDate < StartsOn || finalEffectiveDate > EndsOn)
        {
            throw new ArgumentOutOfRangeException(nameof(finalEffectiveDate));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(supersededAtUtc, CreatedAtUtc);

        EndsOn = finalEffectiveDate;
        SupersededAtUtc = supersededAtUtc;
    }

    private static void EnsureDefinedKind(RecurringSessionSeriesKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void EnsureDayOfWeek(DayOfWeek dayOfWeek)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        }
    }

    private static void EnsureDateRange(DateOnly startsOn, DateOnly endsOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(endsOn, startsOn);
    }

    private static void EnsureTimeRange(TimeOnly start, TimeOnly end)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(end, start);
    }

    private static void EnsureLocation(Guid? locationId, string? onlineMeetingUrl)
    {
        if (locationId == Guid.Empty)
        {
            throw new ArgumentException("Location identifier is invalid.", nameof(locationId));
        }

        if (locationId.HasValue && !string.IsNullOrWhiteSpace(onlineMeetingUrl))
        {
            throw new ArgumentException("A series cannot have both a physical location and an online meeting URL.");
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

    private static string NormalizeRequiredText(string value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value is required and may contain at most {maxLength} characters.", parameterName);
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
