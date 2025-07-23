using AccountService.Common.Interfaces.Service;
using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator(IClientService clientService, ICurrencyService currencyService)
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId не может быть пустым")
            .MustAsync(async (ownerId, cancellation) => await clientService.VerifyClientExistsAsync(ownerId))
            .WithMessage(x => $"Клиент с ID {x.OwnerId} не найден");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Валюта не может быть пустой")
            .Length(3).WithMessage("Код валюты должен состоять из 3 символов")
            .MustAsync(async (currency, cancellation) => await currencyService.IsSupportedCurrencyAsync(currency))
            .WithMessage(x => $"Валюта {x.Currency} не поддерживается");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Недопустимый тип счета");

        RuleFor(x => x.InterestRate)
            .GreaterThan(0).When(x =>
                x.InterestRate.HasValue && x.Type is AccountType.Deposit or AccountType.Credit)
            .WithMessage("Процентная ставка должна быть больше 0 для депозитов и кредитов");
    }
}