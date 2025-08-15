using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AccountService.Common.Interfaces.Service;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Commands.CreateTransaction;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure.Data;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace AccountServiceTests.Integration;

public class ParallelTransferTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("testdb")
        .Build();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private HttpClient _client = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    // Добавляем тестовые настройки для AuthenticationSettings и ConnectionStrings, иначе будет исключение
                    var testSettings = new Dictionary<string, string?>
                    {
                        ["Authentication:AuthenticationServerUrl"] = "http://fake-keycloak",
                        ["Authentication:Audience"] = "test-audience",
                        ["Authentication:Realm"] = "modulbank",
                        ["Authentication:AdminUsername"] = "admin",
                        ["Authentication:AdminPassword"] = "admin",
                        ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                    };
                    config.AddInMemoryCollection(testSettings);
                });
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_postgres.GetConnectionString());
                    });

                    services.AddScoped<IClientService, FakeClientService>();

                    services.RemoveAll(typeof(IGlobalConfiguration));
                    services.AddHangfire(config => config.UseMemoryStorage());
                    services.AddHangfireServer();

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                    services.PostConfigureAll<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    });
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ParallelTransfers_ShouldPreserveTotalBalance()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const decimal initialBalance = 10000m;
        const decimal transferAmount = 100m;

        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = _userId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };

        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = _userId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };

        dbContext.Accounts.AddRange(account1, account2);
        await dbContext.SaveChangesAsync();

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

            return _client.PostAsJsonAsync("/api/v1/transactions", command);
        });

        var responses = await Task.WhenAll(tasks);

        var conflictCount = 0;
        foreach (var response in responses)
            if (response.StatusCode == HttpStatusCode.Conflict)
                conflictCount++;
            else
                response.EnsureSuccessStatusCode();

        testOutputHelper.WriteLine($"Количество ответов с ошибкой 409: {conflictCount}");

        var updatedAccount1 = await dbContext.Accounts.AsNoTracking().FirstAsync(a => a.Id == account1.Id);
        var updatedAccount2 = await dbContext.Accounts.AsNoTracking().FirstAsync(a => a.Id == account2.Id);

        testOutputHelper.WriteLine($"Версия первого аккаунта: {updatedAccount1.Version}");
        var totalFinalBalance = updatedAccount1.Balance + updatedAccount2.Balance;
        Assert.Equal(totalInitialBalance, totalFinalBalance);
    }


    [Fact]
    public async Task CancelTransactions_Parallel_CorrectBalances()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const decimal initialBalance = 10000m;
        const decimal transferAmount = 100m;

        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = _userId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };
        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = _userId,
            Type = AccountType.Checking,
            Currency = "RUB",
            Balance = initialBalance,
            OpenedAt = DateTime.UtcNow
        };

        var transactions = new List<Transaction>();
        for (var i = 0; i < 25; i++)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = i % 2 == 0 ? account1.Id : account2.Id,
                CounterpartyAccountId = null,
                Amount = transferAmount,
                Currency = "RUB",
                Type = i % 2 == 0 ? TransactionType.Credit : TransactionType.Debit,
                Description = $"Transfer {i + 1}",
                Timestamp = DateTime.UtcNow
            };
            transactions.Add(transaction);
            transaction = new Transaction
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
            transactions.Add(transaction);
        }

        dbContext.Accounts.AddRange(account1, account2);
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();

        var totalInitialBalance = account1.Balance + account2.Balance;

        var cancelTasks = transactions.Select(t =>
            _client.PostAsync($"/api/v1/transactions/{t.Id}/cancel", null));

        var cancelResponses = await Task.WhenAll(cancelTasks);
        var conflictCount = 0;

        foreach (var response in cancelResponses)
            if (response.StatusCode == HttpStatusCode.Conflict)
                conflictCount++;
            else
                response.EnsureSuccessStatusCode();

        testOutputHelper.WriteLine($"Количество ответов с ошибкой 409: {conflictCount}");

        var updatedAccount1 = await dbContext.Accounts.AsNoTracking().FirstAsync(a => a.Id == account1.Id);
        var updatedAccount2 = await dbContext.Accounts.AsNoTracking().FirstAsync(a => a.Id == account2.Id);

        var totalFinalBalance = updatedAccount1.Balance + updatedAccount2.Balance;

        Assert.True(updatedAccount1.Balance >= 0);
        Assert.True(updatedAccount2.Balance >= 0);
        Assert.Equal(totalInitialBalance, totalFinalBalance);
    }

    public class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim("sub", "11111111-1111-1111-1111-111111111111") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

public class FakeClientService : IClientService
{
    public Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}