using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Model;

[SwaggerSchema(Description = "Тип банковского счета")]
public enum AccountType
{
    Credit,
    Deposit,
    Checking
}