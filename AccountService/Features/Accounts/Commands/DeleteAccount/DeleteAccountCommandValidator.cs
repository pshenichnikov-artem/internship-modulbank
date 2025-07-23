using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .MustBeValid("Идентификатор счета");
    }
}