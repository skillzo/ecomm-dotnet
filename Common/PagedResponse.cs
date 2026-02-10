namespace ECommerce.Api.Common;

public class PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = new List<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    public static PagedResponse<T> Create(IReadOnlyList<T> data, int total, int skip, int limit)
    {
        var page = limit > 0 ? (skip / limit) + 1 : 1;
        var totalPages = limit > 0 ? (int)Math.Ceiling(total / (double)limit) : 1;

        return new PagedResponse<T>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = limit,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1,
        };
    }
}