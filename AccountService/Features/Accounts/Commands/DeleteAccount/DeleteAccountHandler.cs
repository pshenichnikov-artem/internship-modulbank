using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using FluentValidation;
using MediatR;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accountRepository, ILogger<DeleteAccountHandler> logger)
    : IRequestHandler<DeleteAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountRepository.GetAccountByIdAsync(request.AccountId, cancellationToken);
            if (account == null) return CommandResult<object>.Failure(404, $"Счет с ID {request.AccountId} не найден");

            if (account.OwnerId != request.OwnerId)
                return CommandResult<object>.Failure(403, "У вас нет доступа к этому счету");

            if (account.Balance != 0) return CommandResult<object>.Failure(400, "Счет не пуст");

            account.IsDeleted = true;
            account.ClosedAt = DateTime.UtcNow;

            await accountRepository.UpdateAccountAsync(account, cancellationToken);

            return CommandResult<object>.Success();
        }
        catch (ValidationException ex)
        {
            return CommandResult<object>.Failure(400, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при удалении счёта. AccountId: {AccountId}, OwnerId: {OwnerId}, Time: {TimeUtc}",
                request.AccountId,
                request.OwnerId,
                DateTime.UtcNow);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}