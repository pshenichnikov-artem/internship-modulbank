using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IClientService clientService,
    ICurrencyService currencyService,
    IMapper mapper,
    ILogger<CreateAccountHandler> logger) : IRequestHandler<CreateAccountCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await currencyService.IsSupportedCurrencyAsync(request.Currency, cancellationToken))
                return CommandResult<Guid>.Failure(400, $"Валюта {request.Currency} не поддерживается");

            if (!await clientService.IsClientExistsAsync(request.OwnerId, cancellationToken))
                return CommandResult<Guid>.Failure(400, "Клиент не найден");

            var account = mapper.Map<Account>(request);

            await accountRepository.CreateAccountAsync(account, cancellationToken);

            return CommandResult<Guid>.Success(account.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при создании счёта. OwnerId: {OwnerId}, Currency: {Currency}, Time: {TimeUtc}. Подробности ошибки: {ExceptionDetails}",
                request.OwnerId,
                request.Currency,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.ToString());
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }
}