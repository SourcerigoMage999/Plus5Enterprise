using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Application.Scheduling;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed class EfScheduleEditingService(Plus5DbContext db, TimeProvider clock)
    : IScheduleEditingService
{
    public async Task<ScheduleEditPreviewResult> PreviewAsync(
        Guid owner,
        Guid sessionId,
        ScheduleEditCommand command,
        CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(owner, sessionId, command, cancellationToken);
        if (prepared.Value is null)
        {
            return new(prepared.Failure);
        }

        var conflict = await HasConflictAsync(owner, command.LocationId, prepared.Value,
            cancellationToken);
        return new(conflict ? ScheduleEditFailure.ScheduleConflict : ScheduleEditFailure.None,
            conflict);
    }

    public async Task<ScheduleEditResult> UpdateAsync(
        Guid owner,
        Guid sessionId,
        ScheduleEditCommand command,
        CancellationToken cancellationToken)
    {
        if (!Valid(owner, sessionId, command))
        {
            return new(null, ScheduleEditFailure.Invalid);
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var prepared = await PrepareAsync(owner, sessionId, command, cancellationToken);
            if (prepared.Value is null)
            {
                return new(null, prepared.Failure);
            }

            if (await HasConflictAsync(owner, command.LocationId, prepared.Value,
                cancellationToken))
            {
                return new(null, ScheduleEditFailure.ScheduleConflict);
            }

            var value = prepared.Value;
            db.Entry(value.Session).Property(session => session.RowVersion).OriginalValue = command.RowVersion;

            if ((ScheduleEditScope)command.Scope == ScheduleEditScope.OneOccurrence)
            {
                var occurrence = value.Occurrences[0];
                value.Session.UpdateDetails(
                    occurrence.Start,
                    occurrence.End,
                    command.Title,
                    command.Notes,
                    command.LocationId,
                    command.OnlineMeetingUrl,
                    clock.GetUtcNow());

                await db.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new(value.Session.Id, ScheduleEditFailure.None, 1);
            }

            var now = clock.GetUtcNow();
            var series = value.Series!;
            var effectiveDate = value.Session.SeriesOccurrenceDate!.Value;
            var successorEndsOn = series.EndsOn;
            var finalDate = effectiveDate.AddDays(-1);
            series.Supersede(finalDate < series.StartsOn ? series.StartsOn : finalDate, now);

            foreach (var future in value.ReplaceableSessions)
            {
                future.Cancel(now);
            }

            var contextId = series.GroupId ?? series.StudentId!.Value;
            var successor = new RecurringSessionSeries(
                Guid.NewGuid(),
                owner,
                series.Kind,
                contextId,
                series.DayOfWeek,
                effectiveDate,
                successorEndsOn,
                command.StartsAt,
                command.EndsAt,
                GroupScheduleGenerator.TimeZoneId,
                now,
                command.LocationId,
                command.OnlineMeetingUrl,
                series.Id);
            db.RecurringSessionSeries.Add(successor);

            var deliveryMode = series.Kind == RecurringSessionSeriesKind.RegularGroupSchedule
                ? DeliveryMode.Group
                : DeliveryMode.Individual;
            var created = value.Occurrences.Select(occurrence => new Session(
                Guid.NewGuid(),
                owner,
                deliveryMode,
                contextId,
                occurrence.Start,
                occurrence.End,
                GroupScheduleGenerator.TimeZoneId,
                now,
                value.Session.Title,
                value.Session.Notes,
                command.LocationId,
                command.OnlineMeetingUrl,
                successor.Id,
                occurrence.Date)).ToArray();
            db.Sessions.AddRange(created);

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var selected = created.Single(session => session.SeriesOccurrenceDate == effectiveDate);
            return new(selected.Id, ScheduleEditFailure.None, created.Length);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(null, ScheduleEditFailure.ConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        { Number: 2601 or 2627 or 1205 })
        {
            return new(null, ScheduleEditFailure.ConcurrencyConflict);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return new(null, ScheduleEditFailure.ConcurrencyConflict);
        }
        catch (InvalidOperationException exception) when (
            exception.InnerException?.InnerException is SqlException { Number: 1205 }
            || exception.InnerException is SqlException { Number: 1205 })
        {
            return new(null, ScheduleEditFailure.ConcurrencyConflict);
        }
    }

    public async Task<ScheduleEditFailure> CancelAsync(
        Guid owner,
        Guid sessionId,
        byte[] rowVersion,
        CancellationToken cancellationToken)
    {
        if (owner == Guid.Empty || sessionId == Guid.Empty || rowVersion is not { Length: 8 })
        {
            return ScheduleEditFailure.Invalid;
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var session = await db.Sessions.SingleOrDefaultAsync(
                item => item.TeacherAccountId == owner && item.Id == sessionId,
                cancellationToken);
            if (session is null)
            {
                return ScheduleEditFailure.NotFound;
            }

            if (session.Status is SessionStatus.Held or SessionStatus.Cancelled)
            {
                return ScheduleEditFailure.Unavailable;
            }

            db.Entry(session).Property(item => item.RowVersion).OriginalValue = rowVersion;
            session.Cancel(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return ScheduleEditFailure.None;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ScheduleEditFailure.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        { Number: 2601 or 2627 or 1205 })
        {
            return ScheduleEditFailure.ConcurrencyConflict;
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return ScheduleEditFailure.ConcurrencyConflict;
        }
        catch (InvalidOperationException exception) when (
            exception.InnerException?.InnerException is SqlException { Number: 1205 }
            || exception.InnerException is SqlException { Number: 1205 })
        {
            return ScheduleEditFailure.ConcurrencyConflict;
        }
    }

    private async Task<PreparedResult> PrepareAsync(
        Guid owner,
        Guid sessionId,
        ScheduleEditCommand command,
        CancellationToken cancellationToken)
    {
        if (!Valid(owner, sessionId, command))
        {
            return new(null, ScheduleEditFailure.Invalid);
        }

        var session = await db.Sessions.SingleOrDefaultAsync(
            item => item.TeacherAccountId == owner && item.Id == sessionId,
            cancellationToken);
        if (session is null)
        {
            return new(null, ScheduleEditFailure.NotFound);
        }

        if (session.Status != SessionStatus.Scheduled)
        {
            return new(null, ScheduleEditFailure.Unavailable);
        }

        if (!session.RowVersion.SequenceEqual(command.RowVersion))
        {
            return new(null, ScheduleEditFailure.ConcurrencyConflict);
        }

        if (command.LocationId.HasValue && !await db.Locations.AnyAsync(
            location => location.TeacherAccountId == owner
                && location.Id == command.LocationId
                && location.ArchivedAtUtc == null,
            cancellationToken))
        {
            return new(null, ScheduleEditFailure.NotFound);
        }

        if ((ScheduleEditScope)command.Scope == ScheduleEditScope.OneOccurrence)
        {
            var occurrence = CreateOccurrence(command.Date, command.StartsAt, command.EndsAt);
            if (occurrence is null)
            {
                return new(null, ScheduleEditFailure.InvalidLocalTime);
            }

            if (occurrence.Start < clock.GetUtcNow())
            {
                return new(null, ScheduleEditFailure.Invalid);
            }

            return new(new(session, null, [occurrence], [session]), ScheduleEditFailure.None);
        }

        if (!session.RecurringSessionSeriesId.HasValue
            || !session.SeriesOccurrenceDate.HasValue
            || session.IsSeriesException
            || command.Date != session.SeriesOccurrenceDate
            || Normalize(command.Title) != session.Title
            || Normalize(command.Notes) != session.Notes)
        {
            return new(null, ScheduleEditFailure.Invalid);
        }

        var series = await db.RecurringSessionSeries.SingleOrDefaultAsync(
            item => item.TeacherAccountId == owner
                && item.Id == session.RecurringSessionSeriesId
                && item.SupersededAtUtc == null,
            cancellationToken);
        if (series is null
            || series.DayOfWeek != command.Date.DayOfWeek
            || series.GroupId != session.GroupId
            || series.StudentId != session.StudentId
            || series.EndsOn.HasValue && series.EndsOn < command.Date)
        {
            return new(null, ScheduleEditFailure.Unavailable);
        }

        var generated = GroupScheduleGenerator.Generate(
            [new GroupScheduleSlot((int)series.DayOfWeek, command.StartsAt, command.EndsAt)],
            command.Date,
            series.EndsOn,
            clock.GetUtcNow());
        if (generated is null)
        {
            return new(null, ScheduleEditFailure.InvalidLocalTime);
        }

        if (generated.Count == 0 || generated[0].Date != command.Date)
        {
            return new(null, ScheduleEditFailure.Invalid);
        }

        var affected = await db.Sessions.Where(item =>
                item.RecurringSessionSeriesId == series.Id
                && item.SeriesOccurrenceDate >= command.Date)
            .ToListAsync(cancellationToken);
        var replaceable = affected.Where(item =>
                item.Status == SessionStatus.Scheduled && !item.IsSeriesException)
            .ToList();
        if (!replaceable.Any(item => item.Id == session.Id))
        {
            return new(null, ScheduleEditFailure.Unavailable);
        }

        var preservedDates = affected.Where(item =>
                item.Status != SessionStatus.Scheduled || item.IsSeriesException)
            .Select(item => item.SeriesOccurrenceDate!.Value)
            .ToHashSet();
        var materialized = generated.Where(item => !preservedDates.Contains(item.Date)).ToList();

        return new(new(session, series, materialized, replaceable), ScheduleEditFailure.None);
    }

    private async Task<bool> HasConflictAsync(
        Guid owner,
        Guid? locationId,
        Prepared value,
        CancellationToken cancellationToken)
    {
        var excludedSessionIds = value.ReplaceableSessions.Select(item => item.Id).ToArray();
        var excludedSeriesIds = value.Series is null ? null : new HashSet<Guid> { value.Series.Id };
        if (await GroupScheduleConflictQuery.HasUnmaterializedConflictAsync(
            db,
            owner,
            locationId,
            value.Occurrences,
            cancellationToken,
            excludedSeriesIds))
        {
            return true;
        }

        foreach (var occurrence in value.Occurrences)
        {
            if (await db.Sessions.AnyAsync(session =>
                    !excludedSessionIds.Contains(session.Id)
                    && session.TeacherAccountId == owner
                    && session.Status != SessionStatus.Cancelled
                    && session.StartsAtUtc < occurrence.End
                    && session.EndsAtUtc > occurrence.Start,
                cancellationToken)
                || locationId.HasValue && await db.Sessions.AnyAsync(session =>
                    !excludedSessionIds.Contains(session.Id)
                    && session.LocationId == locationId
                    && session.Status != SessionStatus.Cancelled
                    && session.StartsAtUtc < occurrence.End
                    && session.EndsAtUtc > occurrence.Start,
                cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static GroupOccurrence? CreateOccurrence(DateOnly date, TimeOnly start, TimeOnly end)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var localStart = date.ToDateTime(start, DateTimeKind.Unspecified);
        var localEnd = date.ToDateTime(end, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(localStart) || zone.IsAmbiguousTime(localStart)
            || zone.IsInvalidTime(localEnd) || zone.IsAmbiguousTime(localEnd))
        {
            return null;
        }

        return new(
            0,
            date,
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, zone)));
    }

    private static bool Valid(Guid owner, Guid sessionId, ScheduleEditCommand command) =>
        owner != Guid.Empty
        && sessionId != Guid.Empty
        && command is not null
        && Enum.IsDefined((ScheduleEditScope)command.Scope)
        && command.Date != default
        && command.EndsAt > command.StartsAt
        && command.StartsAt.Ticks % TimeSpan.TicksPerMinute == 0
        && command.EndsAt.Ticks % TimeSpan.TicksPerMinute == 0
        && command.Title?.Length is not > Session.TitleMaxLength
        && command.Notes?.Length is not > Session.NotesMaxLength
        && command.RowVersion is { Length: 8 }
        && command.LocationId != Guid.Empty
        && !(command.LocationId.HasValue && !string.IsNullOrWhiteSpace(command.OnlineMeetingUrl))
        && ValidOnlineMeetingUrl(command.OnlineMeetingUrl);

    private static bool ValidOnlineMeetingUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length <= Session.OnlineMeetingUrlMaxLength
            && Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : value.Trim();

    private sealed record Prepared(
        Session Session,
        RecurringSessionSeries? Series,
        IReadOnlyList<GroupOccurrence> Occurrences,
        IReadOnlyList<Session> ReplaceableSessions);

    private sealed record PreparedResult(Prepared? Value, ScheduleEditFailure Failure);
}
