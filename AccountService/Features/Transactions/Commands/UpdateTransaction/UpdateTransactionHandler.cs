using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository)
    : IRequestHandler<UpdateTransactionCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetTransactionByIdAsync(request.Id, cancellationToken);
        if (transaction == null)
            return CommandResult<object>.Failure(404, $"Транзакция с ID {request.Id} не найдена");

        var account = await accountRepository.GetAccountByIdAsync(transaction.AccountId, cancellationToken);
        if (account == null || account.OwnerId != request.OwnerId)
            return CommandResult<object>.Failure(403, "У вас нет доступа к этой транзакции");

        if (!string.IsNullOrEmpty(request.Description))
            transaction.Description = request.Description;

        await transactionRepository.UpdateTransactionAsync(transaction, cancellationToken);

        return CommandResult<object>.Success();
    }
}