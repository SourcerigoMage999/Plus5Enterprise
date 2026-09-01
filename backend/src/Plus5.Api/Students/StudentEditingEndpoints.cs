using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Students;

namespace Plus5.Api.Students;

public static class StudentEditingEndpoints
{
    public static IEndpointRouteBuilder MapStudentEditing(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi()
            .MapGroup("/students")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("/{studentId:guid}/edit", GetAsync);
        group.MapPut("/{studentId:guid}", UpdateAsync).RequireEditCsrf();
        group.MapPost("/{studentId:guid}/archive", ArchiveAsync).RequireEditCsrf();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid studentId,
        HttpContext context,
        IStudentEditingService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var model = await service.GetAsync(teacherAccountId, studentId, cancellationToken);
        return model is null ? TypedResults.NotFound() : TypedResults.Ok(Map(model));
    }

    private static async Task<IResult> UpdateAsync(
        Guid studentId,
        StudentEditRequest request,
        HttpContext context,
        IStudentEditingService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.UpdateAsync(
            teacherAccountId,
            studentId,
            new StudentEditCommand(
                Convert.FromBase64String(request.RowVersion),
                request.FirstName,
                request.LastName,
                request.SchoolGradeId,
                request.Nickname,
                request.DateOfBirth,
                request.SchoolName,
                request.Gender,
                request.Email,
                request.Phone,
                request.ProgramId,
                ParseDeliveryMode(request.DeliveryMode),
                request.GroupId,
                ParseStatus(request.Status),
                request.Guardians.Select(guardian => new StudentEditGuardianInput(
                    guardian.Id,
                    guardian.FirstName,
                    guardian.LastName,
                    guardian.Relationship,
                    guardian.Email,
                    guardian.Phone,
                    guardian.IsPrimary)).ToList()),
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new StudentEditSavedResponse(Convert.ToBase64String(result.RowVersion!)))
            : Failure(result.Failure);
    }

    private static async Task<IResult> ArchiveAsync(
        Guid studentId,
        StudentArchiveRequest request,
        HttpContext context,
        IStudentEditingService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.ArchiveAsync(
            teacherAccountId,
            studentId,
            Convert.FromBase64String(request.RowVersion),
            cancellationToken);
        return result.Succeeded ? TypedResults.NoContent() : Failure(result.Failure);
    }

    private static StudentEditResponse Map(StudentEditModel model) => new(
        model.Id,
        model.FirstName,
        model.LastName,
        model.Nickname,
        model.DateOfBirth,
        model.SchoolName,
        model.Gender,
        model.Email,
        model.Phone,
        model.SchoolGradeId,
        model.ProgramId,
        model.DeliveryMode.HasValue ? MapDeliveryMode(model.DeliveryMode.Value) : null,
        model.GroupId,
        MapStatus(model.Status),
        model.UpdatedAtUtc,
        Convert.ToBase64String(model.RowVersion),
        model.Guardians.Select(guardian => new StudentEditGuardianResponse(
            guardian.Id,
            guardian.FirstName,
            guardian.LastName,
            guardian.Relationship,
            guardian.Email,
            guardian.Phone,
            guardian.IsPrimary)).ToList());

    private static IResult Failure(StudentEditFailure failure) => failure switch
    {
        StudentEditFailure.StudentNotFound => Problem(404, "student_not_found", "The student was not found."),
        StudentEditFailure.SchoolGradeNotFound => Problem(404, "school_grade_not_found", "The selected school grade was not found."),
        StudentEditFailure.ProgramNotFound => Problem(404, "program_not_found", "The selected program was not found."),
        StudentEditFailure.GroupNotFound => Problem(404, "group_not_found", "The selected group was not found."),
        StudentEditFailure.GroupUnavailable => Problem(409, "group_unavailable", "The selected group is unavailable."),
        StudentEditFailure.GroupCapacityReached => Problem(409, "group_capacity_reached", "The selected group has reached its capacity."),
        StudentEditFailure.GroupProgramMismatch => Problem(409, "group_program_mismatch", "The selected group does not belong to the selected program."),
        StudentEditFailure.GuardianNotFound => Problem(409, "guardian_not_found", "A guardian no longer exists."),
        StudentEditFailure.GuardianSetMismatch => Problem(409, "guardian_set_mismatch", "Existing guardians must remain in this edit."),
        StudentEditFailure.MultiplePrimaryGuardians => Problem(409, "multiple_primary_guardians", "Only one guardian can be primary."),
        StudentEditFailure.ConcurrencyConflict => Problem(409, "concurrency_conflict", "The student changed while being saved. Reload and try again."),
        _ => throw new InvalidOperationException("Unsupported student edit result."),
    };

    private static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static RouteHandlerBuilder RequireEditCsrf(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>()
                    .ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Problem(400, "invalid_csrf_token", "The anti-forgery token is missing or invalid.");
            }

            return await next(context);
        });

    private static StudentCreationDeliveryMode? ParseDeliveryMode(string? value) => value switch
    {
        null => null,
        "individual" => StudentCreationDeliveryMode.Individual,
        "group" => StudentCreationDeliveryMode.Group,
        _ => throw new InvalidOperationException("Request validation did not reject delivery mode."),
    };

    private static string MapDeliveryMode(StudentCreationDeliveryMode value) => value switch
    {
        StudentCreationDeliveryMode.Individual => "individual",
        StudentCreationDeliveryMode.Group => "group",
        _ => throw new InvalidOperationException("Unsupported delivery mode."),
    };

    private static StudentCreationStatus ParseStatus(string value) => value switch
    {
        "active" => StudentCreationStatus.Active,
        "on_hold" => StudentCreationStatus.OnHold,
        "inactive" => StudentCreationStatus.Inactive,
        _ => throw new InvalidOperationException("Request validation did not reject status."),
    };

    private static string MapStatus(StudentCreationStatus value) => value switch
    {
        StudentCreationStatus.Active => "active",
        StudentCreationStatus.OnHold => "on_hold",
        StudentCreationStatus.Inactive => "inactive",
        _ => throw new InvalidOperationException("Unsupported status."),
    };

    public sealed record StudentEditRequest : IValidatableObject
    {
        public string RowVersion { get; init; } = string.Empty;
        [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
        [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
        public Guid SchoolGradeId { get; init; }
        [StringLength(100)] public string? Nickname { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        [StringLength(200)] public string? SchoolName { get; init; }
        [StringLength(64)] public string? Gender { get; init; }
        [EmailAddress, StringLength(320)] public string? Email { get; init; }
        [StringLength(32)] public string? Phone { get; init; }
        public Guid? ProgramId { get; init; }
        public string? DeliveryMode { get; init; }
        public Guid? GroupId { get; init; }
        [Required] public string Status { get; init; } = "active";
        [Required] public IReadOnlyList<StudentEditGuardianRequest> Guardians { get; init; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!TryBase64(RowVersion)) yield return Error("Row version is invalid.", nameof(RowVersion));
            if (SchoolGradeId == Guid.Empty) yield return Error("School grade is required.", nameof(SchoolGradeId));
            if (ProgramId == Guid.Empty) yield return Error("Program identifier must not be empty.", nameof(ProgramId));
            if (GroupId == Guid.Empty) yield return Error("Group identifier must not be empty.", nameof(GroupId));
            if (Status is not ("active" or "on_hold" or "inactive")) yield return Error("Status is invalid.", nameof(Status));
            if (DeliveryMode is not (null or "individual" or "group")) yield return Error("Delivery mode is invalid.", nameof(DeliveryMode));
            if (ProgramId.HasValue != (DeliveryMode is not null)) yield return Error("Program and delivery mode must be provided together.", nameof(ProgramId), nameof(DeliveryMode));
            if (DeliveryMode == "group" && !GroupId.HasValue) yield return Error("Group is required for group delivery.", nameof(GroupId));
            if (DeliveryMode != "group" && GroupId.HasValue) yield return Error("Group is only allowed for group delivery.", nameof(GroupId));
            if (Guardians is null)
            {
                yield return Error("Guardians are required.", nameof(Guardians));
                yield break;
            }

            if (Guardians.Count > 10) yield return Error("At most ten guardians are supported.", nameof(Guardians));
            if (Guardians.Count(guardian => guardian.IsPrimary) > 1) yield return Error("Only one guardian can be primary.", nameof(Guardians));
            if (Guardians.Where(guardian => guardian.Id.HasValue).Select(guardian => guardian.Id).Distinct().Count() != Guardians.Count(guardian => guardian.Id.HasValue)) yield return Error("Guardian identifiers must be unique.", nameof(Guardians));
            foreach (var guardian in Guardians)
            {
                if (guardian is null)
                {
                    yield return Error("Guardian entry is invalid.", nameof(Guardians));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(guardian.FirstName) || string.IsNullOrWhiteSpace(guardian.LastName)) yield return Error("Guardian first and last name are required.", nameof(Guardians));
                if (!string.IsNullOrWhiteSpace(guardian.Email) && !new EmailAddressAttribute().IsValid(guardian.Email)) yield return Error("Guardian email is invalid.", nameof(Guardians));
            }
        }

        private static bool TryBase64(string? value)
        {
            if (value is null) return false;
            try { _ = Convert.FromBase64String(value); return true; }
            catch (FormatException) { return false; }
        }

        private static ValidationResult Error(string message, params string[] members) => new(message, members);
    }

    public sealed record StudentEditGuardianRequest(
        Guid? Id,
        [property: Required, StringLength(100)] string FirstName,
        [property: Required, StringLength(100)] string LastName,
        [property: StringLength(100)] string? Relationship,
        [property: EmailAddress, StringLength(320)] string? Email,
        [property: StringLength(32)] string? Phone,
        bool IsPrimary);

    public sealed record StudentArchiveRequest(string RowVersion) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsValidBase64(RowVersion))
            {
                yield return new ValidationResult("Row version is invalid.", [nameof(RowVersion)]);
            }
        }

        private static bool IsValidBase64(string? value)
        {
            if (value is null) return false;
            try { _ = Convert.FromBase64String(value); return true; }
            catch (FormatException) { return false; }
        }
    }

    public sealed record StudentEditGuardianResponse(Guid Id, string FirstName, string LastName, string? Relationship, string? Email, string? Phone, bool IsPrimary);
    public sealed record StudentEditResponse(Guid Id, string FirstName, string LastName, string? Nickname, DateOnly? DateOfBirth, string? SchoolName, string? Gender, string? Email, string? Phone, Guid SchoolGradeId, Guid? ProgramId, string? DeliveryMode, Guid? GroupId, string Status, DateTimeOffset UpdatedAtUtc, string RowVersion, IReadOnlyList<StudentEditGuardianResponse> Guardians);
    public sealed record StudentEditSavedResponse(string RowVersion);
}
