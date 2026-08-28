using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Plus5.Api.Contracts;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Students;

namespace Plus5.Api.Students;

public static class StudentListEndpoints
{
    private const int SearchMaxLength = 100;

    public static IEndpointRouteBuilder MapStudentList(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi()
            .MapGroup("/students")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("/", GetStudentsAsync);
        group.MapGet("/overview", GetOverviewAsync);

        return endpoints;
    }

    private static async Task<IResult> GetStudentsAsync(
        [AsParameters] StudentListRequest request,
        HttpContext context,
        IStudentListQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var page = await query.GetPageAsync(
            teacherAccountId,
            new StudentListCriteria(
                request.Page ?? PaginationQuery.DefaultPage,
                request.PageSize ?? PaginationQuery.DefaultPageSize,
                request.Search,
                request.ProgramId,
                request.DeliveryMode.HasValue
                    ? (StudentListDeliveryMode)request.DeliveryMode.Value
                    : null,
                request.Status.HasValue
                    ? (StudentListStatus)request.Status.Value
                    : null,
                request.SchoolGradeId),
            cancellationToken);

        return TypedResults.Ok(new PagedResponse<StudentListItemResponse>(
            page.Items.Select(MapItem).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    private static async Task<IResult> GetOverviewAsync(
        HttpContext context,
        IStudentListQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var overview = await query.GetOverviewAsync(teacherAccountId, cancellationToken);
        return TypedResults.Ok(new StudentListOverviewResponse(
            overview.TotalCount,
            overview.ActiveCount,
            overview.OnHoldCount,
            overview.InactiveCount,
            overview.WithoutProgramCount,
            overview.ProgramCounts
                .Select(item => new StudentProgramCountResponse(
                    item.ProgramId,
                    item.Name,
                    item.StudentCount))
                .ToList(),
            overview.Programs
                .Select(item => new StudentFilterOptionResponse(item.Id, item.Name, null))
                .ToList(),
            overview.SchoolGrades
                .Select(item => new StudentFilterOptionResponse(item.Id, item.Name, item.Code))
                .ToList()));
    }

    private static StudentListItemResponse MapItem(StudentListItem item) => new(
        item.Id,
        item.FirstName,
        item.LastName,
        item.Nickname,
        new StudentReferenceResponse(
            item.SchoolGradeId,
            item.SchoolGradeName,
            item.SchoolGradeCode),
        item.ProgramId.HasValue && item.ProgramName is not null
            ? new StudentReferenceResponse(item.ProgramId.Value, item.ProgramName, null)
            : null,
        item.DeliveryMode switch
        {
            StudentListDeliveryMode.Individual => "individual",
            StudentListDeliveryMode.Group => "group",
            _ => null,
        },
        item.GroupId.HasValue && item.GroupName is not null
            ? new StudentReferenceResponse(item.GroupId.Value, item.GroupName, null)
            : null,
        item.Status switch
        {
            StudentListStatus.Active => "active",
            StudentListStatus.OnHold => "on_hold",
            StudentListStatus.Inactive => "inactive",
            _ => throw new InvalidOperationException("Unsupported Student status."),
        },
        item.LastSessionAtUtc);

    public sealed record StudentListRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int? Page { get; init; }

        [Range(1, PaginationQuery.MaximumPageSize)]
        public int? PageSize { get; init; }

        [StringLength(SearchMaxLength)]
        public string? Search { get; init; }

        public Guid? ProgramId { get; init; }

        [Range(
            (int)StudentListDeliveryMode.Individual,
            (int)StudentListDeliveryMode.Group)]
        public int? DeliveryMode { get; init; }

        [Range((int)StudentListStatus.Active, (int)StudentListStatus.Inactive)]
        public int? Status { get; init; }

        public Guid? SchoolGradeId { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ProgramId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Program identifier must not be empty.",
                    [nameof(ProgramId)]);
            }

            if (SchoolGradeId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "School grade identifier must not be empty.",
                    [nameof(SchoolGradeId)]);
            }
        }
    }

    public sealed record StudentListItemResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string? Nickname,
        StudentReferenceResponse SchoolGrade,
        StudentReferenceResponse? Program,
        string? DeliveryMode,
        StudentReferenceResponse? Group,
        string Status,
        DateTimeOffset? LastSessionAtUtc);

    public sealed record StudentReferenceResponse(Guid Id, string Name, string? Code);

    public sealed record StudentListOverviewResponse(
        long TotalCount,
        long ActiveCount,
        long OnHoldCount,
        long InactiveCount,
        long WithoutProgramCount,
        IReadOnlyList<StudentProgramCountResponse> ProgramCounts,
        IReadOnlyList<StudentFilterOptionResponse> Programs,
        IReadOnlyList<StudentFilterOptionResponse> SchoolGrades);

    public sealed record StudentProgramCountResponse(
        Guid ProgramId,
        string Name,
        long StudentCount);

    public sealed record StudentFilterOptionResponse(Guid Id, string Name, string? Code);
}
