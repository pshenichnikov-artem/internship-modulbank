using System.Net;
using System.Net.Http.Json;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace AccountServiceTests.Integration;

[Collection("ParallelTransfer")]
public class ParallelTransferTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
    : BaseIntegrationTest(fixture, testOutputHelper)
{
    [Fact]
    public async Task ParallelTransfers_ShouldPreserveTotalBalance()
    {
        using var scope = Factory.Services.CreateScope();
        var context =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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

        var totalInitialBalance = account1.Balance + account2.Balance;

        var tasks = Enumerable.Range(0, 50).Select(i =>
        {
            var fromId = i % 2 == 0 ? account1.Id : account2.Id;
            var toId = i % 2 == 0 ? account2.Id : account1.Id;

            var command = new CreateTransactionCommand
            {
                AccountId = fromId,
                CounterpartyAccountId = toId,
                Amount = transferAmount,
                Currency = "RUB",
                Type = TransactionType.Transfer,
                Description = $"Transfer {i + 1}"
            };

            return Client.PostAsJsonAsync("/api/v1/transactions", command);
        });

        var responses = await Task.WhenAll(tasks);

        var conflictCount = 0;
        foreach (var response in responses)
            if (response.StatusCode == HttpStatusCode.Conflict)
                conflictCount++;
            else
                response.EnsureSuccessStatusCode();

        Output.WriteLine($"Количество ответов с ошибкой 409: {conflictCount}");

        var updatedAccount1 = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account1.Id);
        var updatedAccount2 = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account2.Id);

        var totalFinalBalance = updatedAccount1.Balance + updatedAccount2.Balance;
        Assert.Equal(totalInitialBalance, totalFinalBalance);
    }


    [Fact]
    public async Task CancelTransactions_Parallel_CorrectBalances()
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

        var transactions = new List<Transaction>();

        for (var i = 0; i < 50; i++)
        {
            var transferTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = i % 2 == 0 ? account1.Id : account2.Id,
                CounterpartyAccountId = i % 2 == 0 ? account2.Id : account1.Id,
                Amount = transferAmount,
                Currency = "RUB",
                Type = TransactionType.Transfer,
                Description = $"Transfer {i + 1}",
                Timestamp = DateTime.UtcNow
            };
            transactions.Add(transferTransaction);
        }

        context.Accounts.AddRange(account1, account2);
        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var cancelTasks = transactions.Select(t =>
            Client.PostAsync($"/api/v1/transactions/{t.Id}/cancel", null));

        var cancelResponses = await Task.WhenAll(cancelTasks);
        var conflictCount = 0;

        foreach (var response in cancelResponses)
            if (response.StatusCode == HttpStatusCode.Conflict)
                conflictCount++;
            else
                response.EnsureSuccessStatusCode();

        Output.WriteLine($"Количество ответов с ошибкой 409: {conflictCount}");

        var updatedAccount1 = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account1.Id);
        var updatedAccount2 = await context.Accounts.AsNoTracking().FirstAsync(a => a.Id == account2.Id);

        var totalFinalBalance = updatedAccount1.Balance + updatedAccount2.Balance;

        Assert.True(updatedAccount1.Balance >= 0);
        Assert.True(updatedAccount2.Balance >= 0);
        Assert.Equal(initialBalance * 2, totalFinalBalance);
    }
}