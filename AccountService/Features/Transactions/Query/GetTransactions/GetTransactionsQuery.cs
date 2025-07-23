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

    [SwaggerSchema(Description = "Параметры сортировки")]
    public List<SortDto> SortOrders { get; init; } = new();
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

    [SwaggerSchema(Description = "Список типов транзакций для фильтрации (Credit = 0, Debit = 1)")]
    public List<TransactionType>? Types { get; init; }
}