namespace Plus5.Application.Groups;

public sealed record GroupCreationCriteria(int Page, int PageSize, Guid ProgramId, Guid SchoolGradeId, string? Search = null);

public sealed record GroupCreationCandidate(Guid Id, string FirstName, string LastName, string SchoolGrade,
    string? ProgramName, bool Recommended, byte[] RowVersion);

public interface IGroupCreationQuery
{
    Task<GroupPage<GroupCreationCandidate>?> GetCandidatesAsync(Guid owner, GroupCreationCriteria criteria, CancellationToken cancellationToken);
    Task<GroupPage<GroupLocation>> GetLocationsAsync(Guid owner, int page, string? search, CancellationToken cancellationToken);
}

public sealed record GroupLocation(Guid Id, string Name);

public sealed record InitialGroupMember(Guid StudentId, byte[] RowVersion);
public sealed record GroupScheduleSlot(int DayOfWeek, TimeOnly Start, TimeOnly End);
public sealed record GroupCreateCommand(string Name, Guid ProgramId, Guid SchoolGradeId, int Capacity,
    string? Description, IReadOnlyList<InitialGroupMember> Members, IReadOnlyList<GroupScheduleSlot> Slots,
    DateOnly? StartsOn, DateOnly? EndsOn, Guid? LocationId);
public enum GroupCreateFailure { None, Invalid, NotFound, DuplicateName, MembershipChanged, Conflict, ScheduleConflict, InvalidLocalTime }
public sealed record GroupCreateResult(Guid? Id, GroupCreateFailure Failure, int SessionCount = 0);
public interface IGroupCreationService
{
    Task<GroupCreateResult> CreateAsync(Guid owner, GroupCreateCommand command, CancellationToken cancellationToken);
}

public sealed record GroupOccurrence(int SlotIndex, DateOnly Date, DateTimeOffset Start, DateTimeOffset End);

public static class GroupScheduleGenerator
{
    public const int HorizonDays = 84;
    public const string TimeZoneId = "Europe/Zagreb";

    public static IReadOnlyList<GroupOccurrence>? Generate(IReadOnlyList<GroupScheduleSlot> slots,
        DateOnly startsOn, DateOnly? endsOn, DateTimeOffset now)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var first = startsOn > today ? startsOn : today;
        var lastDay = Math.Min(DateOnly.MaxValue.DayNumber, (long)first.DayNumber + HorizonDays - 1);
        if (endsOn.HasValue) lastDay = Math.Min(lastDay, endsOn.Value.DayNumber);
        var result = new List<GroupOccurrence>();
        for (var day = first.DayNumber; day <= lastDay; day++)
        {
            var date = DateOnly.FromDayNumber(day);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if ((int)date.DayOfWeek != slot.DayOfWeek) continue;
                var start = date.ToDateTime(slot.Start, DateTimeKind.Unspecified);
                var end = date.ToDateTime(slot.End, DateTimeKind.Unspecified);
                if (zone.IsInvalidTime(start) || zone.IsAmbiguousTime(start) || zone.IsInvalidTime(end) || zone.IsAmbiguousTime(end)) return null;
                var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone));
                var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end, zone));
                if (startUtc >= now) result.Add(new(i, date, startUtc, endUtc));
            }
        }
        return result;
    }
}
