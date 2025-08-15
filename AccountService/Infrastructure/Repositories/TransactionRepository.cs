using AccountService.Common.Extensions;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return transaction;
    }

    public async Task<(List<Transaction> Transactions, int TotalCount)> GetTransactionsAsync(
        List<Guid>? accountIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<TransactionType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions.AsQueryable();

        if (accountIds is { Count: > 0 })
            query = query.Where(t => accountIds.Contains(t.AccountId));

        if (fromDate.HasValue)
            query = query.Where(t => t.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.Timestamp <= toDate.Value);

        if (types is { Count: > 0 })
            query = query.Where(t => types.Contains(t.Type));

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortOrders is { Count: > 0 } ? query.Sort(sortOrders) : query.OrderByDescending(t => t.Timestamp);

        query = query.Paginate(page, pageSize);

        var transactions = await query.ToListAsync(cancellationToken);

        return (transactions, totalCount);
    }

    public async Task CreateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        context.Entry(transaction).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}