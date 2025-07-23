using AccountService.Common.Interfaces.Service;
using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Query.GetAccounts;

public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Account.Id),
        nameof(Account.OwnerId),
        nameof(Account.Currency),
        nameof(Account.Balance),
        nameof(Account.Type),
        nameof(Account.InterestRate),
        nameof(Account.OpenedAt),
        nameof(Account.ClosedAt)
    };

    public GetAccountsQueryValidator(ICurrencyService currencyService)
    {
        RuleFor(x => x.PaginationDto.Page)
            .GreaterThan(0).WithMessage("Номер страницы должен быть больше 0");

        RuleFor(x => x.PaginationDto.PageSize)
            .GreaterThan(0).WithMessage("Размер страницы должен быть больше 0");

        RuleForEach(x => x.Filters.Currencies)
            .Length(3).When(x => x != null)
            .WithMessage("Код валюты должен состоять из 3 символов")
            .MustAsync(async (currency, _) => await currencyService.IsSupportedCurrencyAsync(currency))
            .When(x => x.Filters.Currencies != null && x.Filters.Currencies.Any())
            .WithMessage(currency => $"Валюта {currency} не поддерживается");

        RuleForEach(x => x.SortOrders)
            .Must(sortOrder => AllowedSortFields.Contains(sortOrder.Field))
            .WithMessage(_ =>
                $"Поле сортировки не поддерживается. Допустимые поля: {string.Join(", ", AllowedSortFields)}");
    }
}