namespace Plus5.Application.Groups;

public sealed record GroupCriteria(int Page, int PageSize, string? Search = null, Guid? ProgramId = null, int? Status = null);
public sealed record GroupPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
public sealed record GroupSlot(int DayOfWeek, TimeOnly Start, TimeOnly End, string TimeZoneId, string? Location, bool Online);
public sealed record GroupItem(Guid Id, string Name, Guid ProgramId, string ProgramName, Guid SchoolGradeId,
    string SchoolGrade, int Status, int Capacity, int MemberCount, byte[] RowVersion, IReadOnlyList<GroupSlot> Slots);
public sealed record GroupOverview(long TotalGroups, long ActiveGroups, long Students, long AvailableSeats,
    long SessionsThisWeek, DateOnly WeekStartsOn);
public sealed record GroupStudent(Guid Id, string FirstName, string LastName, string SchoolGrade,
    bool Recommended, byte[] RowVersion);
public sealed record GroupSession(Guid Id, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    string TimeZoneId, string? Location, bool Online, int Status);

public interface IGroupQuery
{
    Task<GroupPage<GroupItem>> GetPageAsync(Guid owner, GroupCriteria criteria, CancellationToken cancellationToken);
    Task<GroupOverview> GetOverviewAsync(Guid owner, CancellationToken cancellationToken);
    Task<GroupItem?> GetAsync(Guid owner, Guid groupId, CancellationToken cancellationToken);
    Task<GroupPage<GroupStudent>?> GetStudentsAsync(Guid owner, Guid groupId, GroupCriteria criteria, bool candidates, CancellationToken cancellationToken);
    Task<GroupPage<GroupSession>?> GetSessionsAsync(Guid owner, Guid groupId, GroupCriteria criteria, CancellationToken cancellationToken);
}

public sealed record GroupMembershipCommand(bool Join, byte[] GroupRowVersion, byte[] StudentRowVersion);
public enum GroupMembershipResult { Saved, NotFound, Conflict, Full, Unavailable, MembershipChanged }
public interface IGroupMembershipService
{
    Task<GroupMembershipResult> ChangeAsync(Guid owner, Guid groupId, Guid studentId,
        GroupMembershipCommand command, CancellationToken cancellationToken);
}
