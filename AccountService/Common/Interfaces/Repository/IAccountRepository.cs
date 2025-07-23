using AccountService.Common.Models.Domain;
using AccountService.Features.Accounts.Model;

namespace AccountService.Common.Interfaces.Repository;

public interface IAccountRepository
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account> CreateAccountAsync(Account account, CancellationToken cancellationToken = default);
    Task<Account> UpdateAccountAsync(Account account, CancellationToken cancellationToken = default);
}