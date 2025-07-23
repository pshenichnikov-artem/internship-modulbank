using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public record DeleteAccountCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор счета для удаления")]
    public Guid AccountId { get; init; }

    public Guid OwnerId { get; init; }
}