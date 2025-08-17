using Messaging.Configuration;
using Messaging.Entities;
using Messaging.Interfaces;

namespace Messaging.Services;

internal class AuditService(IMessagingDbContext context, ServerSettings options) : IAuditService
{
    public async Task SaveAuditEventAsync(string routingKey, string payloadJson, string? correlationId = null,
        CancellationToken ct = default)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ServiceName = options.ServiceName,
            RoutingKey = routingKey,
            PayloadJson = payloadJson,
            ReceivedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        await context.AuditEvents.AddAsync(auditEvent, ct);
        await context.SaveChangesAsync(ct);
    }
}