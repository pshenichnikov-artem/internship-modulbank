using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.Query.GetAccounts;

public class GetAccountsHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetAccountsQuery, CommandResult<PagedResult<AccountDto>>>
{
    public async Task<CommandResult<PagedResult<AccountDto>>> Handle(GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var (accounts, totalCount) = await accountRepository.GetAccountsAsync(
            request.Filters.OwnerIds,
            request.Filters.Currencies,
            request.Filters.Types,
            request.SortOrders?.Select(so => so.ToSortOrder()).ToList(),
            request.Pagination.Page,
            request.Pagination.PageSize,
            cancellationToken);

        var accountsDto = mapper.Map<List<AccountDto>>(accounts);

        var pagedResult = new PagedResult<AccountDto>(
            accountsDto,
            totalCount,
            request.Pagination.Page,
            request.Pagination.PageSize);

        return CommandResult<PagedResult<AccountDto>>.Success(pagedResult);
    }
}