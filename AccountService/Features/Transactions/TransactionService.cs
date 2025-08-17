using AccountService.Common.Extensions;
using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models.Domain.Results;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using FluentValidation;
using Messaging.Events;
using Messaging.Interfaces;

namespace AccountService.Features.Transactions;

public class TransactionService(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    ICurrencyService currencyService,
    IMapper mapper,
    IOutboxService outboxService,
    ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<CommandResult<Guid>> CreateTransactionAsync(CreateTransactionCommand request,
        CancellationToken ct)
    {
        await accountRepository.BeginTransactionAsync(ct);

        try
        {
            var (account, counterpartyAccount, validationError) =
                await ValidateAccountsAsync(request, ct);
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
            transaction.Type = request.Type;
            var currencyCheck = await CheckCurrencies(transaction, account, counterpartyAccount, ct);
            if (!currencyCheck.IsSuccess)
                return CommandResult<Guid>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await ApplyTransferAsync(transaction, account, counterpartyAccount, ct)
                : await ApplySingleTransactionAsync(transaction, account, ct);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<Guid>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            await transactionRepository.CreateTransactionAsync(transaction, ct);

            await PublishTransactionEventAsync(transaction, account, counterpartyAccount, ct);

            await accountRepository.CommitAsync(ct);

            return CommandResult<Guid>.Success(transaction.Id);
        }
        catch (Exception pgEx) when (pgEx.IsConcurrencyException())
        {
            logger.LogWarning("Конфликт параллелизма при создании транзакции. AccountId: {AccountId}, CounterpartyId: {CounterpartyId}, OwnerId: {OwnerId}, Type: {Type}, Amount: {Amount}, Currency: {Currency}, Time: {Time}, Error: {Error}", 
                request.AccountId, request.CounterpartyAccountId, request.OwnerId, request.Type, request.Amount, request.Currency, DateTime.UtcNow.ToString("HH:mm:ss"), pgEx.Message);
            await accountRepository.RollbackAsync(ct);
            return CommandResult<Guid>.Failure(409, "Конфликт обновления данных. Попробуйте снова.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Ошибка при создании транзакции. " +
                "AccountId: {AccountId}, CounterpartyId: {CounterpartyId}, " +
                "OwnerId: {OwnerId}, Type: {Type}, Amount: {Amount}, Currency: {Currency}, " +
                "Request: {@Request} RequestTime: {RequestTime}, Error: {Error}",
                request.AccountId,
                request.CounterpartyAccountId,
                request.OwnerId,
                request.Type,
                request.Amount,
                request.Currency,
                request,
                DateTime.UtcNow,
                ex.Message);
            await accountRepository.RollbackAsync(ct);
            return CommandResult<Guid>.Failure(500, ex.Message);
        }
    }

    public async Task<CommandResult<object>> CancelTransactionAsync(Guid transactionId, Guid ownerId,
        CancellationToken ct)
    {
        await accountRepository.BeginTransactionAsync(ct);

        try
        {
            var transaction = await transactionRepository.GetTransactionByIdAsync(transactionId, ct);
            if (transaction == null)
                return CommandResult<object>.Failure(404, $"Транзакция с ID {transactionId} не найдена");

            if (transaction.IsCanceled)
                return CommandResult<object>.Failure(400, "Транзакция уже отменена");

            var (account, counterpartyAccount, validationError) =
                await ValidateAccountsAsync(transaction.AccountId, transaction.CounterpartyAccountId,
                    ct);
            if (validationError != null)
                return CommandResult<object>.Failure(validationError.Value.StatusCode, validationError.Value.Message);

            var ownerAccount = counterpartyAccount ?? account;
            if (ownerAccount.OwnerId != ownerId)
                return CommandResult<object>.Failure(403, "У вас нет доступа к этой транзакции");

            var currencyCheck = await CheckCurrencies(transaction, account, counterpartyAccount, ct);
            if (!currencyCheck.IsSuccess)
                return CommandResult<object>.Failure(currencyCheck.CommandError!.StatusCode,
                    currencyCheck.CommandError.Message);

            var result = counterpartyAccount != null
                ? await RevertTransferAsync(transaction, account, counterpartyAccount, ct)
                : await RevertSingleTransactionAsync(transaction, account, ct);

            if (!result.IsSuccess)
            {
                await accountRepository.RollbackAsync(ct);
                return CommandResult<object>.Failure(result.CommandError!.StatusCode, result.CommandError.Message);
            }

            transaction.IsCanceled = true;
            transaction.CanceledAt = DateTime.UtcNow;

            await transactionRepository.UpdateTransactionAsync(transaction, ct);

            var transactionCanceled = new TransactionCanceled(transaction.Id, transaction.AccountId,
                transaction.CounterpartyAccountId, transaction.Amount, transaction.Currency,
                transaction.Type.ToString());
            await outboxService.AddAsync(transactionCanceled, ct);

            await accountRepository.CommitAsync(ct);

            return CommandResult<object>.Success();
        }
        catch (Exception pgEx) when (pgEx.IsConcurrencyException())
        {
            logger.LogWarning("Конфликт параллелизма при отмене транзакции. TransactionId: {TransactionId}, OwnerId: {OwnerId}, Time: {Time}, Error: {Error}", 
                transactionId, ownerId, DateTime.UtcNow.ToString("HH:mm:ss"), pgEx.Message);
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(409, "Конфликт обновления данных. Попробуйте снова.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Ошибка при отмене транзакции. TransactionId: {TransactionId}, OwnerId: {OwnerId}, " +
                "RequestTime {RequestTime}, Error: {Error}",
                transactionId,
                ownerId,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ex.Message);
            await accountRepository.RollbackAsync(ct);
            return CommandResult<object>.Failure(500, ex.Message);
        }
    }

    // == ВАЛИДАЦИЯ ==

    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(CreateTransactionCommand request, CancellationToken ct = default)
    {
        if (request is { Type: TransactionType.Transfer, CounterpartyAccountId: null })
            return (null!, null, (400, "Не указан счёт контрагента для перевода"));

        if (request.Type != TransactionType.Transfer && request.CounterpartyAccountId != null)
            return (null!, null, (400, "Контрагент указан, но тип транзакции не Transfer"));

        if (request.Type == TransactionType.Transfer && request.AccountId == request.CounterpartyAccountId)
            return (null!, null, (400, "Нельзя переводить средства на тот же счет"));

        var idsToLock = new List<Guid> { request.AccountId };
        if (request.CounterpartyAccountId.HasValue)
            idsToLock.Add(request.CounterpartyAccountId.Value);

        idsToLock.Sort();

        var lockedAccounts = new Dictionary<Guid, Account>();

        foreach (var id in idsToLock)
        {
            var acc = await accountRepository.GetAccountByIdForUpdateAsync(id, ct);
            if (acc == null)
                return (null!, null, (404, $"Счёт {id} не найден"));

            lockedAccounts[id] = acc;
        }

        var account = lockedAccounts[request.AccountId];
        Account? counterparty = null;
        if (request.CounterpartyAccountId.HasValue)
            counterparty = lockedAccounts[request.CounterpartyAccountId.Value];

        return (account, counterparty, null);
    }


    private async Task<(Account account, Account? counterparty, (int StatusCode, string Message)? Error)>
        ValidateAccountsAsync(Guid accountId, Guid? counterpartyId, CancellationToken ct = default)
    {
        var idsToLock = new List<Guid> { accountId };
        if (counterpartyId.HasValue)
            idsToLock.Add(counterpartyId.Value);

        idsToLock.Sort();

        var lockedAccounts = new Dictionary<Guid, Account>();

        foreach (var id in idsToLock)
        {
            var acc = await accountRepository.GetAccountByIdForUpdateAsync(id, ct);
            if (acc == null)
                return (null!, null, (404, $"Счёт {id} не найден"));

            lockedAccounts[id] = acc;
        }

        var account = lockedAccounts[accountId];
        Account? counterparty = null;
        if (counterpartyId.HasValue)
            counterparty = lockedAccounts[counterpartyId.Value];

        return (account, counterparty, null);
    }

    private async Task<CommandResult<object>> CheckCurrencies(Transaction tx, Account a, Account? c,
        CancellationToken ct = default)
    {
        if (!await currencyService.IsSupportedCurrencyAsync(tx.Currency, ct))
            return CommandResult<object>.Failure(400, $"Валюта {tx.Currency} не поддерживается");

        if (!await currencyService.IsSupportedCurrencyAsync(a.Currency, ct))
            return CommandResult<object>.Failure(400, $"Валюта счёта {a.Currency} не поддерживается");

        if (c != null && !await currencyService.IsSupportedCurrencyAsync(c.Currency, ct))
            return CommandResult<object>.Failure(400, $"Валюта счёта контрагента {c.Currency} не поддерживается");

        return CommandResult<object>.Success();
    }

    // == ПРИМЕНЕНИЕ ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> ApplySingleTransactionAsync(Transaction tx, Account account,
        CancellationToken ct = default)
    {
        if (account.Type == AccountType.Deposit && tx.Type == TransactionType.Debit)
            return CommandResult<object>.Failure(400, "С депозита нельзя списывать средства");

        if (account.IsFrozen && tx.Type == TransactionType.Debit)
            return CommandResult<object>.Failure(409, "Счет заморожен, списание запрещено");

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

        await accountRepository.UpdateAccountAsync(account, ct);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> ApplyTransferAsync(Transaction tx, Account account, Account counterparty,
        CancellationToken ct = default)
    {
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (counterparty.IsFrozen)
            return CommandResult<object>.Failure(409, "Счет отправителя заморожен, перевод запрещен");

        if (!CanWithdraw(counterparty, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств на счёте контрагента");

        counterparty.Balance -= amountFrom;
        account.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(counterparty, ct);
        await accountRepository.UpdateAccountAsync(account, ct);

        return CommandResult<object>.Success();
    }

    // == ОТМЕНА ТРАНЗАКЦИЙ ==

    private async Task<CommandResult<object>> RevertSingleTransactionAsync(Transaction tx, Account account,
        CancellationToken ct = default)
    {
        var amount = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault Требует указать при TransactionType.Transfer, но в этом случае не нужно
        var newBalance = tx.Type switch
        {
            TransactionType.Debit => account.Balance + amount,
            TransactionType.Credit => account.Balance - amount,
            _ => throw new ValidationException("Неверный тип транзакции")
        };

        if (!CanHaveBalance(account, newBalance))
            return CommandResult<object>.Failure(400, "Недостаточно средств для отмены транзакции");

        account.Balance = newBalance;

        await accountRepository.UpdateAccountAsync(account, ct);
        return CommandResult<object>.Success();
    }

    private async Task<CommandResult<object>> RevertTransferAsync(Transaction tx, Account account, Account counterparty,
        CancellationToken ct = default)
    {
        var amountTo = ConvertAmount(tx.Amount, tx.Currency, counterparty.Currency);
        var amountFrom = ConvertAmount(tx.Amount, tx.Currency, account.Currency);

        if (!CanWithdraw(account, amountFrom))
            return CommandResult<object>.Failure(400, "Недостаточно средств для отмены транзакции");

        account.Balance -= amountFrom;
        counterparty.Balance += amountTo;

        await accountRepository.UpdateAccountAsync(account, ct);
        await accountRepository.UpdateAccountAsync(counterparty, ct);

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

    private async Task PublishTransactionEventAsync(Transaction transaction, Account account,
        Account? counterpartyAccount, CancellationToken ct)
    {
        switch (transaction.Type)
        {
            case TransactionType.Credit:
                var credited = new MoneyCredited(account.Id, transaction.Amount,
                    transaction.Currency, transaction.Id);
                await outboxService.AddAsync(credited, ct);
                break;

            case TransactionType.Debit:
                var debited = new MoneyDebited(account.Id, transaction.Amount,
                    transaction.Currency, transaction.Id);
                await outboxService.AddAsync(debited, ct);
                break;

            case TransactionType.Transfer when counterpartyAccount != null:
                var transfer = new MoneyTransfer(counterpartyAccount.Id, account.Id,
                    transaction.Amount, transaction.Currency, transaction.Id);
                await outboxService.AddAsync(transfer, ct);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}