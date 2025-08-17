using AccountService.Common.Models.Domain.Results;
using AccountService.Common.Models.DTOs;
using AccountService.Features.Accounts.Model;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Query.GetAccounts;

public class GetAccountsQuery : IRequest<CommandResult<PagedResult<AccountDto>>>
{
    [SwaggerSchema(Description = "Фильтры для поиска счетов")]
    public AccountQueryFilter Filters { get; init; } = new();

    [SwaggerSchema(Description = "Параметры пагинации")]
    public PaginationDto Pagination { get; init; } = new();

    [SwaggerSchema(Description =
        "Параметры сортировки. Допустимые поля для сортировки:\n" +
        "- Id — идентификатор счета\n" +
        "- OwnerId — идентификатор владельца счета\n" +
        "- Currency — трехбуквенный код валюты (например, USD, EUR, RUB)\n" +
        "- Balance — текущий баланс счета\n" +
        "- Type — тип счета (0 - Credit, 1 - Deposit, 2- Checking)\n" +
        "- InterestRate — процентная ставка\n" +
        "- OpenedAt — дата открытия счета\n" +
        "- ClosedAt — дата закрытия счета (если счет закрыт)")]

    public List<SortDto>? SortOrders { get; init; } = [];
}

public record AccountQueryFilter
{
    [SwaggerSchema(Description = "Список идентификаторов владельцев счетов для фильтрации")]
    public List<Guid>? OwnerIds { get; init; }

    [SwaggerSchema(Description = "Список трёхбуквенных кодов валют для фильтрации (например, USD, EUR, RUB)")]
    public List<string>? Currencies { get; init; }

    [SwaggerSchema(Description = AccountTypeDescriptions.Description)]
    public List<AccountType>? Types { get; init; }
}
