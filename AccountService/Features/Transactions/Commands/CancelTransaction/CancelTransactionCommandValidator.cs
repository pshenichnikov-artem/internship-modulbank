using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

public class CancelTransactionCommandValidator : AbstractValidator<CancelTransactionCommand>
{
    public CancelTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .MustBeValid("Идентификатор транзакции");
    }
}