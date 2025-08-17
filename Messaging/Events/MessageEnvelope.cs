using System.Text.Json;

namespace Messaging.Events;

public sealed record MessageEnvelope(
    Guid EventId,
    DateTime OccurredAt,
    MessageMeta Meta,
    JsonDocument Payload
)
{
    public static MessageEnvelope Create(Guid eventId, DateTime occurredAt, string serviceName, string correlationId,
        string causationId, string payloadJson)
    {
        return new MessageEnvelope(
            eventId,
            occurredAt,
            new MessageMeta("v1", serviceName, correlationId, causationId),
            JsonDocument.Parse(payloadJson)
        );
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static MessageEnvelope FromJson(string json)
    {
        return JsonSerializer.Deserialize<MessageEnvelope>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException("Failed to deserialize MessageEnvelope");
    }
}

public sealed record MessageMeta(
    string Version,
    string Source,
    string CorrelationId,
    string CausationId
);
