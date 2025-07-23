using AccountService.Common.Validators;
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

    public GetAccountsQueryValidator()
    {
        RuleFor(x => x.Pagination.Page)
            .GreaterThan(0).WithMessage("Номер страницы должен быть больше 0");

        RuleFor(x => x.Pagination.PageSize)
            .GreaterThan(0).WithMessage("Размер страницы должен быть больше 0");

        RuleForEach(x => x.Filters.Currencies)
            .MustBeValidCurrencyFormatIfSpecified();

        RuleForEach(x => x.SortOrders)
            .Must(sortOrder => AllowedSortFields.Contains(sortOrder.Field))
            .WithMessage((_, field) =>
                $"Поле {field} не поддерживается. Допустимые поля: {string.Join(", ", AllowedSortFields)}");

        RuleForEach(x => x.SortOrders)
            .Must(sortOrder =>
                sortOrder.Direction == 0 || (int)sortOrder.Direction == 1)
            .WithMessage(_ => "Направление сортировки должно быть '0'(asc) или '1'(desc)");
    }
}