using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.UpdateAccountField;

public class UpdateAccountFieldValidator : AbstractValidator<UpdateAccountFieldCommand>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Currency",
        "InterestRate"
    };

    public UpdateAccountFieldValidator(IAccountRepository accountRepository, ICurrencyService currencyService)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id счета не может быть пустым")
            .MustAsync(async (id, cancellation) => await accountRepository.GetAccountByIdAsync(id) != null)
            .WithMessage(x => $"Счет с ID {x.Id} не найден");

        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("Имя поля не может быть пустым")
            .Must(field => AllowedFields.Contains(field))
            .WithMessage(x =>
                $"Поле '{x.FieldName}' не может быть обновлено. Допустимые поля: {string.Join(", ", AllowedFields)}");

        When(x => x.FieldName.Equals("Currency", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.FieldValue)
                .NotNull().WithMessage("Значение валюты не может быть пустым")
                .Must(value => { return value is string { Length: 3 }; })
                .WithMessage("Код валюты должен состоять из 3 символов")
                .MustAsync(async (value, _) =>
                    value is string stringValue && await currencyService.IsSupportedCurrencyAsync(stringValue))
                .WithMessage(x => $"Валюта {x.FieldValue} не поддерживается");
        });

        When(x => x.FieldName.Equals("InterestRate", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.FieldValue)
                .NotNull().WithMessage("Значение процентной ставки не может быть пустым")
                .Must(value => { return value is decimal or double or int; })
                .WithMessage("Процентная ставка должна быть числом")
                .Must(value => Convert.ToDecimal(value) > 0 && Convert.ToDecimal(value) <= 50)
                .WithMessage("Процентная ставка должна быть больше 0 и меньше 50");
        });
    }
}