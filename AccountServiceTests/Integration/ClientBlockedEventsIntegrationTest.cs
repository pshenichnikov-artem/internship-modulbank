using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure.Data;
using Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit.Abstractions;

namespace AccountServiceTests.Integration;

public class ClientBlockedEventsIntegrationTest(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task BlockedAccount_ShouldPreventDebit()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = 1000m,
            OpenedAt = DateTime.UtcNow,
            IsFrozen = true
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var debitCommand = new CreateTransactionCommand
        {
            AccountId = account.Id,
            Amount = 100m,
            Currency = "RUB",
            Type = TransactionType.Debit,
            Description = "Test debit"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/transactions", debitCommand);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var updatedAccount = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        Assert.Equal(1000m, updatedAccount.Balance);
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientUnblockedEvent_ManualPublish_ShouldWork(bool freeze)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = 1000m,
            OpenedAt = DateTime.UtcNow,
            IsFrozen = freeze
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        object clientEvent = freeze ? new ClientUnblocked(UserId) : new ClientBlocked(UserId);
        var correlationId = Guid.NewGuid().ToString();
        var causationId = Guid.NewGuid().ToString();

        var payloadJson = JsonSerializer.Serialize(clientEvent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var envelope = MessageEnvelope.Create(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "account-service",
            correlationId,
            causationId,
            payloadJson
        );

        using var scope1 = Factory.Services.CreateScope();
        var factory = scope1.ServiceProvider.GetRequiredService<IConnectionFactory>();

        await using var conn = await factory.CreateConnectionAsync();
        await using var ch = await conn.CreateChannelAsync();

        await Task.Delay(5000);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                ["X-Correlation-Id"] = correlationId,
                ["X-Causation-Id"] = causationId
            }
        };

        var body = Encoding.UTF8.GetBytes(envelope.ToJson());
        var routeKey = freeze ? "client.unblocked" : "client.blocked";
        await ch.BasicPublishAsync($"{QueuePrefix}.events", routeKey, false, props, body);


        await Task.Delay(5000);

        var updatedAccount = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        Assert.Equal(updatedAccount.IsFrozen, !freeze);
    }
}