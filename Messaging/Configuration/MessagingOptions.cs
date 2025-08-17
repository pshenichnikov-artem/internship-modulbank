using Messaging.Brokers;
using Messaging.Consumers;
using Messaging.Dispatchers;
using Messaging.Interfaces;
using Messaging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

// ReSharper disable NotResolvedInText

namespace Messaging.Configuration;

public class MessagingOptions
{
    public string? ServiceName { get; set; } = null;
    public string? Exchange { get; set; } = null;
    public string? AuditQueueName { get; set; } = null;

    internal MessagingFeatures EnabledFeatures { get; set; } = MessagingFeatures.None;
    internal Action<IServiceCollection, IConfiguration>? BrokerConfiguration { get; set; }
    internal List<Action<IServiceCollection, IConfiguration>> ServiceConfigurations { get; set; } = [];
    internal List<Action<IServiceCollection, IConfiguration>> ConsumerConfigurations { get; set; } = [];

    public MessagingOptions ConfigureBroker(Action<IServiceCollection, IConfiguration> configure)
    {
        EnabledFeatures |= MessagingFeatures.Broker;
        BrokerConfiguration = configure;
        return this;
    }

    public MessagingOptions UseRabbitMq()
    {
        return ConfigureBroker((services, config) =>
        {
            var cfg = config.GetSection("RabbitMq");
            var connectionFactory = new ConnectionFactory
            {
                HostName = cfg["Host"] ?? throw new ArgumentNullException("RabbitMq:Host"),
                Port = int.Parse(cfg["Port"] ?? throw new ArgumentNullException("RabbitMq:Port")),
                UserName = cfg["User"] ?? throw new ArgumentNullException("RabbitMq:User"),
                Password = cfg["Password"] ?? throw new ArgumentNullException("RabbitMq:Password"),
                VirtualHost = cfg["VirtualHost"] ?? "/"
            };

            services.AddSingleton<IConnectionFactory>(connectionFactory);
            services.AddSingleton<IMessageBroker, RabbitBroker>();
        });
    }

    public MessagingOptions UseOutbox<TService>() where TService : class, IOutboxService
    {
        EnabledFeatures |= MessagingFeatures.Outbox;
        ServiceConfigurations.Add((services, _) => services.AddScoped<IOutboxService, TService>());
        return this;
    }

    public MessagingOptions UseOutbox()
    {
        return UseOutbox<OutboxService>();
    }

    public MessagingOptions UseInbox<TService>() where TService : class, IInboxService
    {
        EnabledFeatures |= MessagingFeatures.Inbox;
        ServiceConfigurations.Add((services, _) => services.AddScoped<IInboxService, TService>());
        return this;
    }

    public MessagingOptions UseInbox()
    {
        return UseInbox<InboxService>();
    }

    public MessagingOptions UseAudit<TService>() where TService : class, IAuditService
    {
        EnabledFeatures |= MessagingFeatures.Audit;
        ServiceConfigurations.Add((services, _) => services.AddScoped<IAuditService, TService>());
        return this;
    }

    public MessagingOptions UseAudit()
    {
        return UseAudit<AuditService>();
    }

    public MessagingOptions AddOutboxDispatcher()
    {
        EnabledFeatures |= MessagingFeatures.Consumers;
        ConsumerConfigurations.Add((services, _) => services.AddHostedService<OutboxDispatcherService>());
        return this;
    }

    public MessagingOptions AddAuditConsumer()
    {
        EnabledFeatures |= MessagingFeatures.Consumers;
        ConsumerConfigurations.Add((services, _) => services.AddHostedService<AuditConsumerService>());
        return this;
    }
}