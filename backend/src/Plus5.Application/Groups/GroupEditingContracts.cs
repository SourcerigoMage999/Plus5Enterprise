namespace Plus5.Application.Groups;

public sealed record GroupEditSlot(Guid SeriesId, int DayOfWeek, TimeOnly Start, TimeOnly End,
    DateOnly StartsOn, DateOnly? EndsOn, Guid? LocationId, string? LocationName);

public sealed record GroupEditItem(Guid Id, string Name, string? Description, Guid ProgramId,
    string ProgramName, Guid SchoolGradeId, string SchoolGrade, int Status, int Capacity,
    int MemberCount, byte[] RowVersion, IReadOnlyList<GroupEditSlot> Slots);

public interface IGroupEditingQuery
{
    Task<GroupEditItem?> GetAsync(Guid owner, Guid groupId, CancellationToken cancellationToken);
}

public sealed record GroupEditCommand(string Name, string? Description, Guid ProgramId,
    Guid SchoolGradeId, int Status, int Capacity, byte[] RowVersion,
    IReadOnlyList<GroupScheduleSlot> Slots, DateOnly? ScheduleStartsOn, DateOnly? ScheduleEndsOn,
    Guid? LocationId);

public enum GroupEditFailure
{
    None,
    Invalid,
    NotFound,
    DuplicateName,
    ProgramHasActiveMembers,
    CapacityBelowMembers,
    Conflict,
    ScheduleConflict,
    InvalidLocalTime,
}

public sealed record GroupEditResult(GroupEditFailure Failure, int SessionCount = 0);

public interface IGroupEditingService
{
    Task<GroupEditResult> UpdateAsync(Guid owner, Guid groupId, GroupEditCommand command,
        CancellationToken cancellationToken);
}
