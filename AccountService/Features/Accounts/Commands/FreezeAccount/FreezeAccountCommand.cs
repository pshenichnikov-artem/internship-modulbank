using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Accounts.Commands.FreezeAccount;

public record FreezeAccountCommand(Guid ClientId, bool IsFrozen) : IRequest<CommandResult<object>>;
