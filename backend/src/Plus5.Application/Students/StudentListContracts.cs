namespace Plus5.Application.Students;

public enum StudentListDeliveryMode
{
    Individual = 1,
    Group = 2,
}

public enum StudentListStatus
{
    Active = 1,
    OnHold = 2,
    Inactive = 3,
}

public sealed record StudentListCriteria(
    int Page,
    int PageSize,
    string? Search,
    Guid? ProgramId,
    StudentListDeliveryMode? DeliveryMode,
    StudentListStatus? Status,
    Guid? SchoolGradeId);

public sealed record StudentListItem(
    Guid Id,
    string FirstName,
    string LastName,
    string? Nickname,
    Guid SchoolGradeId,
    string SchoolGradeCode,
    string SchoolGradeName,
    Guid? ProgramId,
    string? ProgramName,
    StudentListDeliveryMode? DeliveryMode,
    Guid? GroupId,
    string? GroupName,
    StudentListStatus Status,
    DateTimeOffset? LastSessionAtUtc);

public sealed record StudentListPage(
    IReadOnlyList<StudentListItem> Items,
    int Page,
    int PageSize,
    long TotalCount);

public sealed record StudentListOverview(
    long TotalCount,
    long ActiveCount,
    long OnHoldCount,
    long InactiveCount,
    long WithoutProgramCount,
    IReadOnlyList<StudentProgramCount> ProgramCounts,
    IReadOnlyList<StudentFilterOption> Programs,
    IReadOnlyList<StudentFilterOption> SchoolGrades);

public sealed record StudentProgramCount(Guid ProgramId, string Name, long StudentCount);

public sealed record StudentFilterOption(Guid Id, string Code, string Name);

public interface IStudentListQuery
{
    Task<StudentListPage> GetPageAsync(
        Guid teacherAccountId,
        StudentListCriteria criteria,
        CancellationToken cancellationToken);

    Task<StudentListOverview> GetOverviewAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken);
}
