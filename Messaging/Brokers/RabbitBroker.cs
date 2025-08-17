using System.Diagnostics;
using System.Text;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Messaging.Brokers;

public sealed class RabbitBroker(IConnectionFactory factory, ILogger<RabbitBroker> logger)
    : IMessageBroker
{
    public async Task PublishEnvelopeAsync(string exchange, string routingKey, MessageEnvelope envelope,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var eventId = envelope.EventId;
        var correlationId = envelope.Meta.CorrelationId;

        try
        {
            await using var conn = await factory.CreateConnectionAsync(ct);
            await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Headers = new Dictionary<string, object?>
                {
                    ["X-Correlation-Id"] = correlationId,
                    ["X-Causation-Id"] = envelope.Meta.CausationId
                }
            };

            var body = Encoding.UTF8.GetBytes(envelope.ToJson());
            await channel.BasicPublishAsync(exchange, routingKey, false, props, body, ct);

            sw.Stop();
            logger.LogInformation(
                "Сообщение опубликовано {EventId} в {Exchange}/{RoutingKey} за {LatencyMs}мс {CorrelationId}",
                eventId, exchange, routingKey, sw.ElapsedMilliseconds, correlationId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                "Ошибка публикации {EventId} в {Exchange}/{RoutingKey} за {LatencyMs}мс {CorrelationId}: {ErrorMessage}",
                eventId, exchange, routingKey, sw.ElapsedMilliseconds, correlationId, ex.Message);
            throw;
        }
    }
}