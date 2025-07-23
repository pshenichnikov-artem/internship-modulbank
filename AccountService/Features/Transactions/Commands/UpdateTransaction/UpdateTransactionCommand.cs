using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор транзакции (заполняется автоматически из URL)")]
    public Guid Id { get; set; }
    
    [SwaggerSchema(Description = "Новое описание транзакции (до 500 символов)")]
    public string Description { get; init; } = string.Empty;
}