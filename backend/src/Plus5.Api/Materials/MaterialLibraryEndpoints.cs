using System.ComponentModel.DataAnnotations;
using Plus5.Api.Contracts;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Materials;

namespace Plus5.Api.Materials;

public static class MaterialLibraryEndpoints
{
    private const int SearchMaxLength = 100;
    private const int FilterTextMaxLength = 160;

    public static IEndpointRouteBuilder MapMaterialLibrary(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi()
            .MapGroup("/materials")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        group.MapGet("/", GetMaterialsAsync);
        group.MapGet("/overview", GetOverviewAsync);

        return endpoints;
    }

    private static async Task<IResult> GetMaterialsAsync(
        [AsParameters] MaterialLibraryRequest request,
        HttpContext context,
        IMaterialLibraryQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var page = await query.GetPageAsync(
            teacherAccountId,
            new MaterialLibraryCriteria(
                request.Page ?? PaginationQuery.DefaultPage,
                request.PageSize ?? 24,
                request.Ownership.HasValue
                    ? (MaterialLibraryOwnership)request.Ownership.Value
                    : MaterialLibraryOwnership.Mine,
                request.Sort.HasValue
                    ? (MaterialLibrarySort)request.Sort.Value
                    : MaterialLibrarySort.Newest,
                request.Search,
                request.Subject,
                request.ProgramId,
                request.SchoolGradeId,
                request.MaterialTypeCode,
                request.Tag),
            cancellationToken);

        return TypedResults.Ok(new PagedResponse<MaterialLibraryItemResponse>(
            page.Items.Select(MapItem).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    private static async Task<IResult> GetOverviewAsync(
        [AsParameters] MaterialLibraryOverviewRequest request,
        HttpContext context,
        IMaterialLibraryQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var overview = await query.GetOverviewAsync(
            teacherAccountId,
            request.Ownership.HasValue
                ? (MaterialLibraryOwnership)request.Ownership.Value
                : MaterialLibraryOwnership.Mine,
            cancellationToken);

        return TypedResults.Ok(new MaterialLibraryOverviewResponse(
            overview.Subjects,
            overview.Programs.Select(MapOption).ToList(),
            overview.SchoolGrades.Select(MapOption).ToList(),
            overview.MaterialTypes,
            overview.Tags,
            overview.MaterialTypeCounts
                .Select(item => new MaterialLibraryTypeCountResponse(item.Code, item.Count))
                .ToList(),
            overview.RecentlyAdded
                .Select(item => new MaterialLibraryRecentItemResponse(
                    item.Id,
                    item.Title,
                    item.AddedAtUtc))
                .ToList()));
    }

    private static MaterialLibraryItemResponse MapItem(MaterialLibraryItem item) => new(
        item.Id,
        item.VersionId,
        item.Title,
        item.Description,
        item.MaterialTypeCode,
        item.Subject,
        item.ProgramId.HasValue && item.ProgramName is not null
            ? new MaterialLibraryFilterOptionResponse(item.ProgramId.Value, item.ProgramName, null)
            : null,
        item.SchoolGradeId.HasValue && item.SchoolGradeName is not null
            ? new MaterialLibraryFilterOptionResponse(
                item.SchoolGradeId.Value,
                item.SchoolGradeName,
                item.SchoolGradeCode)
            : null,
        item.ProficiencyLevelId.HasValue && item.ProficiencyLevelName is not null
            ? new MaterialLibraryFilterOptionResponse(
                item.ProficiencyLevelId.Value,
                item.ProficiencyLevelName,
                item.ProficiencyLevelCode)
            : null,
        item.FileFormat switch
        {
            MaterialLibraryFileFormat.Pdf => "pdf",
            MaterialLibraryFileFormat.Docx => "docx",
            MaterialLibraryFileFormat.Pptx => "pptx",
            MaterialLibraryFileFormat.Mp4 => "mp4",
            MaterialLibraryFileFormat.Zip => "zip",
            _ => throw new InvalidOperationException("Unsupported material file format."),
        },
        item.AddedAtUtc,
        item.IsOwner,
        item.ShareAccess switch
        {
            MaterialLibraryShareAccess.View => "view",
            MaterialLibraryShareAccess.Use => "use",
            null => null,
            _ => throw new InvalidOperationException("Unsupported material share access."),
        },
        item.Tags);

    private static MaterialLibraryFilterOptionResponse MapOption(
        MaterialLibraryFilterOption item) => new(item.Id, item.Name, item.Code);

    public sealed record MaterialLibraryRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int? Page { get; init; }

        [Range(1, PaginationQuery.MaximumPageSize)]
        public int? PageSize { get; init; }

        [Range((int)MaterialLibraryOwnership.Mine, (int)MaterialLibraryOwnership.SharedWithMe)]
        public int? Ownership { get; init; }

        [Range((int)MaterialLibrarySort.Newest, (int)MaterialLibrarySort.Title)]
        public int? Sort { get; init; }

        [StringLength(SearchMaxLength)]
        public string? Search { get; init; }

        [StringLength(FilterTextMaxLength)]
        public string? Subject { get; init; }

        public Guid? ProgramId { get; init; }

        public Guid? SchoolGradeId { get; init; }

        [StringLength(64)]
        public string? MaterialTypeCode { get; init; }

        [StringLength(64)]
        public string? Tag { get; init; }

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

    public sealed record MaterialLibraryOverviewRequest
    {
        [Range((int)MaterialLibraryOwnership.Mine, (int)MaterialLibraryOwnership.SharedWithMe)]
        public int? Ownership { get; init; }
    }

    public sealed record MaterialLibraryItemResponse(
        Guid Id,
        Guid VersionId,
        string Title,
        string? Description,
        string MaterialTypeCode,
        string? Subject,
        MaterialLibraryFilterOptionResponse? Program,
        MaterialLibraryFilterOptionResponse? SchoolGrade,
        MaterialLibraryFilterOptionResponse? ProficiencyLevel,
        string FileFormat,
        DateTimeOffset AddedAtUtc,
        bool IsOwner,
        string? ShareAccess,
        IReadOnlyList<string> Tags);

    public sealed record MaterialLibraryFilterOptionResponse(
        Guid Id,
        string Name,
        string? Code);

    public sealed record MaterialLibraryTypeCountResponse(string Code, long Count);

    public sealed record MaterialLibraryRecentItemResponse(
        Guid Id,
        string Title,
        DateTimeOffset AddedAtUtc);

    public sealed record MaterialLibraryOverviewResponse(
        IReadOnlyList<string> Subjects,
        IReadOnlyList<MaterialLibraryFilterOptionResponse> Programs,
        IReadOnlyList<MaterialLibraryFilterOptionResponse> SchoolGrades,
        IReadOnlyList<string> MaterialTypes,
        IReadOnlyList<string> Tags,
        IReadOnlyList<MaterialLibraryTypeCountResponse> MaterialTypeCounts,
        IReadOnlyList<MaterialLibraryRecentItemResponse> RecentlyAdded);
}
