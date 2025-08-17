using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Model;

public static class AccountTypeDescriptions
{
    public const string Description =
        "Тип счета:\n" +
        " - Credit = 0 — Кредитный счет\n" +
        " - Deposit = 1 — Депозитный счет\n" +
        " - Checking = 2 — Расчетный счет";
}

[SwaggerSchema(Description = AccountTypeDescriptions.Description)]
public enum AccountType
{
    Credit,
    Deposit,
    Checking
}
