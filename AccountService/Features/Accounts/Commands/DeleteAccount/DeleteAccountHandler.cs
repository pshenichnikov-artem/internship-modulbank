using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using FluentValidation;
using MediatR;
using Messaging.Events;
using Messaging.Interfaces;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountHandler(
    IAccountRepository accountRepository,
    IOutboxService outboxService,
    ILogger<DeleteAccountHandler> logger)
    : IRequestHandler<DeleteAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        try
        {
            await accountRepository.BeginTransactionAsync(ct);
            var account = await accountRepository.GetAccountByIdAsync(request.AccountId, ct);
            if (account == null) return CommandResult<object>.Failure(404, $"Счет с ID {request.AccountId} не найден");

            if (account.OwnerId != request.OwnerId)
                return CommandResult<object>.Failure(403, "У вас нет доступа к этому счету");

            if (account.Balance != 0) return CommandResult<object>.Failure(400, "Счет не пуст");

            account.IsDeleted = true;
            account.ClosedAt = DateTime.UtcNow;

            await accountRepository.UpdateAccountAsync(account, ct);

            var accountClosed = new AccountClosed(account.Id, account.OwnerId, account.Currency);
            await outboxService.AddAsync(accountClosed, ct);

            await accountRepository.CommitAsync(ct);
            return CommandResult<object>.Success();
        }
        catch (ValidationException ex)
        {
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(400, ex.Message);
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync(ct);
            logger.LogError(
                "Ошибка при удалении счёта. AccountId: {AccountId}, OwnerId: {OwnerId}, Time: {TimeUtc}, Error: {Error}",
                request.AccountId,
                request.OwnerId,
                DateTime.UtcNow,
                ex.Message);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}