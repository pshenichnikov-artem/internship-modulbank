using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Unit;

public class TransactionServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ICurrencyService> _currencyService = new();
    private readonly Mock<ILogger<TransactionService>> _logger = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TransactionService _service;
    private readonly Mock<ITransactionRepository> _transactionRepo = new();

    public TransactionServiceTests()
    {
        _service = new TransactionService(_transactionRepo.Object, _accountRepo.Object,
            _currencyService.Object, _mapper.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateTransaction_ValidTransfer_Success()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            CounterpartyAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "RUB",
            Type = TransactionType.Transfer,
            OwnerId = Guid.NewGuid()
        };

        var account = new Account
        {
            Id = command.AccountId, Balance = 200m, Currency = "RUB", Type = AccountType.Checking,
            OwnerId = command.OwnerId.Value
        };
        var counterparty = new Account
        {
            Id = command.CounterpartyAccountId.Value, Balance = 100m, Currency = "RUB", Type = AccountType.Checking,
            OwnerId = command.OwnerId.Value
        };
        var transaction = new Transaction { Id = Guid.NewGuid() };

        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == account.Id ? account : counterparty);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _currencyService.Setup(x => x.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<decimal, string, string>((amount, _, _) => amount);
        _mapper.Setup(x => x.Map<Transaction>(command))
            .Returns(new Transaction { Id = transaction.Id, Type = command.Type });

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(transaction.Id, result.Data);
        _accountRepo.Verify(x => x.UpdateAccountAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _accountRepo.Verify(x => x.UpdateAccountAsync(counterparty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_TransferWithoutCounterparty_Failure()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Transfer
        };

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_NonTransferWithCounterparty_Failure()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            CounterpartyAccountId = Guid.NewGuid(),
            Type = TransactionType.Credit
        };

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_AccountNotFound_Failure()
    {
        var command = new CreateTransactionCommand { AccountId = Guid.NewGuid() };
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_UnauthorizedAccess_Failure()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Credit,
            OwnerId = Guid.NewGuid()
        };
        var account = new Account { Id = command.AccountId, OwnerId = Guid.NewGuid() };
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_UnsupportedCurrency_Failure()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Currency = "USD",
            Type = TransactionType.Credit
        };
        var account = new Account { Id = command.AccountId, Currency = "RUB" };
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapper.Setup(x => x.Map<Transaction>(command)).Returns(new Transaction { Type = command.Type });

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_DepositDebit_Failure()
    {
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Debit,
            Amount = 100m,
            Currency = "RUB"
        };
        var account = new Account { Id = command.AccountId, Type = AccountType.Deposit, Currency = "RUB" };
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _currencyService.Setup(x => x.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<decimal, string, string>((amount, _, _) => amount);
        _mapper.Setup(x => x.Map<Transaction>(command)).Returns(new Transaction { Type = command.Type });

        var result = await _service.CreateTransactionAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CancelTransaction_Success()
    {
        var transactionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = transactionId,
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Credit,
            Amount = 100m,
            Currency = "RUB",
            IsCanceled = false
        };
        var account = new Account
        {
            Id = transaction.AccountId, Balance = 200m, Currency = "RUB", Type = AccountType.Checking, OwnerId = ownerId
        };

        _transactionRepo.Setup(x => x.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _currencyService.Setup(x => x.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<decimal, string, string>((amount, _, _) => amount);

        var result = await _service.CancelTransactionAsync(transactionId, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(transaction.IsCanceled);
        _transactionRepo.Verify(x => x.UpdateTransactionAsync(transaction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelTransaction_NotFound_Failure()
    {
        var transactionId = Guid.NewGuid();
        _transactionRepo.Setup(x => x.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await _service.CancelTransactionAsync(transactionId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CancelTransaction_AlreadyCanceled_Failure()
    {
        var transactionId = Guid.NewGuid();
        var transaction = new Transaction { Id = transactionId, IsCanceled = true };
        _transactionRepo.Setup(x => x.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var result = await _service.CancelTransactionAsync(transactionId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CancelTransaction_UnauthorizedAccess_Failure()
    {
        var transactionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var transaction = new Transaction { Id = transactionId, AccountId = Guid.NewGuid(), IsCanceled = false };
        var account = new Account { Id = transaction.AccountId, OwnerId = Guid.NewGuid() };

        _transactionRepo.Setup(x => x.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _service.CancelTransactionAsync(transactionId, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task CancelTransaction_InsufficientFundsForCancel_Failure()
    {
        var transactionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = transactionId,
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Credit,
            Amount = 200m,
            Currency = "RUB",
            IsCanceled = false
        };
        var account = new Account
        {
            Id = transaction.AccountId, Balance = 100m, Currency = "RUB", Type = AccountType.Checking, OwnerId = ownerId
        };

        _transactionRepo.Setup(x => x.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _currencyService.Setup(x => x.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<decimal, string, string>((amount, _, _) => amount);

        var result = await _service.CancelTransactionAsync(transactionId, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }
}