namespace Plus5.Application.Students;

public sealed record StudentDossierReference(Guid Id, string Name, string? Code = null);

public sealed record StudentDossierGuardian(
    Guid Id,
    string FirstName,
    string LastName,
    string? Relationship,
    string? Email,
    string? Phone);

public sealed record StudentDossierSession(
    Guid Id,
    string? Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    StudentListDeliveryMode DeliveryMode,
    StudentDossierReference? Group);

public sealed record StudentDossier(
    Guid Id,
    string FirstName,
    string LastName,
    string? Nickname,
    DateOnly? DateOfBirth,
    string? SchoolName,
    string? Gender,
    string? Email,
    string? Phone,
    StudentListStatus Status,
    StudentDossierReference SchoolGrade,
    StudentDossierReference? Program,
    StudentListDeliveryMode? DeliveryMode,
    StudentDossierReference? Group,
    StudentDossierGuardian? PrimaryGuardian,
    StudentDossierSession? NextSession,
    StudentDossierSession? LastHeldSession);

public interface IStudentDossierQuery
{
    Task<StudentDossier?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken);
}
