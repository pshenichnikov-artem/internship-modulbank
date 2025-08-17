using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;

namespace AccountService.Common.Interfaces.Service;

public interface IAccountService
{
    Task<CommandResult<Account>> UpdateAccountCurrencyAsync(
        Guid accountId,
        Guid ownerId,
        string newCurrency,
        CancellationToken ct);
}
