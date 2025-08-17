using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор транзакции")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Новое описание транзакции (до 500 символов)")]
    public string Description { get; init; } = string.Empty;

    [SwaggerSchema(Description = "Идентификатор владельца счета(можно передавать null, на сервере всегда 1 владелец")]
    public Guid OwnerId { get; init; }
}
