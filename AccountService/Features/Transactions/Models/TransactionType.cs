using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Models;

public static class TransactionTypeDescriptions
{
    public const string Description =
        "Тип транзакции:\n" +
        " - Credit = 0 — Кредит (поступление средств)\n" +
        " - Debit = 1 — Дебет (списание средств)\n" +
        " - Transfer = 2 — Перевод между счетами";
}

[SwaggerSchema(Description = "Тип транзакции")]
public enum TransactionType
{
    Credit,
    Debit,
    Transfer
}