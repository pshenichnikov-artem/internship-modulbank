using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Common.Validators;

public static class InterestRateValidator
{
    public static IRuleBuilderOptions<T, decimal?> MustBeInValidRange<T>(
        this IRuleBuilder<T, decimal?> ruleBuilder,
        decimal minValue = 0,
        decimal maxValue = 50)
    {
        return ruleBuilder
            .Must(rate => !rate.HasValue || (rate > minValue && rate <= maxValue))
            .WithMessage(
                $"Процентная ставка должна быть больше {minValue} и не больше {maxValue} для депозитов и кредитов");
    }

    public static IRuleBuilderOptions<T, decimal?> MustNotBeSetForAccountType<T>(
        this IRuleBuilder<T, decimal?> ruleBuilder,
        Func<T, AccountType> typeSelector)
    {
        return ruleBuilder
            .Must((obj, rate) => typeSelector(obj) != AccountType.Checking || rate is null)
            .WithMessage("Для типа Checking нельзя указывать процентную ставку");
    }
}
