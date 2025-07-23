using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Common.Models.DTOs;

public class PaginationDto
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private int _pageSize = DefaultPageSize;

    [SwaggerSchema(Description = "Номер страницы, начиная с 1")]
    public int Page { get; set; } = 1;

    [SwaggerSchema(Description = "Размер страницы (максимум 100)")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}