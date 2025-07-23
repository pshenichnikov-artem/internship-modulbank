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
    
    [SwaggerSchema(Description = "Новое значение поля (тип зависит от обновляемого поля)")]
    public object FieldValue { get; set; } = string.Empty;
}