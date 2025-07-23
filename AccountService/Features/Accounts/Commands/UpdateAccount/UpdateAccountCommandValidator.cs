using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator(IAccountRepository accountRepository, ICurrencyService currencyService)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id счета не может быть пустым");

        RuleFor(x => x.Currency)
            .Length(3).When(x => !string.IsNullOrEmpty(x.Currency))
            .WithMessage("Код валюты должен состоять из 3 символов")
            .MustAsync(async (currency, cancellation) =>
                string.IsNullOrEmpty(currency) || await currencyService.IsSupportedCurrencyAsync(currency))
            .WithMessage(x => $"Валюта {x.Currency} не поддерживается");

        RuleFor(x => x.InterestRate)
            .GreaterThan(0).When(x => x.InterestRate.HasValue)
            .WithMessage("Процентная ставка должна быть больше 0");
    }
}