using Messaging.Configuration;
using Messaging.HealthChecks;
using Messaging.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NotResolvedInText

namespace Messaging.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseMessaging<TDbContext>(this IServiceCollection services,
        IConfiguration configuration,
        Action<MessagingOptions> configure) where TDbContext : class, IMessagingDbContext
    {
        var options = new MessagingOptions();
        configure(options);

        MessagingValidator.ValidateConfiguration(options);

        var serverSettings = new ServerSettings
        {
            ServiceName = options.ServiceName ?? configuration["Messaging:ServiceName"]
                ?? throw new ArgumentNullException("Messaging:ServiceName"),
            Exchange = options.Exchange ?? configuration["Messaging:Exchange"]
                ?? throw new ArgumentNullException("Messaging:Exchange"),
            AuditQueueName = options.AuditQueueName ?? configuration["Messaging:AuditQueueName"]
                ?? throw new ArgumentNullException("Messaging:AuditQueueName")
        };

        services.AddSingleton(serverSettings);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


        services.AddScoped<IMessagingDbContext>(provider => provider.GetRequiredService<TDbContext>());

        RegisterBrokerComponents(services, options, configuration);
        RegisterServiceComponents(services, options);
        RegisterConsumers(services, options, configuration);
        RegisterHealthChecks(services);

        return services;
    }


    private static void RegisterBrokerComponents(IServiceCollection services, MessagingOptions options,
        IConfiguration configuration)
    {
        if (!options.EnabledFeatures.HasFlag(MessagingFeatures.Broker))
            return;

        options.BrokerConfiguration?.Invoke(services, configuration);
    }

    private static void RegisterServiceComponents(IServiceCollection services, MessagingOptions options)
    {
        foreach (var serviceConfig in options.ServiceConfigurations) serviceConfig.Invoke(services, null!);
    }

    private static void RegisterConsumers(IServiceCollection services, MessagingOptions options,
        IConfiguration configuration)
    {
        if (!options.EnabledFeatures.HasFlag(MessagingFeatures.Consumers))
            return;

        foreach (var consumerConfig in options.ConsumerConfigurations) consumerConfig.Invoke(services, configuration);
    }

    private static void RegisterHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<OutboxLiveHealthCheck>("outbox-live", tags: ["live"])
            .AddCheck<RabbitMqLiveHealthCheck>("rabbitmq-live", tags: ["live"])
            .AddCheck<OutboxHealthCheck>("outbox-ready", tags: ["ready"])
            .AddCheck<RabbitMqHealthCheck>("rabbitmq-ready", tags: ["ready"]);
    }
}