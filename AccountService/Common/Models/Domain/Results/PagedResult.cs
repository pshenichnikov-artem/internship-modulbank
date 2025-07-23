namespace AccountService.Common.Models.Domain.Results;

public class PagedResult<T>(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
}