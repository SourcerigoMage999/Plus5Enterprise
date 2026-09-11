using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupCreationService(Plus5DbContext db, TimeProvider clock) : IGroupCreationService
{
    public async Task<GroupCreateResult> CreateAsync(Guid owner, GroupCreateCommand command, CancellationToken cancellationToken)
    {
        if (!Valid(command)) return new(null, GroupCreateFailure.Invalid);
        var now = clock.GetUtcNow();
        var occurrences = command.Slots.Count == 0 ? [] : GroupScheduleGenerator.Generate(command.Slots, command.StartsOn!.Value, command.EndsOn, now);
        if (occurrences is null) return new(null, GroupCreateFailure.InvalidLocalTime);
        var sorted = occurrences.OrderBy(item => item.Start).ToArray();
        for (var i = 1; i < sorted.Length; i++)
            if (sorted[i].Start < sorted[i - 1].End) return new(null, GroupCreateFailure.ScheduleConflict);
        // Reject overlapping rules even when the selected date range has no initial occurrence.
        for (var i = 0; i < command.Slots.Count; i++)
            for (var j = i + 1; j < command.Slots.Count; j++)
                if (command.Slots[i].DayOfWeek == command.Slots[j].DayOfWeek && command.Slots[i].Start < command.Slots[j].End
                    && command.Slots[i].End > command.Slots[j].Start) return new(null, GroupCreateFailure.ScheduleConflict);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            if (!await db.Programs.AnyAsync(p => p.Id == command.ProgramId && p.TeacherAccountId == owner, cancellationToken)
                || !await db.SchoolGrades.AnyAsync(g => g.Id == command.SchoolGradeId, cancellationToken)
                || (command.LocationId.HasValue && !await db.Locations.AnyAsync(l => l.Id == command.LocationId
                    && l.TeacherAccountId == owner && l.ArchivedAtUtc == null, cancellationToken)))
                return new(null, GroupCreateFailure.NotFound);
            var normalizedName = command.Name.Trim().ToUpperInvariant();
            if (await db.Groups.AnyAsync(g => g.TeacherAccountId == owner && g.NormalizedName == normalizedName, cancellationToken))
                return new(null, GroupCreateFailure.DuplicateName);
            var ids = command.Members.Select(m => m.StudentId).ToArray();
            var students = await db.Students.Where(s => s.TeacherAccountId == owner && s.ArchivedAtUtc == null && ids.Contains(s.Id))
                .OrderBy(s => s.Id).ToListAsync(cancellationToken);
            if (students.Count != ids.Length) return new(null, GroupCreateFailure.NotFound);
            if (await db.GroupMemberships.AnyAsync(m => ids.Contains(m.StudentId) && m.LeftAtUtc == null, cancellationToken))
                return new(null, GroupCreateFailure.MembershipChanged);
            var versions = command.Members.ToDictionary(m => m.StudentId, m => m.RowVersion);
            if (students.Any(s => !s.RowVersion.AsSpan().SequenceEqual(versions[s.Id]))) return new(null, GroupCreateFailure.Conflict);

            // Indexed overlap probes are protected by the same Serializable transaction as the writes.
            if (await GroupScheduleConflictQuery.HasUnmaterializedConflictAsync(db, owner, command.LocationId, occurrences, cancellationToken))
                return new(null, GroupCreateFailure.ScheduleConflict);
            foreach (var occurrence in sorted)
            {
                if (await db.Sessions.AnyAsync(s => s.TeacherAccountId == owner && s.Status != SessionStatus.Cancelled
                    && s.StartsAtUtc < occurrence.End && s.EndsAtUtc > occurrence.Start, cancellationToken))
                    return new(null, GroupCreateFailure.ScheduleConflict);
                if (command.LocationId.HasValue && await db.Sessions.AnyAsync(s => s.LocationId == command.LocationId
                    && s.Status != SessionStatus.Cancelled && s.StartsAtUtc < occurrence.End && s.EndsAtUtc > occurrence.Start, cancellationToken))
                    return new(null, GroupCreateFailure.ScheduleConflict);
            }

            var group = new Group(Guid.NewGuid(), owner, command.ProgramId, command.SchoolGradeId,
                command.Name, command.Capacity, GroupStatus.Active, now, command.Description);
            db.Groups.Add(group);
            foreach (var student in students)
            {
                student.AssignToGroupProgram(group.ProgramId, now);
                db.Entry(student).Property(s => s.UpdatedAtUtc).IsModified = true;
                db.GroupMemberships.Add(new(Guid.NewGuid(), owner, group.Id, student.Id, now));
            }
            group.RecordMembershipChange(students.Count, now);
            for (var i = 0; i < command.Slots.Count; i++)
            {
                var slot = command.Slots[i];
                var series = new RecurringSessionSeries(Guid.NewGuid(), owner, RecurringSessionSeriesKind.RegularGroupSchedule,
                    group.Id, (DayOfWeek)slot.DayOfWeek, command.StartsOn!.Value, command.EndsOn, slot.Start, slot.End,
                    GroupScheduleGenerator.TimeZoneId, now, command.LocationId);
                db.RecurringSessionSeries.Add(series);
                foreach (var occurrence in occurrences.Where(o => o.SlotIndex == i))
                    db.Sessions.Add(new(Guid.NewGuid(), owner, DeliveryMode.Group, group.Id, occurrence.Start, occurrence.End,
                        GroupScheduleGenerator.TimeZoneId, now, locationId: command.LocationId,
                        recurringSessionSeriesId: series.Id, seriesOccurrenceDate: occurrence.Date));
            }
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(group.Id, GroupCreateFailure.None, occurrences.Count);
        }
        catch (DbUpdateConcurrencyException) { return new(null, GroupCreateFailure.Conflict); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 1205 })
        { return new(null, GroupCreateFailure.Conflict); }
        catch (SqlException ex) when (ex.Number == 1205) { return new(null, GroupCreateFailure.Conflict); }
        catch (InvalidOperationException ex) when (ex.InnerException?.InnerException is SqlException { Number: 1205 }
            || ex.InnerException is SqlException { Number: 1205 })
        { return new(null, GroupCreateFailure.Conflict); }
    }

    private static bool Valid(GroupCreateCommand c) => !string.IsNullOrWhiteSpace(c.Name) && c.Name.Length <= Group.NameMaxLength
        && c.Description?.Length is not > Group.DescriptionMaxLength && c.ProgramId != Guid.Empty && c.SchoolGradeId != Guid.Empty
        && c.Capacity > 0 && c.Members is not null && c.Members.Count <= 100 && c.Members.Count <= c.Capacity
        && c.Members.All(m => m is not null && m.StudentId != Guid.Empty && m.RowVersion is { Length: 8 })
        && c.Members.Select(m => m.StudentId).Distinct().Count() == c.Members.Count
        && c.Slots is not null && c.Slots.Count <= 14 && c.LocationId != Guid.Empty
        && (c.Slots.Count == 0 || (c.StartsOn.HasValue && (!c.EndsOn.HasValue || c.EndsOn >= c.StartsOn)))
        && c.Slots.All(s => s is not null && s.DayOfWeek is >= 0 and <= 6 && s.End > s.Start
            && s.Start.Ticks % TimeSpan.TicksPerMinute == 0 && s.End.Ticks % TimeSpan.TicksPerMinute == 0);
}
