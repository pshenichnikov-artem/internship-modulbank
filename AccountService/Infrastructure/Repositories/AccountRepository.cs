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

    public async Task<Account> CreateAccountAsync(Account account)
    {
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    public async Task<Account> UpdateAccountAsync(Account account)
    {
        context.Entry(account).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return account;
    }

    public async Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        bool? isActive = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100)
    {
        var query = context.Accounts.AsQueryable();

        if (ownerIds != null && ownerIds.Any()) query = query.Where(a => ownerIds.Contains(a.OwnerId));

        if (currencies != null && currencies.Any()) query = query.Where(a => currencies.Contains(a.Currency));

        if (types != null && types.Any()) query = query.Where(a => types.Contains(a.Type));

        if (isActive.HasValue)
            query = isActive.Value
                ? query.Where(a => a.ClosedAt == null)
                : query.Where(a => a.ClosedAt != null);

        var totalCount = await query.CountAsync();

        if (sortOrders != null && sortOrders.Any())
            query = query.Sort(sortOrders);
        else
            query = query.OrderBy(a => a.Id);

        query = query.Paginate(page, pageSize);

        var accounts = await query.ToListAsync();

        return (accounts, totalCount);
    }

    public async Task BeginTransactionAsync()
    {
        // Реальный код транзакции (для поддерживаемых БД)
        // _transaction = await context.Database.BeginTransactionAsync();

        _transaction = null;
        await Task.CompletedTask;
    }

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task<Account?> GetAccountByIdForUpdateAsync(Guid id)
    {
        return await context.Accounts
            // .FromSqlRaw("SELECT * FROM \"Accounts\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid id)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id);

        return account;
    }
}