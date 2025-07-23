using AccountService.Common.Models.Domain;
using AccountService.Features.Accounts.Model;

namespace AccountService.Common.Interfaces.Repository;

public interface IAccountRepository
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();

    Task<(IEnumerable<Account> accounts, int totalCount)> GetAccountsAsync(
        List<Guid>? ownerIds = null,
        List<string>? currencies = null,
        List<AccountType>? types = null,
        bool? isActive = null,
        List<SortOrder>? sortOrders = null,
        int page = 1,
        int pageSize = 100);

    Task<Account?> GetAccountByIdAsync(Guid id);
    Task<Account?> GetAccountByIdForUpdateAsync(Guid id);
    Task<Account> CreateAccountAsync(Account account);
    Task<Account> UpdateAccountAsync(Account account);
}