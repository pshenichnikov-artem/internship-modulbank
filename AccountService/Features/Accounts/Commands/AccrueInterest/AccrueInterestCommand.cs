using AccountService.Common.Models.Domain.Results;
using MediatR;

namespace AccountService.Features.Accounts.Commands.AccrueInterest;

public record AccrueInterestCommand(Guid AccountId) : IRequest<CommandResult<Guid>>;