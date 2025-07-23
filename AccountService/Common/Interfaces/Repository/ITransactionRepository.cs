using AccountService.Common.Models.Domain;
using AccountService.Features.Transactions.Models;

namespace AccountService.Common.Interfaces.Repository;

public interface ITransactionRepository
{
    Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<Transaction> Transactions, int TotalCount)> GetTransactionsAsync(
        List<Guid>? accountIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<TransactionType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task CreateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
}