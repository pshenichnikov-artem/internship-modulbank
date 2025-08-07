using AccountService.Common.Interfaces.Jobs;
using AccountService.Common.Interfaces.Repository;
using AccountService.Features.Accounts.Commands.AccrueInterest;
using AccountService.Features.Accounts.Model;
using MediatR;
using Serilog;

namespace AccountService.Features.Accounts.Jobs;

public class AccrueInterestJob(IAccountRepository accountRepository, IMediator mediator) : IAccrueInterestJob
{
    public async Task ExecuteAsync()
    {
        Log.Warning("Запуск ежедневного начисления процентов");

        try
        {
            var (depositAccounts, _) = await accountRepository.GetAccountsAsync(
                types: [AccountType.Deposit],
                pageSize: int.MaxValue);
            var processedCount = 0;
            var errorCount = 0;

            foreach (var account in depositAccounts)
            {
                if (account.IsDeleted || account.InterestRate == null || account.InterestRate <= 0)
                    continue;

                try
                {
                    var command = new AccrueInterestCommand(account.Id);
                    var result = await mediator.Send(command);

                    if (result.IsSuccess)
                    {
                        processedCount++;
                    }
                    else
                    {
                        Log.Warning("Не удалось начислить проценты для счёта {AccountId}: {Error}",
                            account.Id, result.CommandError?.Message);
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка начисления процентов для счёта {AccountId}", account.Id);
                    errorCount++;
                }
            }

            Log.Warning("Завершено начисление процентов. Обработано: {ProcessedCount}, ошибок: {ErrorCount}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Критическая ошибка при выполнении ежедневного начисления процентов");
        }
    }
}