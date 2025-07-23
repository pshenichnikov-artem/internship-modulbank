using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.Query.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id, IEnumerable<string>? Fields = null)
    : IRequest<CommandResult<Dictionary<string, object?>>>
{
    [SwaggerSchema(Description = "Идентификатор транзакции")]
    public Guid Id { get; } = Id;

    [SwaggerSchema(Description = "Список полей для включения в ответ (если не указано, возвращаются все поля)")]
    public IEnumerable<string>? Fields { get; } = Fields;
}