using AccountService.Common.Models.Domain.Results;
using AccountService.Common.Models.DTOs;
using AccountService.Features.Accounts.Model;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Query.GetAccounts;

public class GetAccountsQuery : IRequest<CommandResult<PagedResult<AccountDto>>>
{
    public AccountQueryFilers Filters { get; init; } = new();

    [SwaggerSchema(Description = "Параметры пагинации")]
    public PaginationDto PaginationDto { get; init; } = new();

    [SwaggerSchema(Description = "Параметры сортировки")]
    public List<SortDto> SortOrders { get; init; } = [];
}

public record AccountQueryFilers
{
    [SwaggerSchema(Description = "Список идентификаторов владельцев счетов для фильтрации")]
    public List<Guid>? OwnerIds { get; init; }

    [SwaggerSchema(Description = "Список трехбуквенных кодов валют для фильтрации (USD, EUR, RUB)")]
    public List<string>? Currencies { get; init; }

    [SwaggerSchema(Description = "Список типов счетов для фильтрации (Credit = 0, Deposit = 1, Checking = 2)")]
    public List<AccountType>? Types { get; init; }

    [SwaggerSchema(Description = "Фильтр по статусу счета: true - только активные, false - только закрытые")]
    public bool? IsActive { get; init; }
}