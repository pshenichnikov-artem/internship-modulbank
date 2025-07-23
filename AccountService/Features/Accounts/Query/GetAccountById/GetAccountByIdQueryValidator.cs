using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Query.GetAccountById;

public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Account.Id),
        nameof(Account.OwnerId),
        nameof(Account.Type),
        nameof(Account.Currency),
        nameof(Account.Balance),
        nameof(Account.InterestRate),
        nameof(Account.OpenedAt),
        nameof(Account.ClosedAt),
        nameof(Account.Transactions)
    };

    public GetAccountByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID счета не может быть пустым");

        RuleForEach(x => x.Fields)
            .Must(field => AllowedFields.Contains(field))
            .When(x => x.Fields != null && x.Fields.Any())
            .WithMessage(field =>
                $"Поле '{field}' не существует. Допустимые поля: {string.Join(", ", AllowedFields)}");
    }
}