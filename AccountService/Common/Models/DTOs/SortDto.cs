using AccountService.Common.Models.Domain;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Common.Models.DTOs;

[SwaggerSchema(Description = "Направление сортировки (Ascending = 0, Descending = 1)")]
public enum SortDirection
{
    Ascending,
    Descending
}

public class SortDto
{
    [SwaggerSchema(Description = "Имя поля для сортировки")]
    public string Field { get; set; } = string.Empty;
    
    [SwaggerSchema(Description = "Направление сортировки (Ascending = 0, Descending = 1)")]
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    public SortOrder ToSortOrder()
    {
        return new SortOrder(Field, Direction == SortDirection.Descending);
    }
}