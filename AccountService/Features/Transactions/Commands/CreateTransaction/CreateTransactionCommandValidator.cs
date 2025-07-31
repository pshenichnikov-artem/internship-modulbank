using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.CreateTransaction;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .MustBeValid("AccountId");

        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Сумма транзакции не может быть пустой")
            .NotEqual(0).WithMessage("Сумма транзакции не может быть равна 0");

        RuleFor(x => x.Currency)
            .MustBeValidCurrencyFormat();

        RuleFor(x => x.Type)
            .MustBeValidEnum();

        RuleFor(x => x.Description)
            .MustHaveMaxLength(500, "Описание");
    }
}