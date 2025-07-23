using AccountService.Common.Interfaces;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using FluentValidation;

namespace AccountService.Features.Transactions;

public class TransactionProcessor(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    ICurrencyService currencyService,
    IMapper mapper)
    : ITransactionProcessor
{
    public async Task<CommandResult<Guid>> CreateTransactionAsync(CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        await accountRepository.BeginTransactionAsync();

        try
        {
            var (account, counterpartyAccount, validationError) = await ValidateAccountsAsync(request);
            if (validationError != null)
                return CommandResult<Guid>.Failure(validationError.Value.StatusCode, validationError.Value.Message);

            var transaction = mapper.Map<Transaction>(request);

            var currencyCheck = CheckCurrencies(transaction, account, counterpartyAccount);
            if (!currencyCheck.IsSuccess)
                return CommandResult<Guid>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await ApplyTransferAsync(transaction, account, counterpartyAccount)
                : await ApplySingleTransactionAsync(transaction, account);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync();
                return CommandResult<Guid>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            await transactionRepository.CreateTransactionAsync(transaction);
            await accountRepository.CommitAsync();

            return CommandResult<Guid>.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync();
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }

    public async Task<CommandResult<object>> CancelTransactionAsync(Guid transactionId,
        CancellationToken cancellationToken)
    {
        await accountRepository.BeginTransactionAsync();

        try
        {
            var transaction = await transactionRepository.GetTransactionByIdAsync(transactionId);
            if (transaction == null)
                return CommandResult<object>.Failure(404, $"Транзакция с ID {transactionId} не найдена");

            if (transaction.IsCanceled)
                return CommandResult<object>.Failure(400, "Транзакция уже отменена");

            var (account, counterpartyAccount, validationError) =
                await ValidateAccountsAsync(transaction.AccountId, transaction.CounterpartyAccountId);
            if (validationError != null)
                return CommandResult<object>.Failure(validationError.Value.StatusCode, validationError.Value.Message);

            var currencyCheck = CheckCurrencies(transaction, account, counterpartyAccount);
            if (!currencyCheck.IsSuccess)
                return CommandResult<object>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await RevertTransferAsync(transaction, account, counterpartyAccount)
                : await RevertSingleTransactionAsync(transaction, account);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync();
                return CommandResult<object>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            transaction.IsCanceled = true;
            transaction.CanceledAt = DateTime.UtcNow;

            await transactionRepository.UpdateTransactionAsync(transaction);
            await accountRepository.CommitAsync();

            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            await accountRepository.RollbackAsync();
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }

    // == ВАЛИДАЦИЯ ==

    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(CreateTransactionCommand request)
    {
        var account = await accountRepository.GetAccountByIdForUpdateAsync(request.AccountId);
        if (account == null)
            return (null!, null, (404, $"Счёт {request.AccountId} не найден"));

        if (request.Type == TransactionType.Transfer && request.CounterpartyAccountId == null)
            return (null!, null, (400, "Не указан счёт контрагента для перевода"));

        if (request.Type != TransactionType.Transfer && request.CounterpartyAccountId != null)
            return (null!, null, (400, "Контрагент указан, но тип транзакции не Transfer"));

        var counterparty = request.CounterpartyAccountId.HasValue
            ? await accountRepository.GetAccountByIdForUpdateAsync(request.CounterpartyAccountId.Value)
            : null;

        if (request.CounterpartyAccountId.HasValue && counterparty == null)
            return (null!, null, (404, $"Счёт контрагента {request.CounterpartyAccountId} не найден"));

        return (account, counterparty, null);
    }

    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(Guid accountId, Guid? counterpartyId)
    {
        var account = await accountRepository.GetAccountByIdForUpdateAsync(accountId);
        if (account == null)
            return (null!, null, (404, $"Счёт {accountId} не найден"));

        var counterparty = counterpartyId.HasValue
            ? await accountRepository.GetAccountByIdForUpdateAsync(counterpartyId.Value)
            : null;

        if (counterpartyId.HasValue && counterparty == null)
            return (null!, null, (404, $"Счёт контрагента {counterpartyId} не найден"));

        return (account, counterparty, null);
    }

    private CommandResult<object> CheckCurrencies(Transaction tx, Account a, Account? c)
    {
        if (!currencyService.IsSupportedCurrencyAsync(tx.Currency).Result)
            return CommandResult<object>.Failure(400, $"Валюта {tx.Currency} не поддерживается");

        if (!currencyService.IsSupportedCurrencyAsync(a.Currency).Result)
            return CommandResult<object>.Failure(400, $"Валюта счёта {a.Currency} не поддерживается");

        if (c != null && !currencyService.IsSupportedCurrencyAsync(c.Currency).Result)
            return CommandResult<object>.Failure(400, $"Валюта счёта контрагента {c.Currency} не поддерживается");

        return CommandResult<object>.Success();
    }

    // == ПРИМЕНЕНИЕ ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> ApplySingleTransactionAsync(Transaction tx, Account account)
    {
        if (account.Type == AccountType.Deposit && tx.Type == TransactionType.Debit)
            return CommandResult<object>.Failure(400, "С депозита нельзя списывать средства");

        var amount = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (tx.Type == TransactionType.Credit)
        {
            account.Balance += amount;
        }
        else if (tx.Type == TransactionType.Debit)
        {
            if (!CanWithdraw(account, amount))
                return CommandResult<object>.Failure(400, "Недостаточно средств на счёте");

            account.Balance -= amount;
        }
        else
        {
            return CommandResult<object>.Failure(400, "Неверный тип транзакции");
        }

        await accountRepository.UpdateAccountAsync(account);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> ApplyTransferAsync(Transaction tx, Account account, Account counterparty)
    {
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (!CanWithdraw(counterparty, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств на счёте контрагента");

        counterparty.Balance -= amountFrom;
        account.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(counterparty);
        await accountRepository.UpdateAccountAsync(account);

        return CommandResult<object>.Success();
    }

    // == ОТМЕНА ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> RevertSingleTransactionAsync(Transaction tx, Account account)
    {
        var amount = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        var newBalance = tx.Type switch
        {
            TransactionType.Debit => account.Balance + amount,
            TransactionType.Credit => account.Balance - amount,
            _ => throw new ValidationException("Неверный тип транзакции")
        };

        if (!CanHaveBalance(account, newBalance))
            return CommandResult<object>.Failure(400, "Недостаточно средств для отмены транзакции");

        account.Balance = newBalance;

        await accountRepository.UpdateAccountAsync(account);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> RevertTransferAsync(Transaction tx, Account account, Account counterparty)
    {
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (!CanWithdraw(account, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств для отмены транзакции");

        account.Balance -= amountFrom;
        counterparty.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(account);
        await accountRepository.UpdateAccountAsync(counterparty);

        return CommandResult<object>.Success();
    }

    // == ХЕЛПЕРЫ ==

    private bool CanWithdraw(Account account, decimal amount)
    {
        return account.Type == AccountType.Credit || account.Balance >= amount;
    }

    private bool CanHaveBalance(Account account, decimal newBalance)
    {
        return account.Type == AccountType.Credit || newBalance >= 0;
    }

    private decimal ConvertAmount(decimal amount, string fromCurrency, string toCurrency)
    {
        return string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)
            ? amount
            : currencyService.Convert(amount, fromCurrency, toCurrency);
    }
}