namespace Messaging.Interfaces;

public interface IAuditService
{
    Task SaveAuditEventAsync(string routingKey, string payloadJson, string? correlationId = null,
        CancellationToken ct = default);
}
