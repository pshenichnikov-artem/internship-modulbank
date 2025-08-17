using Messaging.Events;

namespace Messaging.Interfaces;

public interface IMessageBroker
{
    Task PublishEnvelopeAsync(string exchange, string routingKey, MessageEnvelope envelope,
        CancellationToken ct = default);
}