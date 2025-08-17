using System.Net.Http.Json;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit.Abstractions;

namespace AccountServiceTests.Integration;

public class OutboxDispatcherIntegrationTest(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task TransferEmitsSingleEvent()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const decimal initialBalance = 10000m;
        const decimal transferAmount = 100m;

        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };

        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };

        context.Accounts.AddRange(account1, account2);
        await context.SaveChangesAsync();

        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();


        for (var i = 0; i < 50; i++)
        {
            var fromId = i % 2 == 0 ? account1.Id : account2.Id;
            var toId = i % 2 == 0 ? account2.Id : account1.Id;

            var command = new CreateTransactionCommand
            {
                AccountId = toId,
                CounterpartyAccountId = fromId,
                Amount = transferAmount,
                Currency = "RUB",
                Type = TransactionType.Transfer,
                Description = $"Transfer {i + 1}"
            };

            await Client.PostAsJsonAsync("/api/v1/transactions", command);
        }

        await Task.Delay(5000);

        var transaction = await context.Transactions.CountAsync();
        var outbox = await context.OutboxMessages.CountAsync();

        Assert.Equal(outbox, transaction);
    }
}