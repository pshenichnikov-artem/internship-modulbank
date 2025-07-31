using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .MustBeValid("Идентификатор счета");
    }
}