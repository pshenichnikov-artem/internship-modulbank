using AccountService.Common.Validators;
using AccountService.Features.Transactions.Models;
using FluentValidation;

namespace AccountService.Features.Transactions.Query.GetTransactionById;

public class GetTransactionByIdQueryValidator : AbstractValidator<GetTransactionByIdQuery>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Transaction.Id),
        nameof(Transaction.AccountId),
        nameof(Transaction.CounterpartyAccountId),
        nameof(Transaction.Amount),
        nameof(Transaction.Currency),
        nameof(Transaction.Type),
        nameof(Transaction.Description),
        nameof(Transaction.Timestamp)
    };

    public GetTransactionByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .MustBeValid("ID транзакции");

        RuleForEach(x => x.Fields)
            .Must(field => AllowedFields.Contains(field))
            .When(x => x.Fields != null && x.Fields.Any())
            .WithMessage((_, field) =>
                $"Поле '{field}' не существует. Допустимые поля: {string.Join(", ", AllowedFields)}");
    }
}