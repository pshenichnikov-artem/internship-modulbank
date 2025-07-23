using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Transactions.Commands.CreateTransaction;

namespace AccountService.Common.Interfaces;

public interface ITransactionProcessor
{
    Task<CommandResult<Guid>> CreateTransactionAsync(CreateTransactionCommand request,
        CancellationToken cancellationToken);

    Task<CommandResult<object>> CancelTransactionAsync(Guid transactionId, CancellationToken cancellationToken);
}