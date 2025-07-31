using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.CancelTransaction;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class CancelTransactionCommandValidator : AbstractValidator<CancelTransactionCommand>
{
    public CancelTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .MustBeValid("Идентификатор транзакции");
    }
}