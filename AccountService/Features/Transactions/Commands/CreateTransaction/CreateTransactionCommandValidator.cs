using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator(IAccountRepository accountRepository, ICurrencyService currencyService)
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("AccountId не может быть пустым")
            .MustAsync(async (accountId, _) => await accountRepository.GetAccountByIdAsync(accountId) != null)
            .WithMessage(x => $"Счет с ID {x.AccountId} не найден");

        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Сумма транзакции не может быть пустой")
            .NotEqual(0).WithMessage("Сумма транзакции не может быть равна 0");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Валюта не может быть пустой")
            .Length(3).WithMessage("Код валюты должен состоять из 3 символов")
            .MustAsync(async (currency, _) => await currencyService.IsSupportedCurrencyAsync(currency))
            .WithMessage(x => $"Валюта {x.Currency} не поддерживается");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Недопустимый тип транзакции");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Описание не может превышать 500 символов");
    }
}