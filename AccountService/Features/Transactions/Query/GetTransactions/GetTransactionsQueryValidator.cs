using AccountService.Common.Interfaces.Service;
using AccountService.Features.Transactions.Models;
using FluentValidation;

namespace AccountService.Features.Transactions.Query.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Transaction.Id),
        nameof(Transaction.AccountId),
        nameof(Transaction.CounterpartyAccountId),
        nameof(Transaction.Amount),
        nameof(Transaction.Currency),
        nameof(Transaction.Type),
        nameof(Transaction.Timestamp)
    };

    public GetTransactionsQueryValidator(ICurrencyService currencyService)
    {
        RuleFor(x => x.Filter.FromDate)
            .LessThanOrEqualTo(x => x.Filter.ToDate)
            .When(x => x.Filter is { FromDate: not null, ToDate: not null })
            .WithMessage("Дата начала должна быть меньше или равна дате окончания");

        RuleForEach(x => x.SortOrders)
            .Must(sortOrder => AllowedSortFields.Contains(sortOrder.Field))
            .WithMessage(_ =>
                $"Поле сортировки не поддерживается. Допустимые поля: {string.Join(", ", AllowedSortFields)}");

        RuleForEach(x => x.Filter.Currencies)
            .Length(3).WithMessage("Код валюты должен состоять из 3 символов")
            .MustAsync(async (currency, _) => await currencyService.IsSupportedCurrencyAsync(currency))
            .When(x => x.Filter.Currencies != null && x.Filter.Currencies.Any())
            .WithMessage(currency => $"Валюта {currency} не поддерживается");
    }
}