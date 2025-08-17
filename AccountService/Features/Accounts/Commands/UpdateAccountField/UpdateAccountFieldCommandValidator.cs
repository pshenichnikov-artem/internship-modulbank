using AccountService.Common.Validators;
using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.UpdateAccountField;

// ReSharper disable once UnusedMember.Global
// Класс валидатора используется через механизм автоматической регистрации
public class UpdateAccountFieldCommandValidator : AbstractValidator<UpdateAccountFieldCommand>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Account.Currency),
        nameof(Account.InterestRate)
    };

    public UpdateAccountFieldCommandValidator()
    {
        RuleFor(x => x.Id)
            .MustBeValid("Id счета");

        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("Имя поля не может быть пустым")
            .Must(field => AllowedFields.Contains(field))
            .WithMessage(x =>
                $"Поле '{x.FieldName}' не может быть обновлено. Допустимые поля: {string.Join(", ", AllowedFields)}");

        When(x => x.FieldName.Equals("Currency", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.FieldValue as string)!
                .NotNull().WithMessage("Значение должно быть строкой")!
                .MustBeValidCurrencyFormat();
        });

        RuleFor(x => x.FieldValue)
            .Must(value =>
            {
                try
                {
                    var number = Convert.ToDecimal(value);
                    return number is >= 1 and <= 50;
                }
                catch
                {
                    return false;
                }
            })
            .When(x => x.FieldName.Equals("InterestRate", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Процентная ставка должна быть числом от 1 до 50%");
    }
}
