using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper) : IRequestHandler<CreateAccountCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var account = mapper.Map<Account>(request);

            await accountRepository.CreateAccountAsync(account);

            return CommandResult<Guid>.Success(account.Id);
        }
        catch (Exception ex)
        {
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }
}