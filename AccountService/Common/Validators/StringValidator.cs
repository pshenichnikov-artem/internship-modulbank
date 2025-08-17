using FluentValidation;

namespace AccountService.Common.Validators;

public static class StringValidator
{
    public static IRuleBuilderOptions<T, string> MustHaveMaxLength<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int maxLength,
        string fieldName = "Строка")
    {
        return ruleBuilder
            .Must(x => string.IsNullOrEmpty(x) || x.Length <= maxLength)
            .WithMessage($"{fieldName} не может превышать {maxLength} символов");
    }
}
