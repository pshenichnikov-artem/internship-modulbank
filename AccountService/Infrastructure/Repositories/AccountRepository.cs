using System.Data;
using AccountService.Common.Extensions;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain;
using AccountService.Features.Accounts.Model;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountService.Infrastructure.Repositories;

public class AccountRepository(ApplicationDbContext context) : IAccountRepository
{
    private IDbContextTransaction? _transaction;

    public async Task<Account> CreateAccountAsync(Account account, CancellationToken ct = default)
    {
        context.Accounts.Add(account);
        await context.SaveChangesAsync(ct);
        return account;
    }

    public async Task<Account> UpdateAccountAsync(Account account, CancellationToken ct = default)
    {
        context.Entry(account).State = EntityState.Modified;
        await context.SaveChangesAsync(ct);
        return account;
    }

    public async Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var query = context.Accounts.AsQueryable();

        if (ownerIds is { Count: > 0 }) query = query.Where(a => ownerIds.Contains(a.OwnerId));

        if (currencies is { Count: > 0 }) query = query.Where(a => currencies.Contains(a.Currency));

        if (types is { Count: > 0 }) query = query.Where(a => types.Contains(a.Type));

        var totalCount = await query.CountAsync(ct);

        query = sortOrders is { Count: > 0 } ? query.Sort(sortOrders) : query.OrderBy(a => a.Id);

        query = query.Paginate(page, pageSize);

        var accounts = await query.ToListAsync(ct);

        return (accounts, totalCount);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        await Task.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task<Account?> GetAccountByIdForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Accounts
            .FromSqlRaw("SELECT *, xmin FROM \"Accounts\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<int> UpdateAccountsFrozenStatusAsync(Guid ownerId, bool isFrozen, CancellationToken ct = default)
    {
        return await context.Accounts
            .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
            .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsFrozen, isFrozen), ct);
    }
}