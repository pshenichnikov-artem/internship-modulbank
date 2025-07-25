﻿using AccountService.Common.Extensions;
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

    public async Task<Account> CreateAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> UpdateAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        context.Entry(account).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = context.Accounts.AsQueryable();

        if (ownerIds != null && ownerIds.Any()) query = query.Where(a => ownerIds.Contains(a.OwnerId));

        if (currencies != null && currencies.Any()) query = query.Where(a => currencies.Contains(a.Currency));

        if (types != null && types.Any()) query = query.Where(a => types.Contains(a.Type));

        var totalCount = await query.CountAsync(cancellationToken);

        if (sortOrders != null && sortOrders.Any())
            query = query.Sort(sortOrders);
        else
            query = query.OrderBy(a => a.Id);

        query = query.Paginate(page, pageSize);

        var accounts = await query.ToListAsync(cancellationToken);

        return (accounts, totalCount);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Реальный код транзакции (для поддерживаемых БД) в in-memory ошибка
        // _transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        _transaction = null;
        await Task.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task<Account?> GetAccountByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            // .FromSqlRaw("SELECT * FROM \"Accounts\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}