using System.ComponentModel.DataAnnotations;

namespace Plus5.Api.Contracts;

public sealed record PaginationQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = DefaultPage;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}
