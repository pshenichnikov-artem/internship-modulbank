using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Id)
            .MustBeValid("Id транзакции");

        RuleFor(x => x.Description)
            .MustHaveMaxLength(500, "Описание");
    }
}