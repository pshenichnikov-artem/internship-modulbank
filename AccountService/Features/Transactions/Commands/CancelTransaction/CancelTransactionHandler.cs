using AccountService.Common.Interfaces;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

public class CancelTransactionHandler(
    ITransactionProcessor transactionProcessor)
    : IRequestHandler<CancelTransactionCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(CancelTransactionCommand request,
        CancellationToken cancellationToken)
    {
        return await transactionProcessor.CancelTransactionAsync(request.TransactionId, cancellationToken);
    }
}