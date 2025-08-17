using AccountService.Common.Interfaces.Repository;
using AccountService.Features.Accounts.Commands.DeleteAccount;
using AccountService.Features.Accounts.Model;
using Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Unit;

public class DeleteAccountHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly DeleteAccountHandler _handler;
    private readonly Mock<ILogger<DeleteAccountHandler>> _logger = new();
    private readonly Mock<IOutboxService> _outboxService = new();

    public DeleteAccountHandlerTests()
    {
        _handler = new DeleteAccountHandler(_accountRepo.Object, _outboxService.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = ownerId,
            Balance = 0m,
            IsDeleted = false
        };

        _accountRepo.Setup(x => x.GetAccountByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var command = new DeleteAccountCommand { AccountId = accountId, OwnerId = ownerId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(account.IsDeleted);
        Assert.NotNull(account.ClosedAt);
        _accountRepo.Verify(x => x.UpdateAccountAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_Failure()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.Setup(x => x.GetAccountByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var command = new DeleteAccountCommand { AccountId = accountId, OwnerId = Guid.NewGuid() };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task Handle_UnauthorizedAccess_Failure()
    {
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, OwnerId = Guid.NewGuid(), Balance = 0m };

        _accountRepo.Setup(x => x.GetAccountByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var command = new DeleteAccountCommand { AccountId = accountId, OwnerId = Guid.NewGuid() };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.CommandError!.StatusCode);
    }

    [Fact]
    public async Task Handle_NonZeroBalance_Failure()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = ownerId,
            Balance = 100m
        };

        _accountRepo.Setup(x => x.GetAccountByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var command = new DeleteAccountCommand { AccountId = accountId, OwnerId = ownerId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.CommandError!.StatusCode);
        Assert.Equal("Счет не пуст", result.CommandError.Message);
    }
}
