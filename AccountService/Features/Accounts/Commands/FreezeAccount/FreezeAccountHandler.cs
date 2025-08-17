using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Accounts.Commands.FreezeAccount;

public class FreezeAccountHandler(IAccountRepository accountRepository, ILogger<FreezeAccountHandler> logger)
    : IRequestHandler<FreezeAccountCommand, CommandResult<object>>
{
    public async Task<CommandResult<object>> Handle(FreezeAccountCommand request, CancellationToken ct)
    {
        try
        {
            await accountRepository.UpdateAccountsFrozenStatusAsync(request.ClientId,
                request.IsFrozen,
                ct);

            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Ошибка при {Action} счетов клиента {ClientId}. Время: {TimeUtc}, Error: {Error}",
                request.IsFrozen ? "блокировке" : "разблокировке",
                request.ClientId,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.Message);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }
}