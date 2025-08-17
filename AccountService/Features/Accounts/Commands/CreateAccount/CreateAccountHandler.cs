using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AutoMapper;
using MediatR;
using Messaging.Events;
using Messaging.Interfaces;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IClientService clientService,
    ICurrencyService currencyService,
    IMapper mapper,
    IOutboxService outboxService,
    ILogger<CreateAccountHandler> logger) : IRequestHandler<CreateAccountCommand, CommandResult<Guid>>
{
    public async Task<CommandResult<Guid>> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        try
        {
            if (!await currencyService.IsSupportedCurrencyAsync(request.Currency, ct))
                return CommandResult<Guid>.Failure(400, $"Валюта {request.Currency} не поддерживается");

            if (!await clientService.IsClientExistsAsync(request.OwnerId, ct))
                return CommandResult<Guid>.Failure(400, "Клиент не найден");

            var account = mapper.Map<Account>(request);

            await accountRepository.BeginTransactionAsync(ct);

            await accountRepository.CreateAccountAsync(account, ct);

            var accountOpened = new AccountOpened(account.Id, account.OwnerId,
                account.Currency, account.Type.ToString());
            await outboxService.AddAsync(accountOpened, ct);

            await accountRepository.CommitAsync(ct);
            return CommandResult<Guid>.Success(account.Id);
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync(ct);
            logger.LogError(
                "Ошибка при создании счёта. OwnerId: {OwnerId}, Currency: {Currency}, Time: {TimeUtc}. Error: {Error}",
                request.OwnerId,
                request.Currency,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.Message);
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }
}