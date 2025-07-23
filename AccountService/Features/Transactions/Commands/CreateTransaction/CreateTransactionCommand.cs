using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Transactions.Models;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Commands.CreateTransaction;

public record CreateTransactionCommand : IRequest<CommandResult<Guid>>
{
    [SwaggerSchema(Description = "Идентификатор счета, по которому проводится транзакция")]
    public Guid AccountId { get; init; }

    [SwaggerSchema(Description = "Идентификатор владельца счета(можно передавать null, на сервере всегда 1 владелец")]
    public Guid? OwnerId { get; set; }

    [SwaggerSchema(Description =
        "Идентификатор счета контрагента (не указывается для операций внесения/снятия наличных), TransactionType указать Transfer")]
    public Guid? CounterpartyAccountId { get; init; }

    [SwaggerSchema(Description = "Сумма транзакции (положительное число)")]
    public decimal Amount { get; init; }

    [SwaggerSchema(Description = "Трехбуквенный код валюты (USD, EUR, RUB)")]
    public string Currency { get; init; } = string.Empty;

    [SwaggerSchema(Description = TransactionTypeDescriptions.Description)]
    public TransactionType Type { get; init; }

    [SwaggerSchema(Description = "Описание транзакции (до 500 символов)")]
    public string Description { get; init; } = string.Empty;
}