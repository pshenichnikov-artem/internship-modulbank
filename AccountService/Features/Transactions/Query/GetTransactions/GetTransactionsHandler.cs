using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.Query.GetTransactions;

public class GetTransactionsHandler(ITransactionRepository transactionRepository, IMapper mapper)
    : IRequestHandler<GetTransactionsQuery, CommandResult<PagedResult<TransactionDto>>>
{
    public async Task<CommandResult<PagedResult<TransactionDto>>> Handle(GetTransactionsQuery request,
        CancellationToken ct)
    {
        try
        {
            var (transactions, totalCount) = await transactionRepository.GetTransactionsAsync(
                request.Filter.AccountIds,
                request.Filter.FromDate,
                request.Filter.ToDate,
                request.Filter.Types,
                request.SortOrders.Select(so => so.ToSortOrder()).ToList(),
                request.Pagination.Page,
                request.Pagination.PageSize,
                ct);

            var transactionsDto = mapper.Map<List<TransactionDto>>(transactions);

            var pagedResult = new PagedResult<TransactionDto>(
                transactionsDto,
                totalCount,
                request.Pagination.Page,
                request.Pagination.PageSize);

            return CommandResult<PagedResult<TransactionDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return CommandResult<PagedResult<TransactionDto>>.Failure(500, ex.Message);
        }
    }
}
