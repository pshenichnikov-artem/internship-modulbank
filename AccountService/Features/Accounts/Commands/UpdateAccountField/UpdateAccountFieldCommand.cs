using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.UpdateAccountField;

public class UpdateAccountFieldCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор счета (заполняется автоматически из URL)")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Имя обновляемого поля (Currency, InterestRate, ClosedAt)")]
    public string FieldName { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Новое значение поля. Тип зависит от обновляемого поля:\n" +
                                 " - Для Currency — строка (три символа ISO 4217)\n" +
                                 " - Для InterestRate — десятичное число или null\n" +
                                 " - Для ClosedAt — дата и время или null")]
    public object FieldValue { get; set; } = string.Empty;

    public Guid OwnerId { get; init; }
}
