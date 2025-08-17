using AccountService.Common.Interfaces.Service;

namespace AccountService.Infrastructure.Services;

public class CurrencyService(ILogger<CurrencyService> logger) : ICurrencyService
{
    private readonly Dictionary<string, decimal> _exchangeRates = new(StringComparer.OrdinalIgnoreCase)
    {
        { "RUB", 1m },
        { "USD", 75m },
        { "EUR", 90m }
    };

    private readonly HashSet<string> _supportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "RUB", "USD", "EUR"
    };

    public Task<bool> IsSupportedCurrencyAsync(string currencyCode, CancellationToken ct = default)
    {
        return Task.FromResult(_supportedCurrencies.Contains(currencyCode));
    }

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (!_supportedCurrencies.Contains(fromCurrency))
        {
            logger.LogWarning("Попытка конвертации из неподдерживаемой валюты: {FromCurrency}", fromCurrency);
            throw new ArgumentException($"Валюта {fromCurrency} не поддерживается", nameof(fromCurrency));
        }

        if (!_supportedCurrencies.Contains(toCurrency))
        {
            logger.LogWarning("Попытка конвертации в неподдерживаемую валюту: {ToCurrency}", toCurrency);
            throw new ArgumentException($"Валюта {toCurrency} не поддерживается", nameof(toCurrency));
        }

        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var amountInRub = amount * _exchangeRates[fromCurrency];
        var convertedAmount = amountInRub / _exchangeRates[toCurrency];
        return decimal.Round(convertedAmount, 2);
    }
}
