using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupEditingService(Plus5DbContext db, TimeProvider clock) : IGroupEditingService
{
    public async Task<GroupEditResult> UpdateAsync(Guid owner, Guid groupId, GroupEditCommand command,
        CancellationToken cancellationToken)
    {
        if (!Valid(command)) return new(GroupEditFailure.Invalid);
        var now = clock.GetUtcNow();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(GroupScheduleGenerator.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            var group = await db.Groups.SingleOrDefaultAsync(item => item.TeacherAccountId == owner
                && item.Id == groupId && item.ArchivedAtUtc == null, cancellationToken);
            if (group is null) return new(GroupEditFailure.NotFound);
            if (!group.RowVersion.AsSpan().SequenceEqual(command.RowVersion)) return new(GroupEditFailure.Conflict);

            var memberCount = await db.GroupMemberships.CountAsync(member => member.TeacherAccountId == owner
                && member.GroupId == groupId && member.LeftAtUtc == null, cancellationToken);
            if (command.Capacity < memberCount) return new(GroupEditFailure.CapacityBelowMembers);
            if (group.ProgramId != command.ProgramId && memberCount > 0)
                return new(GroupEditFailure.ProgramHasActiveMembers);
            if (!await db.Programs.AnyAsync(program => program.TeacherAccountId == owner
                    && program.Id == command.ProgramId, cancellationToken)
                || !await db.SchoolGrades.AnyAsync(grade => grade.Id == command.SchoolGradeId, cancellationToken)
                || command.LocationId.HasValue && !await db.Locations.AnyAsync(location => location.TeacherAccountId == owner
                    && location.Id == command.LocationId && location.ArchivedAtUtc == null, cancellationToken))
                return new(GroupEditFailure.NotFound);
            var normalizedName = command.Name.Trim().ToUpperInvariant();
            if (await db.Groups.AnyAsync(item => item.TeacherAccountId == owner && item.Id != groupId
                && item.NormalizedName == normalizedName, cancellationToken)) return new(GroupEditFailure.DuplicateName);

            var current = await db.RecurringSessionSeries.Where(series => series.TeacherAccountId == owner
                    && series.GroupId == groupId && series.SupersededAtUtc == null
                    && (series.EndsOn == null || series.EndsOn >= today))
                .OrderBy(series => series.DayOfWeek).ThenBy(series => series.LocalStartTime).ThenBy(series => series.Id)
                .ToListAsync(cancellationToken);
            var scheduleChanged = !SameSchedule(current, command);
            IReadOnlyList<GroupOccurrence> occurrences = [];
            var effective = command.ScheduleStartsOn;
            if (scheduleChanged)
            {
                if (current.Count > 0)
                {
                    effective = command.Slots.Count == 0 ? today.AddDays(1) : command.ScheduleStartsOn;
                    if (!effective.HasValue || effective.Value <= today) return new(GroupEditFailure.Invalid);
                }
                if (command.Slots.Count > 0)
                {
                    var generated = GroupScheduleGenerator.Generate(command.Slots, effective!.Value,
                        command.ScheduleEndsOn, now);
                    if (generated is null) return new(GroupEditFailure.InvalidLocalTime);
                    occurrences = generated;
                    if (Overlaps(command.Slots, occurrences)) return new(GroupEditFailure.ScheduleConflict);
                    var excluded = current.Select(series => series.Id).ToHashSet();
                    if (await GroupScheduleConflictQuery.HasUnmaterializedConflictAsync(db, owner,
                        command.LocationId, occurrences, cancellationToken, excluded))
                        return new(GroupEditFailure.ScheduleConflict);
                    foreach (var occurrence in occurrences)
                    {
                        if (await db.Sessions.AnyAsync(session =>
                                (!session.RecurringSessionSeriesId.HasValue || !excluded.Contains(session.RecurringSessionSeriesId.Value))
                                && session.TeacherAccountId == owner && session.Status != SessionStatus.Cancelled
                                && session.StartsAtUtc < occurrence.End && session.EndsAtUtc > occurrence.Start, cancellationToken)
                            || command.LocationId.HasValue && await db.Sessions.AnyAsync(session =>
                                (!session.RecurringSessionSeriesId.HasValue || !excluded.Contains(session.RecurringSessionSeriesId.Value))
                                && session.LocationId == command.LocationId && session.Status != SessionStatus.Cancelled
                                && session.StartsAtUtc < occurrence.End && session.EndsAtUtc > occurrence.Start, cancellationToken))
                            return new(GroupEditFailure.ScheduleConflict);
                    }
                }
            }

            group.UpdateDetails(command.ProgramId, command.SchoolGradeId, command.Name, command.Description,
                command.Capacity, (GroupStatus)command.Status, memberCount, now);
            db.Entry(group).Property(item => item.UpdatedAtUtc).IsModified = true;

            if (scheduleChanged)
            {
                var changeDate = effective!.Value;
                var finalDate = changeDate.AddDays(-1);
                var replaced = current.Where(series => series.EndsOn == null || series.EndsOn >= changeDate).ToList();
                var replacedIds = replaced.Select(series => series.Id).ToArray();
                foreach (var series in replaced)
                    series.Supersede(finalDate < series.StartsOn ? series.StartsOn : finalDate, now);
                var future = await db.Sessions.Where(session => session.RecurringSessionSeriesId.HasValue
                        && replacedIds.Contains(session.RecurringSessionSeriesId.Value)
                        && session.SeriesOccurrenceDate >= changeDate && session.Status == SessionStatus.Scheduled)
                    .ToListAsync(cancellationToken);
                foreach (var session in future) session.Cancel(now);

                for (var index = 0; index < command.Slots.Count; index++)
                {
                    var slot = command.Slots[index];
                    var series = new RecurringSessionSeries(Guid.NewGuid(), owner,
                        RecurringSessionSeriesKind.RegularGroupSchedule, groupId, (DayOfWeek)slot.DayOfWeek,
                        changeDate, command.ScheduleEndsOn, slot.Start, slot.End,
                        GroupScheduleGenerator.TimeZoneId, now, command.LocationId,
                        previousSeriesId: index < replaced.Count ? replaced[index].Id : null);
                    db.RecurringSessionSeries.Add(series);
                    foreach (var occurrence in occurrences.Where(item => item.SlotIndex == index))
                        db.Sessions.Add(new Session(Guid.NewGuid(), owner, DeliveryMode.Group, groupId,
                            occurrence.Start, occurrence.End, GroupScheduleGenerator.TimeZoneId, now,
                            locationId: command.LocationId, recurringSessionSeriesId: series.Id,
                            seriesOccurrenceDate: occurrence.Date));
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(GroupEditFailure.None, occurrences.Count);
        }
        catch (DbUpdateConcurrencyException) { return new(GroupEditFailure.Conflict); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 or 1205 })
        { return new(GroupEditFailure.Conflict); }
        catch (SqlException exception) when (exception.Number == 1205) { return new(GroupEditFailure.Conflict); }
        catch (InvalidOperationException exception) when (exception.InnerException?.InnerException is SqlException { Number: 1205 }
            || exception.InnerException is SqlException { Number: 1205 })
        { return new(GroupEditFailure.Conflict); }
    }

    private static bool SameSchedule(List<RecurringSessionSeries> current, GroupEditCommand command)
    {
        if (current.Count != command.Slots.Count) return false;
        var ordered = command.Slots.OrderBy(slot => slot.DayOfWeek).ThenBy(slot => slot.Start).ThenBy(slot => slot.End).ToArray();
        for (var index = 0; index < current.Count; index++)
            if ((int)current[index].DayOfWeek != ordered[index].DayOfWeek
                || current[index].LocalStartTime != ordered[index].Start || current[index].LocalEndTime != ordered[index].End
                || current[index].StartsOn != command.ScheduleStartsOn || current[index].EndsOn != command.ScheduleEndsOn
                || current[index].LocationId != command.LocationId) return false;
        return true;
    }

    private static bool Overlaps(IReadOnlyList<GroupScheduleSlot> slots, IReadOnlyList<GroupOccurrence> occurrences)
    {
        var ordered = occurrences.OrderBy(item => item.Start).ToArray();
        for (var index = 1; index < ordered.Length; index++)
            if (ordered[index].Start < ordered[index - 1].End) return true;
        for (var first = 0; first < slots.Count; first++)
            for (var second = first + 1; second < slots.Count; second++)
                if (slots[first].DayOfWeek == slots[second].DayOfWeek && slots[first].Start < slots[second].End
                    && slots[first].End > slots[second].Start) return true;
        return false;
    }

    private static bool Valid(GroupEditCommand command) => !string.IsNullOrWhiteSpace(command.Name)
        && command.Name.Length <= Group.NameMaxLength && command.Description?.Length is not > Group.DescriptionMaxLength
        && command.ProgramId != Guid.Empty && command.SchoolGradeId != Guid.Empty
        && command.Status is >= 1 and <= 3 && command.Capacity > 0 && command.RowVersion is { Length: 8 }
        && command.Slots is not null && command.Slots.Count <= 14 && command.LocationId != Guid.Empty
        && (command.Slots.Count == 0
            ? command.ScheduleStartsOn is null && command.ScheduleEndsOn is null && command.LocationId is null
            : command.ScheduleStartsOn.HasValue
                && (!command.ScheduleEndsOn.HasValue || command.ScheduleEndsOn >= command.ScheduleStartsOn))
        && command.Slots.All(slot => slot is not null && slot.DayOfWeek is >= 0 and <= 6 && slot.End > slot.Start
            && slot.Start.Ticks % TimeSpan.TicksPerMinute == 0 && slot.End.Ticks % TimeSpan.TicksPerMinute == 0);
}
