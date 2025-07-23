using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Query.GetAccountById;

public record GetAccountByIdQuery(
    [SwaggerSchema(Description = "Идентификатор счета (GUID)")]
    Guid Id,
    [SwaggerSchema(Description =
        "Список полей для включения в ответ. Если не указан, возвращаются все поля.\n" +
        "Пример значений: \"Id\", \"OwnerId\", \"Currency\", \"Type\", \"Balance\", \"InterestRate\", \"IsActive\", \"ClosedAt\"")]
    IEnumerable<string>? Fields = null)
    : IRequest<CommandResult<Dictionary<string, object?>>>;