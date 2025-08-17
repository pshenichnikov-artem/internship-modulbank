using FluentValidation;

namespace AccountService.Common.Validators;

public static class IdValidator
{
    public static IRuleBuilderOptions<T, Guid> MustBeValid<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName = "Идентификатор")
    {
        return ruleBuilder
            .NotEmpty().WithMessage($"{fieldName} не может быть пустым");
    }
}
