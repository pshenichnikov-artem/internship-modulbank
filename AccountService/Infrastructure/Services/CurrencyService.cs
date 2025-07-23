using AccountService.Common.Interfaces.Service;

namespace AccountService.Infrastructure.Services;

public class CurrencyService : ICurrencyService
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

    public Task<bool> IsSupportedCurrencyAsync(string currencyCode)
    {
        return Task.FromResult(_supportedCurrencies.Contains(currencyCode));
    }

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (!_supportedCurrencies.Contains(fromCurrency))
            throw new ArgumentException($"Валюта {fromCurrency} не поддерживается", nameof(fromCurrency));
        if (!_supportedCurrencies.Contains(toCurrency))
            throw new ArgumentException($"Валюта {toCurrency} не поддерживается", nameof(toCurrency));

        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var amountInRub = amount * _exchangeRates[fromCurrency];

        var convertedAmount = amountInRub / _exchangeRates[toCurrency];

        return decimal.Round(convertedAmount, 2);
    }
}