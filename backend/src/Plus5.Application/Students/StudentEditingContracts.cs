namespace Plus5.Application.Students;

public sealed record StudentEditGuardian(
    Guid Id,
    string FirstName,
    string LastName,
    string? Relationship,
    string? Email,
    string? Phone,
    bool IsPrimary);

public sealed record StudentEditModel(
    Guid Id,
    string FirstName,
    string LastName,
    string? Nickname,
    DateOnly? DateOfBirth,
    string? SchoolName,
    string? Gender,
    string? Email,
    string? Phone,
    Guid SchoolGradeId,
    Guid? ProgramId,
    StudentCreationDeliveryMode? DeliveryMode,
    Guid? GroupId,
    StudentCreationStatus Status,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion,
    IReadOnlyList<StudentEditGuardian> Guardians);

public sealed record StudentEditGuardianInput(
    Guid? Id,
    string FirstName,
    string LastName,
    string? Relationship,
    string? Email,
    string? Phone,
    bool IsPrimary);

public sealed record StudentEditCommand(
    byte[] RowVersion,
    string FirstName,
    string LastName,
    Guid SchoolGradeId,
    string? Nickname,
    DateOnly? DateOfBirth,
    string? SchoolName,
    string? Gender,
    string? Email,
    string? Phone,
    Guid? ProgramId,
    StudentCreationDeliveryMode? DeliveryMode,
    Guid? GroupId,
    StudentCreationStatus Status,
    IReadOnlyList<StudentEditGuardianInput> Guardians);

public enum StudentEditFailure
{
    None = 0,
    StudentNotFound,
    SchoolGradeNotFound,
    ProgramNotFound,
    GroupNotFound,
    GroupUnavailable,
    GroupCapacityReached,
    GroupProgramMismatch,
    GuardianNotFound,
    GuardianSetMismatch,
    MultiplePrimaryGuardians,
    ConcurrencyConflict,
}

public sealed record StudentEditResult(StudentEditFailure Failure, byte[]? RowVersion = null)
{
    public bool Succeeded => Failure == StudentEditFailure.None;

    public static StudentEditResult Success(byte[] rowVersion) =>
        new(StudentEditFailure.None, rowVersion);

    public static StudentEditResult Failed(StudentEditFailure failure) => new(failure);
}

public interface IStudentEditingService
{
    Task<StudentEditModel?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken);

    Task<StudentEditResult> UpdateAsync(
        Guid teacherAccountId,
        Guid studentId,
        StudentEditCommand command,
        CancellationToken cancellationToken);

    Task<StudentEditResult> ArchiveAsync(
        Guid teacherAccountId,
        Guid studentId,
        byte[] rowVersion,
        CancellationToken cancellationToken);
}
