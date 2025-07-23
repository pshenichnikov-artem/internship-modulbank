using FluentValidation;

namespace AccountService.Common.Validators;

public static class CurrencyValidator
{
    public static IRuleBuilderOptions<T, string> MustBeValidCurrencyFormat<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Валюта не может быть пустой")
            .Length(3).WithMessage("Код валюты должен состоять из 3 символов");
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidCurrencyFormatIfSpecified<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => string.IsNullOrEmpty(x) || x.Length == 3)
            .WithMessage("Код валюты должен состоять из 3 символов");
    }
}