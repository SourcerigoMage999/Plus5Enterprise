namespace Plus5.Application.Materials;

public enum MaterialLibraryOwnership
{
    Mine = 1,
    SharedWithMe = 2,
}

public enum MaterialLibrarySort
{
    Newest = 1,
    Oldest = 2,
    Title = 3,
}

public enum MaterialLibraryFileFormat
{
    Pdf = 1,
    Docx = 2,
    Pptx = 3,
    Mp4 = 4,
    Zip = 5,
}

public enum MaterialLibraryShareAccess
{
    View = 1,
    Use = 2,
}

public sealed record MaterialLibraryCriteria(
    int Page,
    int PageSize,
    MaterialLibraryOwnership Ownership,
    MaterialLibrarySort Sort,
    string? Search,
    string? Subject,
    Guid? ProgramId,
    Guid? SchoolGradeId,
    string? MaterialTypeCode,
    string? Tag);

public sealed record MaterialLibraryItem(
    Guid Id,
    Guid VersionId,
    string Title,
    string? Description,
    string MaterialTypeCode,
    string? Subject,
    Guid? ProgramId,
    string? ProgramName,
    Guid? SchoolGradeId,
    string? SchoolGradeCode,
    string? SchoolGradeName,
    Guid? ProficiencyLevelId,
    string? ProficiencyLevelCode,
    string? ProficiencyLevelName,
    MaterialLibraryFileFormat FileFormat,
    DateTimeOffset AddedAtUtc,
    bool IsOwner,
    MaterialLibraryShareAccess? ShareAccess,
    IReadOnlyList<string> Tags);

public sealed record MaterialLibraryPage(
    IReadOnlyList<MaterialLibraryItem> Items,
    int Page,
    int PageSize,
    long TotalCount);

public sealed record MaterialLibraryFilterOption(Guid Id, string Name, string? Code);

public sealed record MaterialLibraryTypeCount(string Code, long Count);

public sealed record MaterialLibraryRecentItem(Guid Id, string Title, DateTimeOffset AddedAtUtc);

public sealed record MaterialLibraryOverview(
    IReadOnlyList<string> Subjects,
    IReadOnlyList<MaterialLibraryFilterOption> Programs,
    IReadOnlyList<MaterialLibraryFilterOption> SchoolGrades,
    IReadOnlyList<string> MaterialTypes,
    IReadOnlyList<string> Tags,
    IReadOnlyList<MaterialLibraryTypeCount> MaterialTypeCounts,
    IReadOnlyList<MaterialLibraryRecentItem> RecentlyAdded);

public interface IMaterialLibraryQuery
{
    Task<MaterialLibraryPage> GetPageAsync(
        Guid teacherAccountId,
        MaterialLibraryCriteria criteria,
        CancellationToken cancellationToken);

    Task<MaterialLibraryOverview> GetOverviewAsync(
        Guid teacherAccountId,
        MaterialLibraryOwnership ownership,
        CancellationToken cancellationToken);
}
