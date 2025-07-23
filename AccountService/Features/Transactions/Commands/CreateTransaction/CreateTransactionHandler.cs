using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionHandler(
    ITransactionService transactionService)
    : IRequestHandler<CreateTransactionCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        return await transactionService.CreateTransactionAsync(request, cancellationToken);
    }
}