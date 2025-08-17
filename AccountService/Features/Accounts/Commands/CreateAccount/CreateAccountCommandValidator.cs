using AccountService.Common.Validators;
using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.OwnerId)
            .MustBeValid("OwnerId");

        RuleFor(x => x.Currency)
            .MustBeValidCurrencyFormat();

        RuleFor(x => x.Type)
            .MustBeValidEnum();

        RuleFor(x => x.InterestRate)
            .NotNull()
            .MustBeInValidRange()
            .When(x => x.Type is AccountType.Deposit or AccountType.Credit);

        RuleFor(x => x.InterestRate)
            .MustNotBeSetForAccountType(x => x.Type);
    }
}
