using FluentValidation;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

public class CancelTransactionCommandValidator : AbstractValidator<CancelTransactionCommand>
{
    public CancelTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("Идентификатор транзакции не может быть пустым");
    }
}