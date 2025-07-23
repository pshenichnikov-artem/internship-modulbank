using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Models;

public class TransactionDto
{
    [SwaggerSchema(Description = "Уникальный идентификатор транзакции")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Идентификатор счета, по которому проведена транзакция")]
    public Guid AccountId { get; set; }

    [SwaggerSchema(Description = "Идентификатор счета контрагента (null для операций внесения/снятия наличных)")]
    public Guid? CounterpartyAccountId { get; set; }

    [SwaggerSchema(Description = "Сумма транзакции (положительное число)")]
    public decimal Amount { get; set; }

    [SwaggerSchema(Description = "Трехбуквенный код валюты (USD, EUR, RUB)")]
    public string Currency { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Тип транзакции (Credit = 0, Debit = 1)")]
    public TransactionType Type { get; set; }

    [SwaggerSchema(Description = "Описание транзакции (до 500 символов)")]
    public string Description { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Дата и время проведения транзакции")]
    public DateTime Timestamp { get; set; }

    public bool IsCanceled { get; set; } = false;
}