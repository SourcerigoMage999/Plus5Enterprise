namespace Plus5.Api.Contracts;

public sealed record PagedResponse<T>
{
    public PagedResponse(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, PaginationQuery.MaximumPageSize);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public long TotalCount { get; }

    public long TotalPages => TotalCount == 0
        ? 0
        : ((TotalCount - 1) / PageSize) + 1;
}
