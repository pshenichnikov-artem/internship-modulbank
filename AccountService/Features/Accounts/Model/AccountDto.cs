using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts.Model;

public class AccountDto
{
    [SwaggerSchema(Description = "Уникальный идентификатор счета")]
    public Guid Id { get; set; }
    
    [SwaggerSchema(Description = "Трехбуквенный код валюты (USD, EUR, RUB)")]
    public string Currency { get; set; } = string.Empty;
    
    [SwaggerSchema(Description = "Текущий баланс счета")]
    public decimal Balance { get; set; }
    
    [SwaggerSchema(Description = "Тип счета (Credit = 0, Deposit = 1, Checking = 2)")]
    public AccountType Type { get; set; }
    
    [SwaggerSchema(Description = "Процентная ставка (обязательна для депозитов и кредитов)")]
    public decimal? InterestRate { get; set; }
    
    [SwaggerSchema(Description = "Дата открытия счета")]
    public DateTime OpenedAt { get; set; }
    
    [SwaggerSchema(Description = "Дата закрытия счета (нулл для активных счетов)")]
    public DateTime? ClosedAt { get; set; }
}