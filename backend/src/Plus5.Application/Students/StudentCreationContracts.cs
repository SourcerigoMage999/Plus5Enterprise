namespace Plus5.Application.Students;

public enum StudentCreationStatus
{
    Active = 1,
    OnHold = 2,
    Inactive = 3,
}

public enum StudentCreationDeliveryMode
{
    Individual = 1,
    Group = 2,
}

public sealed record StudentGuardianInput(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone);

public sealed record StudentCreateCommand(
    string FirstName,
    string LastName,
    Guid SchoolGradeId,
    string? SchoolName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Email,
    string? Phone,
    Guid? ProgramId,
    StudentCreationDeliveryMode? DeliveryMode,
    Guid? GroupId,
    StudentCreationStatus Status,
    StudentGuardianInput? Guardian);

public enum StudentCreateFailure
{
    None = 0,
    SchoolGradeNotFound,
    ProgramNotFound,
    GroupNotFound,
    GroupUnavailable,
    GroupCapacityReached,
    GroupProgramMismatch,
    ConcurrencyConflict,
}

public sealed record StudentCreateResult(Guid? StudentId, StudentCreateFailure Failure)
{
    public static StudentCreateResult Success(Guid studentId) => new(studentId, StudentCreateFailure.None);

    public static StudentCreateResult Failed(StudentCreateFailure failure) => new(null, failure);
}

public sealed record StudentCreateOption(Guid Id, string Name, string? Code = null);

public sealed record StudentGroupCreateOption(
    Guid Id,
    string Name,
    Guid ProgramId,
    int ActiveMemberCount,
    int Capacity);

public sealed record StudentCreateOptions(
    IReadOnlyList<StudentCreateOption> SchoolGrades,
    IReadOnlyList<StudentCreateOption> Programs,
    IReadOnlyList<StudentGroupCreateOption> Groups);

public interface IStudentCreationService
{
    Task<StudentCreateOptions> GetOptionsAsync(
        Guid teacherAccountId,
        Guid? programId,
        CancellationToken cancellationToken);

    Task<StudentCreateResult> CreateAsync(
        Guid teacherAccountId,
        StudentCreateCommand command,
        CancellationToken cancellationToken);
}
