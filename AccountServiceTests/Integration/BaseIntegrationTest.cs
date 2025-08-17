using System.Security.Claims;
using System.Text.Encodings.Web;
using AccountService;
using AccountService.Common.Interfaces.Service;
using AccountService.Infrastructure.Data;
using Hangfire;
using Hangfire.MemoryStorage;
using Messaging.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace AccountServiceTests.Integration;

public class IntegrationTestFixture : IAsyncLifetime
{
    public readonly PostgreSqlContainer Postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("testdb")
        .Build();

    public readonly RabbitMqContainer RabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await RabbitMq.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}

public abstract class BaseIntegrationTest(IntegrationTestFixture fixture, ITestOutputHelper output)
    : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture = fixture;
    protected readonly ITestOutputHelper Output = output;
    protected readonly string QueuePrefix = $"test_{Guid.NewGuid():N}";
    protected readonly string SchemaName = $"test_{Guid.NewGuid():N}";
    protected readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;

    public virtual async Task InitializeAsync()
    {
        for (var i = 0; i < 10; i++)
            try
            {
                Factory = new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("Testing");
                        builder.ConfigureAppConfiguration((_, config) =>
                        {
                            config.Sources.Clear();
                            config.AddInMemoryCollection(GetTestSettings());
                        });
                        builder.ConfigureServices(ConfigureServices);
                        builder.ConfigureLogging(logging =>
                        {
                            logging.AddConsole();
                            logging.SetMinimumLevel(LogLevel.Warning);
                        });
                    });

                _ = Factory.Server;
                Client = Factory.CreateClient();

                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {SchemaName}");
                await context.Database.ExecuteSqlRawAsync($"SET search_path TO {SchemaName}");
                await context.Database.EnsureCreatedAsync();

                await SetupTestQueues();

                break;
            }
            catch (Exception ex) when (i < 9)
            {
                var delay = i * 1000;

                await Task.Delay(Math.Min(delay, 5000));
                Output.WriteLine($"Ошибка при инициализации: {ex}");
            }
    }

    public virtual async Task DisposeAsync()
    {
        try
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS {SchemaName} CASCADE");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"Ошибка при очистке: {ex}");
        }

        await Factory.DisposeAsync();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IClientService, FakeClientService>();

        services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(Fixture.Postgres.GetConnectionString()));

        services.RemoveAll(typeof(IGlobalConfiguration));
        services.AddHangfire(config => config.UseMemoryStorage());
        services.AddHangfireServer();

        services.RemoveAll<IConnectionFactory>();
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            HostName = "localhost",
            Port = Fixture.RabbitMq.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/"
        });

        services.RemoveAll<ServerSettings>();
        services.AddSingleton(new ServerSettings
        {
            ServiceName = QueuePrefix,
            Exchange = $"{QueuePrefix}.events",
            AuditQueueName = $"{QueuePrefix}.audit"
        });

        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        services.PostConfigureAll<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = "Test";
            options.DefaultChallengeScheme = "Test";
        });
    }

    protected virtual Dictionary<string, string?> GetTestSettings()
    {
        var testSettings = new Dictionary<string, string?>
        {
            ["Authentication:AuthenticationServerUrl"] = "http://fake-keycloak",
            ["Authentication:Audience"] = "test-audience",
            ["Authentication:Realm"] = "modulbank",
            ["Authentication:AdminUsername"] = "admin",
            ["Authentication:AdminPassword"] = "admin",
            ["ConnectionStrings:DefaultConnection"] = Fixture.Postgres.GetConnectionString(),
            ["ConnectionStrings:MessagingConnection"] = Fixture.Postgres.GetConnectionString(),
            ["Messaging:ServiceName"] = QueuePrefix,
            ["Messaging:Exchange"] = $"{QueuePrefix}.events",
            ["Messaging:AuditQueueName"] = $"{QueuePrefix}.audit",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:Port"] = Fixture.RabbitMq.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:User"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["RabbitMq:VirtualHost"] = "/"
        };

        return testSettings;
    }

    private async Task SetupTestQueues()
    {
        using var scope = Factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var exchange = $"{QueuePrefix}.events";
        await channel.ExchangeDeclareAsync(exchange, "topic", true);

        await channel.QueueDeclareAsync($"{QueuePrefix}.crm", true, false, false);
        await channel.QueueDeclareAsync($"{QueuePrefix}.notifications", true, false, false);
        await channel.QueueDeclareAsync("account.antifraud", true, false, false);
        await channel.QueueDeclareAsync($"{QueuePrefix}.audit", true, false, false);

        await channel.QueueBindAsync($"{QueuePrefix}.crm", exchange, "account.*");
        await channel.QueueBindAsync($"{QueuePrefix}.notifications", exchange, "money.*");
        await channel.QueueBindAsync("account.antifraud", exchange, "client.*");
        await channel.QueueBindAsync($"{QueuePrefix}.audit", exchange, "#");

        await channel.CloseAsync();
        await connection.CloseAsync();
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

    public class FakeClientService : IClientService
    {
        public Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}