using AccountService.Common.Validators;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
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
