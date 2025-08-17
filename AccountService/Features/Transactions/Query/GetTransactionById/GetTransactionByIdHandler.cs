using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.Query.GetTransactionById;

public class GetTransactionByIdHandler(
    ITransactionRepository transactionRepository,
    IMapper mapper)
    : IRequestHandler<GetTransactionByIdQuery, CommandResult<Dictionary<string, object?>>>
{
    public async Task<CommandResult<Dictionary<string, object?>>> Handle(GetTransactionByIdQuery request,
        CancellationToken ct)
    {
        var transaction = await transactionRepository.GetTransactionByIdAsync(request.Id, ct);
        if (transaction == null)
            return CommandResult<Dictionary<string, object?>>.Failure(404, $"Транзакция с ID {request.Id} не найдена");

        var transactionDto = mapper.Map<TransactionDto>(transaction);

        var normalizedFields = request.Fields?.Select(f => f.ToLower());

        var filteredDto = transactionDto
            .GetType()
            .GetProperties()
            .Where(p => normalizedFields == null || normalizedFields.Contains(p.Name.ToLowerInvariant()))
            .ToDictionary(p => p.Name, p => p.GetValue(transactionDto));

        return CommandResult<Dictionary<string, object?>>.Success(filteredDto);
    }
}
