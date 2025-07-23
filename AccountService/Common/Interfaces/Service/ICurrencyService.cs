namespace AccountService.Common.Interfaces.Service;

public interface ICurrencyService
{
    Task<bool> IsSupportedCurrencyAsync(string currencyCode);
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
}