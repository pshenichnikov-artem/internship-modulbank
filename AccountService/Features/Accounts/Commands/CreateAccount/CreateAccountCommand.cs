using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public record CreateAccountCommand : IRequest<CommandResult<Guid>>
{
    [SwaggerSchema(Description = "Идентификатор владельца счета (GUID)")]
    public Guid OwnerId { get; set; }

    [SwaggerSchema(Description = "Трёхбуквенный код валюты в формате ISO 4217 (например, USD, EUR, RUB)")]
    public string Currency { get; init; } = string.Empty;

    [SwaggerSchema(Description = AccountTypeDescriptions.Description)]
    public AccountType Type { get; init; }

    [SwaggerSchema(Description =
        "Процентная ставка:\n" +
        " - Обязательна для депозитных и кредитных счетов\n" +
        " - Для расчетного (Checking) счета должна быть null")]
    public decimal? InterestRate { get; init; }
}
