using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using FluentValidation;

namespace AccountService.Features.Transactions;

public class TransactionService(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    ICurrencyService currencyService,
    IMapper mapper,
    ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<CommandResult<Guid>> CreateTransactionAsync(CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        await accountRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var (account, counterpartyAccount, validationError) =
                await ValidateAccountsAsync(request, cancellationToken);
            if (validationError != null)
                return CommandResult<Guid>.Failure(validationError.Value.StatusCode, validationError.Value.Message);

            if (request.OwnerId.HasValue)
            {
                if (request.Type == TransactionType.Transfer)
                {
                    if (counterpartyAccount != null && counterpartyAccount.OwnerId != request.OwnerId.Value)
                        return CommandResult<Guid>.Failure(403, "У вас нет доступа к счёту отправителя");
                }
                else
                {
                    if (account.OwnerId != request.OwnerId.Value)
                        return CommandResult<Guid>.Failure(403, "У вас нет доступа к этому счету");
                }
            }

            var transaction = mapper.Map<Transaction>(request);

            var currencyCheck = await CheckCurrencies(transaction, account, counterpartyAccount, cancellationToken);
            if (!currencyCheck.IsSuccess)
                return CommandResult<Guid>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await ApplyTransferAsync(transaction, account, counterpartyAccount, cancellationToken)
                : await ApplySingleTransactionAsync(transaction, account, cancellationToken);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync(cancellationToken);
                return CommandResult<Guid>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            await transactionRepository.CreateTransactionAsync(transaction, cancellationToken);
            await accountRepository.CommitAsync(cancellationToken);

            return CommandResult<Guid>.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при создании транзакции. " +
                "AccountId: {AccountId}, CounterpartyId: {CounterpartyId}, " +
                "OwnerId: {OwnerId}, Type: {Type}, Amount: {Amount}, Currency: {Currency}, " +
                "Request: {@Request} RequestTime: {RequestTime},",
                request.AccountId,
                request.CounterpartyAccountId,
                request.OwnerId,
                request.Type,
                request.Amount,
                request.Currency,
                request,
                DateTime.UtcNow);
            await accountRepository.RollbackAsync(cancellationToken);
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }

    public async Task<CommandResult<object>> CancelTransactionAsync(Guid transactionId, Guid ownerId,
        CancellationToken cancellationToken)
    {
        await accountRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var transaction = await transactionRepository.GetTransactionByIdAsync(transactionId, cancellationToken);
            if (transaction == null)
                return CommandResult<object>.Failure(404, $"Транзакция с ID {transactionId} не найдена");

            if (transaction.IsCanceled)
                return CommandResult<object>.Failure(400, "Транзакция уже отменена");

            var (account, counterpartyAccount, validationError) =
                await ValidateAccountsAsync(transaction.AccountId, transaction.CounterpartyAccountId,
                    cancellationToken);
            if (validationError != null)
                return CommandResult<object>.Failure(validationError.Value.StatusCode, validationError.Value.Message);

            var ownerAccount = counterpartyAccount ?? account;
            if (ownerAccount.OwnerId != ownerId)
                return CommandResult<object>.Failure(403, "У вас нет доступа к этой транзакции");

            var currencyCheck = await CheckCurrencies(transaction, account, counterpartyAccount, cancellationToken);
            if (!currencyCheck.IsSuccess)
                return CommandResult<object>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await RevertTransferAsync(transaction, account, counterpartyAccount, cancellationToken)
                : await RevertSingleTransactionAsync(transaction, account, cancellationToken);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync(cancellationToken);
                return CommandResult<object>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            transaction.IsCanceled = true;
            transaction.CanceledAt = DateTime.UtcNow;

            await transactionRepository.UpdateTransactionAsync(transaction, cancellationToken);
            await accountRepository.CommitAsync(cancellationToken);

            return CommandResult<object>.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Ошибка при отмене транзакции. TransactionId: {TransactionId}, OwnerId: {OwnerId}, " +
                "RequestTime {RequestTime}",
                transactionId,
                ownerId,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            await accountRepository.RollbackAsync(cancellationToken);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }

    // == ВАЛИДАЦИЯ ==

    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(CreateTransactionCommand request, CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetAccountByIdForUpdateAsync(request.AccountId, cancellationToken);
        if (account == null)
            return (null!, null, (404, $"Счёт {request.AccountId} не найден"));

        if (request is { Type: TransactionType.Transfer, CounterpartyAccountId: null })
            return (null!, null, (400, "Не указан счёт контрагента для перевода"));

        if (request.Type != TransactionType.Transfer && request.CounterpartyAccountId != null)
            return (null!, null, (400, "Контрагент указан, но тип транзакции не Transfer"));

        var counterparty = request.CounterpartyAccountId.HasValue
            ? await accountRepository.GetAccountByIdForUpdateAsync(request.CounterpartyAccountId.Value,
                cancellationToken)
            : null;

        if (request.CounterpartyAccountId.HasValue && counterparty == null)
            return (null!, null, (404, $"Счёт контрагента {request.CounterpartyAccountId} не найден"));

        return (account, counterparty, null);
    }

    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(Guid accountId, Guid? counterpartyId, CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetAccountByIdForUpdateAsync(accountId, cancellationToken);
        if (account == null)
            return (null!, null, (404, $"Счёт {accountId} не найден"));

        var counterparty = counterpartyId.HasValue
            ? await accountRepository.GetAccountByIdForUpdateAsync(counterpartyId.Value, cancellationToken)
            : null;

        if (counterpartyId.HasValue && counterparty == null)
            return (null!, null, (404, $"Счёт контрагента {counterpartyId} не найден"));

        return (account, counterparty, null);
    }

    private async Task<CommandResult<object>> CheckCurrencies(Transaction tx, Account a, Account? c,
        CancellationToken cancellationToken = default)
    {
        if (!await currencyService.IsSupportedCurrencyAsync(tx.Currency, cancellationToken))
            return CommandResult<object>.Failure(400, $"Валюта {tx.Currency} не поддерживается");

        if (!await currencyService.IsSupportedCurrencyAsync(a.Currency, cancellationToken))
            return CommandResult<object>.Failure(400, $"Валюта счёта {a.Currency} не поддерживается");

        if (c != null && !await currencyService.IsSupportedCurrencyAsync(c.Currency, cancellationToken))
            return CommandResult<object>.Failure(400, $"Валюта счёта контрагента {c.Currency} не поддерживается");

        return CommandResult<object>.Success();
    }

    // == ПРИМЕНЕНИЕ ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> ApplySingleTransactionAsync(Transaction tx, Account account,
        CancellationToken cancellationToken = default)
    {
        if (account.Type == AccountType.Deposit && tx.Type == TransactionType.Debit)
            return CommandResult<object>.Failure(400, "С депозита нельзя списывать средства");

        var amount = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        switch (tx.Type)
        {
            case TransactionType.Credit:
                account.Balance += amount;
                break;
            case TransactionType.Debit when !CanWithdraw(account, amount):
                return CommandResult<object>.Failure(400, "Недостаточно средств на счёте");
            case TransactionType.Debit:
                account.Balance -= amount;
                break;
            case TransactionType.Transfer:
            default:
                logger.LogError("Неизвестный тип транзакции: {TransactionType} для транзакции {TransactionId}", tx.Type,
                    tx.Id);
                return CommandResult<object>.Failure(400, "Неверный тип транзакции");
        }

        await accountRepository.UpdateAccountAsync(account, cancellationToken);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> ApplyTransferAsync(Transaction tx, Account account, Account counterparty,
        CancellationToken cancellationToken = default)
    {
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (!CanWithdraw(counterparty, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств на счёте контрагента");

        counterparty.Balance -= amountFrom;
        account.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(counterparty, cancellationToken);
        await accountRepository.UpdateAccountAsync(account, cancellationToken);

        return CommandResult<object>.Success();
    }

    // == ОТМЕНА ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> RevertSingleTransactionAsync(Transaction tx, Account account,
        CancellationToken cancellationToken = default)
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

        await accountRepository.UpdateAccountAsync(account, cancellationToken);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> RevertTransferAsync(Transaction tx, Account account, Account counterparty,
        CancellationToken cancellationToken = default)
    {
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (!CanWithdraw(account, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств для отмены транзакции");

        account.Balance -= amountFrom;
        counterparty.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(account, cancellationToken);
        await accountRepository.UpdateAccountAsync(counterparty, cancellationToken);

        return CommandResult<object>.Success();
    }

    // == ХЕЛПЕРЫ ==

    private static bool CanWithdraw(Account account, decimal amount)
    {
        return account.Type == AccountType.Credit || account.Balance >= amount;
    }

    private static bool CanHaveBalance(Account account, decimal newBalance)
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