// ReSharper disable UnusedMember.Global Данные поля не используются в коде так как это DTO передается в качестве ответа на запрос

namespace AccountService.Common.Models.Domain.Results;

public class PagedResult<T>(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
}
