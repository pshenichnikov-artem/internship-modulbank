using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper) : IRequestHandler<UpdateAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountRepository.GetAccountByIdAsync(request.Id);
            if (account == null) return CommandResult<object>.Failure(404, $"Счет с ID {request.Id} не найден");

            mapper.Map(request, account);

            await accountRepository.UpdateAccountAsync(account);

            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}