using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Models;

[SwaggerSchema(Description = "Тип транзакции")]
public enum TransactionType
{
    Credit,
    Debit,
    Transfer
}