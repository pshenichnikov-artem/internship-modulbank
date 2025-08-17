using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .MustBeValid("Id счета");

        RuleFor(x => x.Currency)
            .MustBeValidCurrencyFormat();

        RuleFor(x => x.InterestRate)
            .Must(rate => rate is >= 1 and <= 50)
            .When(x => x.InterestRate.HasValue)
            .WithMessage("Процентная ставка должна быть в диапазоне от 1 до 50%");
    }
}
