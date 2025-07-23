using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Query.GetAccountById;

public record GetAccountByIdQuery(
    [property: SwaggerSchema(Description = "Идентификатор счета")]
    Guid Id,
    [property:
        SwaggerSchema(Description = "Список полей для включения в ответ (если не указано, возвращаются все поля)")]
    IEnumerable<string>? Fields = null)
    : IRequest<CommandResult<Dictionary<string, object?>>>;