using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Students;

namespace Plus5.Api.Students;

public static class StudentCreationEndpoints
{
    public static IEndpointRouteBuilder MapStudentCreation(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi()
            .MapGroup("/students")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("/create-options", GetOptionsAsync);
        group.MapPost("/", CreateAsync).RequireCsrf();

        return endpoints;
    }

    private static async Task<IResult> GetOptionsAsync(
        Guid? programId,
        HttpContext context,
        IStudentCreationService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (programId == Guid.Empty)
        {
            return Problem(400, "invalid_program_id", "Program identifier must not be empty.");
        }

        var options = await service.GetOptionsAsync(
            teacherAccountId,
            programId,
            cancellationToken);

        return TypedResults.Ok(new StudentCreateOptionsResponse(
            options.SchoolGrades.Select(MapOption).ToList(),
            options.Programs.Select(MapOption).ToList(),
            options.Groups.Select(group => new StudentGroupCreateOptionResponse(
                group.Id,
                group.Name,
                group.ProgramId,
                group.ActiveMemberCount,
                group.Capacity)).ToList()));
    }

    private static async Task<IResult> CreateAsync(
        StudentCreateRequest request,
        HttpContext context,
        IStudentCreationService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.CreateAsync(
            teacherAccountId,
            new StudentCreateCommand(
                request.FirstName,
                request.LastName,
                request.SchoolGradeId,
                request.SchoolName,
                request.DateOfBirth,
                request.Gender,
                request.Email,
                request.Phone,
                request.ProgramId,
                ParseDeliveryMode(request.DeliveryMode),
                request.GroupId,
                ParseStatus(request.Status),
                request.Guardian is null
                    ? null
                    : new StudentGuardianInput(
                        request.Guardian.FirstName,
                        request.Guardian.LastName,
                        request.Guardian.Email,
                        request.Guardian.Phone)),
            cancellationToken);

        if (result.StudentId.HasValue)
        {
            return TypedResults.Created(
                $"/api/v1/students/{result.StudentId.Value}",
                new StudentCreatedResponse(result.StudentId.Value));
        }

        return result.Failure switch
        {
            StudentCreateFailure.SchoolGradeNotFound =>
                Problem(404, "school_grade_not_found", "The selected school grade was not found."),
            StudentCreateFailure.ProgramNotFound =>
                Problem(404, "program_not_found", "The selected program was not found."),
            StudentCreateFailure.GroupNotFound =>
                Problem(404, "group_not_found", "The selected group was not found."),
            StudentCreateFailure.GroupUnavailable =>
                Problem(409, "group_unavailable", "The selected group is no longer available."),
            StudentCreateFailure.GroupCapacityReached =>
                Problem(409, "group_capacity_reached", "The selected group has reached its capacity."),
            StudentCreateFailure.GroupProgramMismatch =>
                Problem(409, "group_program_mismatch", "The selected group does not belong to the selected program."),
            StudentCreateFailure.ConcurrencyConflict =>
                Problem(409, "concurrency_conflict", "The group changed while the student was being saved. Try again."),
            _ => throw new InvalidOperationException("Unsupported student creation result."),
        };
    }

    private static StudentCreationDeliveryMode? ParseDeliveryMode(string? value) => value switch
    {
        null => null,
        "individual" => StudentCreationDeliveryMode.Individual,
        "group" => StudentCreationDeliveryMode.Group,
        _ => throw new InvalidOperationException("Request validation did not reject delivery mode."),
    };

    private static StudentCreationStatus ParseStatus(string value) => value switch
    {
        "active" => StudentCreationStatus.Active,
        "on_hold" => StudentCreationStatus.OnHold,
        "inactive" => StudentCreationStatus.Inactive,
        _ => throw new InvalidOperationException("Request validation did not reject status."),
    };

    private static StudentCreateOptionResponse MapOption(StudentCreateOption option) =>
        new(option.Id, option.Name, option.Code);

    private static IResult Problem(int status, string code, string title) =>
        Results.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static RouteHandlerBuilder RequireCsrf(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Problem(400, "invalid_csrf_token", "The anti-forgery token is missing or invalid.");
            }

            return await next(context);
        });

    public sealed record StudentCreateRequest : IValidatableObject
    {
        [Required, StringLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; init; } = string.Empty;

        public Guid SchoolGradeId { get; init; }

        [StringLength(200)]
        public string? SchoolName { get; init; }

        public DateOnly? DateOfBirth { get; init; }

        [StringLength(64)]
        public string? Gender { get; init; }

        [EmailAddress, StringLength(320)]
        public string? Email { get; init; }

        [StringLength(32)]
        public string? Phone { get; init; }

        public Guid? ProgramId { get; init; }

        public string? DeliveryMode { get; init; }

        public Guid? GroupId { get; init; }

        [Required]
        public string Status { get; init; } = "active";

        public StudentGuardianRequest? Guardian { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SchoolGradeId == Guid.Empty)
            {
                yield return Error("School grade is required.", nameof(SchoolGradeId));
            }

            if (ProgramId == Guid.Empty)
            {
                yield return Error("Program identifier must not be empty.", nameof(ProgramId));
            }

            if (GroupId == Guid.Empty)
            {
                yield return Error("Group identifier must not be empty.", nameof(GroupId));
            }

            if (Status is not ("active" or "on_hold" or "inactive"))
            {
                yield return Error("Status is invalid.", nameof(Status));
            }

            if (DeliveryMode is not (null or "individual" or "group"))
            {
                yield return Error("Delivery mode is invalid.", nameof(DeliveryMode));
            }

            if (ProgramId.HasValue != (DeliveryMode is not null))
            {
                yield return Error(
                    "Program and delivery mode must either both be provided or both be omitted.",
                    nameof(ProgramId),
                    nameof(DeliveryMode));
            }

            if (DeliveryMode == "group" && !GroupId.HasValue)
            {
                yield return Error("Group is required for group delivery.", nameof(GroupId));
            }

            if (DeliveryMode != "group" && GroupId.HasValue)
            {
                yield return Error("Group is only allowed for group delivery.", nameof(GroupId));
            }

            if (Guardian is not null)
            {
                if (string.IsNullOrWhiteSpace(Guardian.FirstName))
                {
                    yield return Error("Guardian first name is required.", nameof(Guardian.FirstName));
                }

                if (string.IsNullOrWhiteSpace(Guardian.LastName))
                {
                    yield return Error("Guardian last name is required.", nameof(Guardian.LastName));
                }

                if (!string.IsNullOrWhiteSpace(Guardian.Email) &&
                    !new EmailAddressAttribute().IsValid(Guardian.Email))
                {
                    yield return Error("Guardian email is invalid.", nameof(Guardian.Email));
                }
            }
        }

        private static ValidationResult Error(string message, params string[] members) =>
            new(message, members);
    }

    public sealed record StudentGuardianRequest(
        [property: StringLength(100)] string FirstName,
        [property: StringLength(100)] string LastName,
        [property: StringLength(320)] string? Email,
        [property: StringLength(32)] string? Phone);

    public sealed record StudentCreateOptionResponse(Guid Id, string Name, string? Code);

    public sealed record StudentGroupCreateOptionResponse(
        Guid Id,
        string Name,
        Guid ProgramId,
        int ActiveMemberCount,
        int Capacity);

    public sealed record StudentCreateOptionsResponse(
        IReadOnlyList<StudentCreateOptionResponse> SchoolGrades,
        IReadOnlyList<StudentCreateOptionResponse> Programs,
        IReadOnlyList<StudentGroupCreateOptionResponse> Groups);

    public sealed record StudentCreatedResponse(Guid Id);
}
