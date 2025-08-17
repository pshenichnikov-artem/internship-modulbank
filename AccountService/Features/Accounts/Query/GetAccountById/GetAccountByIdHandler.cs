using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.Query.GetAccountById;

public class GetAccountByIdHandler(
    IAccountRepository accountRepository,
    IMapper mapper)
    : IRequestHandler<GetAccountByIdQuery, CommandResult<Dictionary<string, object?>>>
{
    public async Task<CommandResult<Dictionary<string, object?>>> Handle(GetAccountByIdQuery request,
        CancellationToken ct)
    {
        var account = await accountRepository.GetAccountByIdAsync(request.Id, ct);
        if (account == null)
            return CommandResult<Dictionary<string, object?>>.Failure(404, $"Счет с ID {request.Id} не найден");

        var accountDto = mapper.Map<AccountDto>(account);

        var normalizedFields = request.Fields?.Select(f => f.ToLowerInvariant()).ToHashSet();

        var filteredDto = accountDto
            .GetType()
            .GetProperties()
            .Where(p => normalizedFields == null || normalizedFields.Contains(p.Name.ToLowerInvariant()))
            .ToDictionary(p => p.Name, p => p.GetValue(accountDto));

        return CommandResult<Dictionary<string, object?>>.Success(filteredDto);
    }
}
