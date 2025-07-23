using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор счета (заполняется автоматически из URL)")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Трехбуквенный код валюты (USD, EUR, RUB)")]
    public string Currency { get; init; } = string.Empty;

    [SwaggerSchema(Description = "Процентная ставка (депозитов и кредитов)")]
    public decimal? InterestRate { get; init; }
}