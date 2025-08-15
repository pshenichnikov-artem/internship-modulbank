using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Features.Accounts.Model;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Unit;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ICurrencyService> _currencyService = new();
    private readonly Mock<ILogger<AccountService.Features.Accounts.AccountService>> _logger = new();
    private readonly AccountService.Features.Accounts.AccountService _service;

    public AccountServiceTests()
    {
        _service = new AccountService.Features.Accounts.AccountService(_currencyService.Object, _accountRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task UpdateAccountCurrency_ValidCurrency_Success()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = ownerId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = 1000m,
            OpenedAt = DateTime.UtcNow
        };

        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _currencyService.Setup(x => x.Convert(1000m, "RUB", "USD")).Returns(13.33m);

        var result = await _service.UpdateAccountCurrencyAsync(accountId, ownerId, "USD", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Data!.Currency);
        Assert.Equal(13.33m, result.Data.Balance);
    }

    [Fact]
    public async Task UpdateAccountCurrency_AccountNotFound_Failure()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result =
            await _service.UpdateAccountCurrencyAsync(accountId, Guid.NewGuid(), "USD", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task UpdateAccountCurrency_UnauthorizedAccess_Failure()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account { Id = accountId, OwnerId = Guid.NewGuid() };

        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _service.UpdateAccountCurrencyAsync(accountId, ownerId, "USD", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task UpdateAccountCurrency_UnsupportedCurrency_Failure()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account { Id = accountId, OwnerId = ownerId };

        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _currencyService.Setup(x => x.IsSupportedCurrencyAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateAccountCurrencyAsync(accountId, ownerId, "XXX", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task UpdateAccountCurrency_SameCurrency_Success()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = ownerId,
            Currency = "RUB",
            Balance = 1000m
        };

        _accountRepo.Setup(x => x.GetAccountByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _service.UpdateAccountCurrencyAsync(accountId, ownerId, "RUB", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("RUB", result.Data!.Currency);
        Assert.Equal(1000m, result.Data.Balance);
    }
}