using AccountService.Common.Extensions;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;

namespace AccountService.Features.Accounts;

public class AccountService(
    ICurrencyService currencyService,
    IAccountRepository accountRepository,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<CommandResult<Account>> UpdateAccountCurrencyAsync(
        Guid accountId,
        Guid ownerId,
        string newCurrency,
        CancellationToken cancellationToken)
    {
        await accountRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await accountRepository.GetAccountByIdForUpdateAsync(accountId, cancellationToken);
            if (account == null)
            {
                await accountRepository.RollbackAsync(cancellationToken);
                return CommandResult<Account>.Failure(404, $"Счет с ID {accountId} не найден");
            }

            if (account.OwnerId != ownerId)
            {
                await accountRepository.RollbackAsync(cancellationToken);
                return CommandResult<Account>.Failure(403, "У вас нет доступа к этому счету");
            }

            if (string.IsNullOrWhiteSpace(newCurrency) ||
                string.Equals(account.Currency, newCurrency, StringComparison.OrdinalIgnoreCase))
                return CommandResult<Account>.Success(account);

            if (!await currencyService.IsSupportedCurrencyAsync(newCurrency, cancellationToken))
            {
                await accountRepository.RollbackAsync(cancellationToken);
                return CommandResult<Account>.Failure(400, $"Валюта {newCurrency} не поддерживается");
            }

            var newBalance = currencyService.Convert(account.Balance, account.Currency, newCurrency);
            account.Balance = Math.Round(newBalance, 2);
            account.Currency = newCurrency;

            return CommandResult<Account>.Success(account);
        }
        catch (Exception pgEx) when (pgEx.IsConcurrencyException())
        {
            logger.LogWarning(pgEx, "Конфликт параллелизма при обновлении валюты счета");
            await accountRepository.RollbackAsync(cancellationToken);
            return CommandResult<Account>.Failure(409, "Попробуйте снова.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при обработке счета {AccountId} для клиента {OwnerId}: попытка изменить валюту на '{NewCurrency}' в {Timestamp}. Подробности ошибки: {ExceptionDetails}",
                accountId,
                ownerId,
                newCurrency,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.ToString());
            await accountRepository.RollbackAsync(cancellationToken);
            return CommandResult<Account>.Failure(500, ex.Message);
        }
    }
}