using AccountService.Common.Interfaces.Repository;
using AccountService.Features.Accounts.Model;
using FluentValidation;

namespace AccountService.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public DeleteAccountCommandValidator(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Идентификатор счета не может быть пустым")
            .MustAsync(AccountExistsAsync).WithMessage("Счет с указанным идентификатором не найден");

        RuleFor(x => x.AccountId)
            .MustAsync(CanDeleteAccountAsync).WithMessage(CanDeleteAccountErrorMessage)
            .When(x => x.AccountId != Guid.Empty);
    }

    private async Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetAccountByIdAsync(accountId);
        return account != null;
    }

    private async Task<bool> CanDeleteAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetAccountByIdAsync(accountId);
        if (account == null)
            return false;

        // Проверяем баланс в зависимости от типа счета
        return account.Type switch
        {
            AccountType.Checking => account.Balance == 0,
            AccountType.Deposit => account.Balance == 0,
            AccountType.Credit => account.Balance == 0,
            _ => false
        };
    }

    private string CanDeleteAccountErrorMessage(DeleteAccountCommand command)
    {
        return "Невозможно удалить счет с ненулевым балансом. Пожалуйста, обнулите баланс перед удалением счета.";
    }
}