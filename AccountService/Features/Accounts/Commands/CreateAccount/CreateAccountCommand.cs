using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public record CreateAccountCommand : IRequest<CommandResult<Guid>>
{
    [SwaggerSchema(Description = "Идентификатор владельца счета")]
    public Guid OwnerId { get; init; }

    [SwaggerSchema(Description = "Трехбуквенный код валюты (USD, EUR, RUB)")]
    public string Currency { get; init; } = string.Empty;

    [SwaggerSchema(Description = "Тип счета (Credit = 0, Deposit = 1, Checking = 2)")]
    public AccountType Type { get; init; }

    [SwaggerSchema(Description = "Процентная ставка (обязательна для депозитов и кредитов)")]
    public decimal? InterestRate { get; init; }
}