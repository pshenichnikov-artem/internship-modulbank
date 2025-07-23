using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using FluentValidation;
using MediatR;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accountRepository)
    : IRequestHandler<DeleteAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountRepository.GetAccountByIdAsync(request.AccountId);
            if (account == null) return CommandResult<object>.Failure(404, $"Счет с ID {request.AccountId} не найден");

            account.IsDeleted = true;
            account.ClosedAt = DateTime.UtcNow;

            await accountRepository.UpdateAccountAsync(account);

            return CommandResult<object>.Success();
        }
        catch (ValidationException ex)
        {
            return CommandResult<object>.Failure(400, ex.Message);
        }
        catch (Exception ex)
        {
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}