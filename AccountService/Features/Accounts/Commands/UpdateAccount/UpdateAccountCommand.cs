using AccountService.Common.Models.Domain.Results;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommand : IRequest<CommandResult<object>>
{
    [SwaggerSchema(Description = "Идентификатор счета (заполняется автоматически из URL)")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Трёхбуквенный код валюты в формате ISO 4217 (например, USD, EUR, RUB)")]
    public string Currency { get; init; } = string.Empty;

    [SwaggerSchema(Description =
        "Процентная ставка:\n" +
        " - Обязательна для депозитных и кредитных счетов\n" +
        " - Для расчетного (Checking) счета должна быть null")]
    public decimal? InterestRate { get; init; }

    public Guid OwnerId { get; init; }
}
