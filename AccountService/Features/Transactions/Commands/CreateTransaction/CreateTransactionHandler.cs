using AccountService.Common.Interfaces;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionHandler(
    ITransactionProcessor transactionProcessor)
    : IRequestHandler<CreateTransactionCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        return await transactionProcessor.CreateTransactionAsync(request, cancellationToken);
    }
}