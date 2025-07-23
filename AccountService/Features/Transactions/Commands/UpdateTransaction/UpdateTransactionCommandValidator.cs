using AccountService.Common.Interfaces.Repository;
using FluentValidation;

namespace AccountService.Features.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator(ITransactionRepository transactionRepository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id транзакции не может быть пустым")
            .MustAsync(async (id, _) =>
            {
                var transaction = await transactionRepository.GetTransactionByIdAsync(id);
                return transaction is { IsCanceled: false };
            })
            .WithMessage(x =>
                $"Транзакция с ID {x.Id} не найдена или уже отменена и не может быть изменена");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Описание не может превышать 500 символов");
    }
}