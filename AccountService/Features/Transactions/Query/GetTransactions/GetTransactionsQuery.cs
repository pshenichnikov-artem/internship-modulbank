using AccountService.Common.Models.Domain.Results;
using AccountService.Common.Models.DTOs;
using AccountService.Features.Transactions.Models;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Query.GetTransactions;

public class GetTransactionsQuery : IRequest<CommandResult<PagedResult<TransactionDto>>>
{
    [SwaggerSchema(Description = "Фильтр по параметрам транзакций")]
    public TransactionsQueryFilter Filter { get; init; } = new();

    [SwaggerSchema(Description = "Параметры пагинации")]
    public PaginationDto Pagination { get; init; } = new();

    [SwaggerSchema(Description =
        "Параметры сортировки. Доступные поля:\n" +
        " - Id\n" +
        " - AccountId\n" +
        " - CounterpartyAccountId\n" +
        " - Amount\n" +
        " - Currency\n" +
        " - Timestamp")]
    public List<SortDto> SortOrders { get; init; } = [];
}

public record TransactionsQueryFilter
{
    [SwaggerSchema(Description = "Список идентификаторов счетов для фильтрации транзакций")]
    public List<Guid>? AccountIds { get; init; }

    [SwaggerSchema(Description = "Список трехбуквенных кодов валют для фильтрации (USD, EUR, RUB)")]
    public List<string>? Currencies { get; init; }

    [SwaggerSchema(Description = "Начальная дата для фильтрации транзакций")]
    public DateTime? FromDate { get; init; }

    [SwaggerSchema(Description = "Конечная дата для фильтрации транзакций")]
    public DateTime? ToDate { get; init; }

    [SwaggerSchema(Description = TransactionTypeDescriptions.Description)]
    public List<TransactionType>? Types { get; init; }
}
