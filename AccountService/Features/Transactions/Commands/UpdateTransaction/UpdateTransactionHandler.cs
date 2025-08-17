using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using MediatR;
using Messaging.Events;
using Messaging.Interfaces;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    IOutboxService outboxService,
    ILogger<UpdateTransactionHandler> logger)
    : IRequestHandler<UpdateTransactionCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateTransactionCommand request,
        CancellationToken ct)
    {
        await accountRepository.BeginTransactionAsync(ct);
        
        try
        {
            var transaction = await transactionRepository.GetTransactionByIdAsync(request.Id, ct);
            if (transaction == null)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<object>.Failure(404, $"Транзакция с ID {request.Id} не найдена");
            }

            var account = await accountRepository.GetAccountByIdAsync(transaction.AccountId, ct);
            if (account == null || account.OwnerId != request.OwnerId)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<object>.Failure(403, "У вас нет доступа к этой транзакции");
            }

            if (!string.IsNullOrEmpty(request.Description))
                transaction.Description = request.Description;

            await transactionRepository.UpdateTransactionAsync(transaction, ct);
            
            var transactionUpdated = new TransactionUpdated(transaction.Id, transaction.AccountId, 
                transaction.Description);
            await outboxService.AddAsync(transactionUpdated, ct);
            
            await accountRepository.CommitAsync(ct);
            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync(ct);
            logger.LogError(
                "Ошибка при обновлении транзакции. TransactionId: {TransactionId}, OwnerId: {OwnerId}, Description: {Description}, RequestTime: {TimeUtc}, Error: {Error}",
                request.Id, request.OwnerId, request.Description, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}
