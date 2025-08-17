using Messaging.Extensions;
using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace MessagingLibraryTests.Integration;

public class MessagingTestFixture : IAsyncLifetime
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

public abstract class BaseMessagingIntegrationTest(MessagingTestFixture fixture)
    : IClassFixture<MessagingTestFixture>, IAsyncLifetime
{
    protected readonly MessagingTestFixture Fixture = fixture;
    protected readonly string QueuePrefix = $"test_{Guid.NewGuid():N}";
    protected readonly string SchemaName = $"test_{Guid.NewGuid():N}";
    protected IHost Host = null!;
    protected IServiceProvider Services = null!;

    public virtual async Task InitializeAsync()
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                var testSettings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = Fixture.Postgres.GetConnectionString(),
                    ["Messaging:ServiceName"] = QueuePrefix,
                    ["Messaging:Exchange"] = $"{QueuePrefix}.events",
                    ["Messaging:AuditQueueName"] = $"{QueuePrefix}.audit",
                    ["RabbitMq:Host"] = Fixture.RabbitMq.Hostname,
                    ["RabbitMq:Port"] = Fixture.RabbitMq.GetMappedPublicPort(5672).ToString(),
                    ["RabbitMq:User"] = "guest",
                    ["RabbitMq:Password"] = "guest",
                    ["RabbitMq:VirtualHost"] = "/"
                };

                config.AddInMemoryCollection(testSettings);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<TestDbContext>(opts =>
                    opts.UseNpgsql(Fixture.Postgres.GetConnectionString()));

                services.RemoveAll<IConnectionFactory>();
                services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
                {
                    HostName = Fixture.RabbitMq.Hostname,
                    Port = Fixture.RabbitMq.GetMappedPublicPort(5672),
                    UserName = "guest",
                    Password = "guest",
                    VirtualHost = "/"
                });

                services.UseMessaging<TestDbContext>(context.Configuration, opt =>
                {
                    opt.ServiceName = QueuePrefix;
                    opt.Exchange = $"{QueuePrefix}.events";
                    opt.UseFullMessaging();
                });
            });

        Host = builder.Build();
        Services = Host.Services;
        await EnsureDatabaseCreated();
        await SetupTestQueues();
        await Host.StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();

            await context.Database.ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS {SchemaName} CASCADE");
        }
        catch
        {
            // ignored
        }
        
        await Host.StopAsync();
        Host.Dispose();
    }

    private async Task EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();

        await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {SchemaName}");
        await context.Database.ExecuteSqlRawAsync($"SET search_path TO {SchemaName}");
        await context.Database.EnsureCreatedAsync();
    }

    public async Task SetupTestQueues()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var exchange = $"{QueuePrefix}.events";
        await channel.ExchangeDeclareAsync(exchange, "topic", true);

        await channel.QueueDeclareAsync($"{QueuePrefix}.crm", true, false, false);
        await channel.QueueDeclareAsync($"{QueuePrefix}.notifications", true, false, false);
        await channel.QueueDeclareAsync($"{QueuePrefix}.antifraud", true, false, false);
        await channel.QueueDeclareAsync($"{QueuePrefix}.audit", true, false, false);

        await channel.QueueBindAsync($"{QueuePrefix}.crm", exchange, "account.*");
        await channel.QueueBindAsync($"{QueuePrefix}.notifications", exchange, "money.*");
        await channel.QueueBindAsync($"{QueuePrefix}.antifraud", exchange, "client.*");
        await channel.QueueBindAsync($"{QueuePrefix}.audit", exchange, "#");

        await channel.CloseAsync();
        await connection.CloseAsync();
    }
}