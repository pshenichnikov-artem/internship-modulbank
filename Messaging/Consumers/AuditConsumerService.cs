using Messaging.Configuration;
using Messaging.Constants;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Messaging.Consumers;

public sealed class AuditConsumerService(
    IServiceProvider serviceProvider,
    ILogger<AuditConsumerService> logger,
    IConnectionFactory connectionFactory,
    ServerSettings settings)
    : BaseConsumer(serviceProvider, logger, connectionFactory, settings.AuditQueueName, settings)
{
    protected override string HandlerName => "AuditConsumerService";

    protected override async Task HandlePayloadAsync(IServiceProvider scope, string routingKey,
        MessageEnvelope envelope,
        CancellationToken ct)
    {
        if (!RoutingKeys.IsValid(routingKey))
        {
            logger.LogError("Неподдерживаемый routingKey {RoutingKey}", routingKey);
            throw new InvalidOperationException($"Неподдерживаемый routingKey {routingKey}");
        }

        var auditService = scope.GetRequiredService<IAuditService>();

        await auditService.SaveAuditEventAsync(routingKey, envelope.Payload.RootElement.GetRawText(),
            envelope.Meta.CorrelationId, ct);
    }
}