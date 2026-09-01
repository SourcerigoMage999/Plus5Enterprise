using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Students;

namespace Plus5.Api.Students;

public static class StudentDossierEndpoints
{
    public static IEndpointRouteBuilder MapStudentDossier(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapVersionOneApi()
            .MapGroup("/students")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy)
            .MapGet("/{studentId:guid}", GetAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid studentId,
        HttpContext context,
        IStudentDossierQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (studentId == Guid.Empty)
        {
            return TypedResults.NotFound();
        }

        var dossier = await query.GetAsync(
            teacherAccountId,
            studentId,
            cancellationToken);

        return dossier is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(dossier));
    }

    private static StudentDossierResponse Map(StudentDossier dossier) => new(
        dossier.Id,
        dossier.FirstName,
        dossier.LastName,
        dossier.Nickname,
        dossier.DateOfBirth,
        dossier.SchoolName,
        dossier.Gender,
        dossier.Email,
        dossier.Phone,
        MapStatus(dossier.Status),
        MapReference(dossier.SchoolGrade),
        dossier.Program is null ? null : MapReference(dossier.Program),
        dossier.DeliveryMode.HasValue ? MapDeliveryMode(dossier.DeliveryMode.Value) : null,
        dossier.Group is null ? null : MapReference(dossier.Group),
        dossier.PrimaryGuardian is null
            ? null
            : new StudentDossierGuardianResponse(
                dossier.PrimaryGuardian.Id,
                dossier.PrimaryGuardian.FirstName,
                dossier.PrimaryGuardian.LastName,
                dossier.PrimaryGuardian.Relationship,
                dossier.PrimaryGuardian.Email,
                dossier.PrimaryGuardian.Phone),
        dossier.NextSession is null ? null : MapSession(dossier.NextSession),
        dossier.LastHeldSession is null ? null : MapSession(dossier.LastHeldSession));

    private static StudentDossierSessionResponse MapSession(StudentDossierSession session) => new(
        session.Id,
        session.Title,
        session.StartsAtUtc,
        session.EndsAtUtc,
        session.TimeZoneId,
        MapDeliveryMode(session.DeliveryMode),
        session.Group is null ? null : MapReference(session.Group));

    private static StudentDossierReferenceResponse MapReference(StudentDossierReference reference) =>
        new(reference.Id, reference.Name, reference.Code);

    private static string MapDeliveryMode(StudentListDeliveryMode mode) => mode switch
    {
        StudentListDeliveryMode.Individual => "individual",
        StudentListDeliveryMode.Group => "group",
        _ => throw new InvalidOperationException("Unsupported delivery mode."),
    };

    private static string MapStatus(StudentListStatus status) => status switch
    {
        StudentListStatus.Active => "active",
        StudentListStatus.OnHold => "on_hold",
        StudentListStatus.Inactive => "inactive",
        _ => throw new InvalidOperationException("Unsupported Student status."),
    };

    public sealed record StudentDossierReferenceResponse(Guid Id, string Name, string? Code);

    public sealed record StudentDossierGuardianResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string? Relationship,
        string? Email,
        string? Phone);

    public sealed record StudentDossierSessionResponse(
        Guid Id,
        string? Title,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        string TimeZoneId,
        string DeliveryMode,
        StudentDossierReferenceResponse? Group);

    public sealed record StudentDossierResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string? Nickname,
        DateOnly? DateOfBirth,
        string? SchoolName,
        string? Gender,
        string? Email,
        string? Phone,
        string Status,
        StudentDossierReferenceResponse SchoolGrade,
        StudentDossierReferenceResponse? Program,
        string? DeliveryMode,
        StudentDossierReferenceResponse? Group,
        StudentDossierGuardianResponse? PrimaryGuardian,
        StudentDossierSessionResponse? NextSession,
        StudentDossierSessionResponse? LastHeldSession);
}
