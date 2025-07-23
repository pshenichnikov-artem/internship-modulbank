using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

public record CancelTransactionCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор транзакции для отмены")]
    public Guid TransactionId { get; init; }
}