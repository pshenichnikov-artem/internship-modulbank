namespace AccountService.Common.Interfaces.Service;

public interface ICurrencyService
{
    Task<bool> IsSupportedCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
}