using AccountService.Common.Models.Domain;
using AccountService.Features.Accounts.Model;

namespace AccountService.Common.Interfaces.Repository;

public interface IAccountRepository
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);

    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetAccountByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<Account> CreateAccountAsync(Account account, CancellationToken ct = default);
    Task<Account> UpdateAccountAsync(Account account, CancellationToken ct = default);
    Task<int> UpdateAccountsFrozenStatusAsync(Guid ownerId, bool isFrozen, CancellationToken ct = default);
}
