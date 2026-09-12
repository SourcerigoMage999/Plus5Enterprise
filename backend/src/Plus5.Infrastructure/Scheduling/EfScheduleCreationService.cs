using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Application.Scheduling;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed class EfScheduleCreationService(Plus5DbContext db, TimeProvider clock)
    : IScheduleCreationService
{
    public async Task<ScheduleCreateResult> CreateAsync(
        Guid owner,
        ScheduleCreateCommand command,
        CancellationToken cancellationToken)
    {
        if (!Valid(owner, command))
        {
            return new(null, ScheduleCreateFailure.Invalid);
        }

        var now = clock.GetUtcNow();
        var occurrences = CreateOccurrences(command, now);
        if (occurrences is null)
        {
            return new(null, ScheduleCreateFailure.InvalidLocalTime);
        }

        if (occurrences.Count == 0 || occurrences[0].Start < now)
        {
            return new(null, ScheduleCreateFailure.Invalid);
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            if (!await ContextExistsAsync(owner, command, cancellationToken)
                || command.LocationId.HasValue && !await db.Locations.AnyAsync(
                    location => location.Id == command.LocationId
                        && location.TeacherAccountId == owner
                        && location.ArchivedAtUtc == null,
                    cancellationToken))
            {
                return new(null, ScheduleCreateFailure.NotFound);
            }

            if (!await ContextAvailableAsync(owner, command, cancellationToken))
            {
                return new(null, ScheduleCreateFailure.Unavailable);
            }

            if (await GroupScheduleConflictQuery.HasUnmaterializedConflictAsync(
                    db,
                    owner,
                    command.LocationId,
                    occurrences,
                    cancellationToken))
            {
                return new(null, ScheduleCreateFailure.ScheduleConflict);
            }

            foreach (var occurrence in occurrences)
            {
                if (await db.Sessions.AnyAsync(
                    session => session.TeacherAccountId == owner
                        && session.Status != SessionStatus.Cancelled
                        && session.StartsAtUtc < occurrence.End
                        && session.EndsAtUtc > occurrence.Start,
                    cancellationToken))
                {
                    return new(null, ScheduleCreateFailure.ScheduleConflict);
                }

                if (command.LocationId.HasValue && await db.Sessions.AnyAsync(
                    session => session.LocationId == command.LocationId
                        && session.Status != SessionStatus.Cancelled
                        && session.StartsAtUtc < occurrence.End
                        && session.EndsAtUtc > occurrence.Start,
                    cancellationToken))
                {
                    return new(null, ScheduleCreateFailure.ScheduleConflict);
                }
            }

            RecurringSessionSeries? series = null;
            if (command.RepeatWeekly)
            {
                series = new RecurringSessionSeries(
                    Guid.NewGuid(),
                    owner,
                    RecurringSessionSeriesKind.IndividualRecurrence,
                    command.ContextId,
                    command.Date.DayOfWeek,
                    command.Date,
                    command.EndsOn,
                    command.StartsAt,
                    command.EndsAt,
                    GroupScheduleGenerator.TimeZoneId,
                    now,
                    command.LocationId,
                    command.OnlineMeetingUrl);
                db.RecurringSessionSeries.Add(series);
            }

            var sessions = occurrences.Select(occurrence => new Session(
                Guid.NewGuid(),
                owner,
                (DeliveryMode)command.DeliveryMode,
                command.ContextId,
                occurrence.Start,
                occurrence.End,
                GroupScheduleGenerator.TimeZoneId,
                now,
                command.Title,
                command.Notes,
                locationId: command.LocationId,
                onlineMeetingUrl: command.OnlineMeetingUrl,
                recurringSessionSeriesId: series?.Id,
                seriesOccurrenceDate: series is null ? null : occurrence.Date)).ToArray();

            db.Sessions.AddRange(sessions);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new(sessions[0].Id, ScheduleCreateFailure.None, sessions.Length);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(null, ScheduleCreateFailure.ConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        { Number: 2601 or 2627 or 1205 })
        {
            return new(null, ScheduleCreateFailure.ConcurrencyConflict);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return new(null, ScheduleCreateFailure.ConcurrencyConflict);
        }
        catch (InvalidOperationException exception) when (
            exception.InnerException?.InnerException is SqlException { Number: 1205 }
            || exception.InnerException is SqlException { Number: 1205 })
        {
            return new(null, ScheduleCreateFailure.ConcurrencyConflict);
        }
    }

    private static IReadOnlyList<GroupOccurrence>? CreateOccurrences(
        ScheduleCreateCommand command,
        DateTimeOffset now)
    {
        if (command.RepeatWeekly)
        {
            return GroupScheduleGenerator.Generate(
                [new GroupScheduleSlot((int)command.Date.DayOfWeek, command.StartsAt, command.EndsAt)],
                command.Date,
                command.EndsOn,
                now);
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var localStart = command.Date.ToDateTime(command.StartsAt, DateTimeKind.Unspecified);
        var localEnd = command.Date.ToDateTime(command.EndsAt, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(localStart) || zone.IsAmbiguousTime(localStart)
            || zone.IsInvalidTime(localEnd) || zone.IsAmbiguousTime(localEnd))
        {
            return null;
        }

        return
        [
            new GroupOccurrence(
                0,
                command.Date,
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone)),
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, zone))),
        ];
    }

    private Task<bool> ContextExistsAsync(
        Guid owner,
        ScheduleCreateCommand command,
        CancellationToken cancellationToken) => command.DeliveryMode == (int)DeliveryMode.Group
            ? db.Groups.AnyAsync(group => group.Id == command.ContextId
                && group.TeacherAccountId == owner, cancellationToken)
            : db.Students.AnyAsync(student => student.Id == command.ContextId
                && student.TeacherAccountId == owner, cancellationToken);

    private Task<bool> ContextAvailableAsync(
        Guid owner,
        ScheduleCreateCommand command,
        CancellationToken cancellationToken) => command.DeliveryMode == (int)DeliveryMode.Group
            ? db.Groups.AnyAsync(group => group.Id == command.ContextId
                && group.TeacherAccountId == owner
                && group.Status == GroupStatus.Active
                && group.ArchivedAtUtc == null, cancellationToken)
            : db.Students.AnyAsync(student => student.Id == command.ContextId
                && student.TeacherAccountId == owner
                && student.Status != StudentStatus.Inactive
                && student.ArchivedAtUtc == null, cancellationToken);

    private static bool Valid(Guid owner, ScheduleCreateCommand command) => owner != Guid.Empty
        && command is not null
        && Enum.IsDefined((DeliveryMode)command.DeliveryMode)
        && command.ContextId != Guid.Empty
        && command.Date != default
        && command.EndsAt > command.StartsAt
        && command.StartsAt.Ticks % TimeSpan.TicksPerMinute == 0
        && command.EndsAt.Ticks % TimeSpan.TicksPerMinute == 0
        && command.Title?.Length is not > Session.TitleMaxLength
        && command.Notes?.Length is not > Session.NotesMaxLength
        && command.LocationId != Guid.Empty
        && !(command.LocationId.HasValue && !string.IsNullOrWhiteSpace(command.OnlineMeetingUrl))
        && ValidOnlineMeetingUrl(command.OnlineMeetingUrl)
        && (!command.RepeatWeekly || command.DeliveryMode == (int)DeliveryMode.Individual)
        && (command.RepeatWeekly
            ? !command.EndsOn.HasValue || command.EndsOn >= command.Date
            : !command.EndsOn.HasValue);

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
}
