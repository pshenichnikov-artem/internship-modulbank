using FluentValidation;

namespace AccountService.Common.Validators;

public static class EnumValidator
{
    public static IRuleBuilderOptions<T, TEnum> MustBeValidEnum<T, TEnum>(
        this IRuleBuilder<T, TEnum> ruleBuilder,
        string? customErrorMessage = null)
        where TEnum : struct, Enum
    {
        var enumType = typeof(TEnum);
        var validValues = Enum.GetValues(enumType)
            .Cast<TEnum>()
            .Select(e => $"{e} = {(int)(object)e}")
            .ToArray();

        var messagePrefix = customErrorMessage ?? "Недопустимое значение";
        var errorMessage = $"{messagePrefix}. Допустимые значения: {string.Join(", ", validValues)}";

        return ruleBuilder
            .IsInEnum()
            .WithMessage(errorMessage);
    }
}