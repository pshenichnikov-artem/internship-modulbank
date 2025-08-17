using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;
using Messaging.Events;
using Messaging.Interfaces;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IAccountService accountService,
    IMapper mapper,
    IOutboxService outboxService,
    ILogger<UpdateAccountHandler> logger) : IRequestHandler<UpdateAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var accountResult = await accountService.UpdateAccountCurrencyAsync(
            request.Id,
            request.OwnerId,
            request.Currency,
            ct);

        if (!accountResult.IsSuccess || accountResult.Data == null)
        {
            if (accountResult.CommandError != null)
                return CommandResult<object>.Failure(accountResult.CommandError.StatusCode,
                    accountResult.CommandError.Message);

            logger.LogError(
                "Сервис счетов вернул неуспешный результат без деталей ошибки для счета {AccountId}, валюта {Currency}, владелец {OwnerId} в {TimeUtc}",
                request.Id,
                request.Currency,
                request.OwnerId,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            return CommandResult<object>.Failure(500, "Ошибка конвертирования валюты");
        }

        var account = accountResult.Data;

        if (request.InterestRate.HasValue && account.Type == AccountType.Checking)
        {
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(400, "Нельзя установить процентную ставку для расчетного счета");
        }

        if (request.InterestRate is null or 0 && account.Type != AccountType.Checking)
        {
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(400, "Для Credit и Debit счетов необходимо указать процентную ставку");
        }

        mapper.Map(request, account);

        try
        {
            await accountRepository.UpdateAccountAsync(account, ct);

            var accountUpdated = new AccountUpdated(account.Id, account.OwnerId, 
                account.Currency, account.InterestRate);
            await outboxService.AddAsync(accountUpdated, ct);

            await accountRepository.CommitAsync(ct);
            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Не удалось обновить счет {AccountId}. Владелец: {OwnerId}, Валюта: {Currency}, Процентная ставка: {InterestRate}, Время (UTC): {TimeUtc}, Error: {Error}",
                request.Id,
                request.OwnerId,
                request.Currency,
                request.InterestRate?.ToString() ?? "null",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.Message);
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}
