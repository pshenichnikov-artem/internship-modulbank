using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using MediatR;

namespace AccountService.Features.Accounts.Commands.UpdateAccountField;

public class UpdateAccountFieldHandler(
    IAccountRepository accountRepository,
    IAccountService accountService,
    ILogger<UpdateAccountFieldHandler> logger)
    : IRequestHandler<UpdateAccountFieldCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateAccountFieldCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return request.FieldName.Equals("Currency", StringComparison.OrdinalIgnoreCase)
                ? await HandleCurrencyUpdateAsync(request, cancellationToken)
                : await HandleInterestRateUpdateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Неожиданная ошибка при обновлении поля '{FieldName}' счета {AccountId}. OwnerId: {OwnerId}, RequestTime: {TimeUtc}",
                request.FieldName, request.Id, request.OwnerId, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await SafeRollbackAsync(cancellationToken);
            return CommandResult<object>.Failure(500, "Внутренняя ошибка сервера");
        }
    }

    private async Task<CommandResult<object>> HandleCurrencyUpdateAsync(UpdateAccountFieldCommand req,
        CancellationToken ct)
    {
        var currencyCode = (string)req.FieldValue;

        var updateResult = await accountService.UpdateAccountCurrencyAsync(req.Id, req.OwnerId, currencyCode, ct);

        if (!updateResult.IsSuccess || updateResult.Data == null)
        {
            if (updateResult.CommandError != null)
                return CommandResult<object>.Failure(updateResult.CommandError.StatusCode,
                    updateResult.CommandError.Message);

            logger.LogError(
                "Сервис счетов вернул ошибку без деталей для счета {AccountId}, Currency: {Currency}, OwnerId: {OwnerId}, RequestTime: {TimeUtc}",
                req.Id, currencyCode, req.OwnerId, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            return CommandResult<object>.Failure(500, "Ошибка конвертирования валюты");
        }

        try
        {
            await accountRepository.UpdateAccountAsync(updateResult.Data, ct);
            await accountRepository.CommitAsync(ct);
            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при обновлении счета {AccountId} при смене валюты на '{Currency}'. OwnerId: {OwnerId}, RequestTime: {TimeUtc}",
                req.Id, currencyCode, req.OwnerId, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await SafeRollbackAsync(ct);
            return CommandResult<object>.Failure(500, "Не удалось сохранить изменения");
        }
    }

    private async Task<CommandResult<object>> HandleInterestRateUpdateAsync(UpdateAccountFieldCommand req,
        CancellationToken ct)
    {
        var account = await accountRepository.GetAccountByIdAsync(req.Id, ct);
        if (account == null)
            return CommandResult<object>.Failure(404, $"Счет с ID {req.Id} не найден");

        if (account.OwnerId != req.OwnerId)
            return CommandResult<object>.Failure(403, "У вас нет доступа к этому счету");

        if (account.Type == AccountType.Checking)
            return CommandResult<object>.Failure(400, "Нельзя установить процентную ставку для расчетного счета");

        account.InterestRate = Convert.ToDecimal(req.FieldValue);

        try
        {
            await accountRepository.UpdateAccountAsync(account, ct);
            await accountRepository.CommitAsync(ct);
            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при сохранении изменений поля InterestRate счета {AccountId}. OwnerId: {OwnerId}, RequestTime: {TimeUtc}",
                req.Id, req.OwnerId, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await SafeRollbackAsync(ct);
            return CommandResult<object>.Failure(500, "Не удалось сохранить изменения");
        }
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await accountRepository.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при попытке отката транзакции");
        }
    }
}