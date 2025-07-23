using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<UpdateTransactionCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetTransactionByIdAsync(request.Id);
        if (transaction == null)
            return CommandResult<object>.Failure(404, $"Транзакция с ID {request.Id} не найдена");

        if (!string.IsNullOrEmpty(request.Description))
            transaction.Description = request.Description;

        await transactionRepository.UpdateTransactionAsync(transaction);

        return CommandResult<object>.Success();
    }
}