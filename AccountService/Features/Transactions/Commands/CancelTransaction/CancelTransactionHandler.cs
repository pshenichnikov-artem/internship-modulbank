using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

public class CancelTransactionHandler(
    ITransactionService transactionService)
    : IRequestHandler<CancelTransactionCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(CancelTransactionCommand request,
        CancellationToken cancellationToken)
    {
        return await transactionService.CancelTransactionAsync(request.TransactionId, request.OwnerId,
            cancellationToken);
    }
}