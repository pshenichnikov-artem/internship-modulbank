using System.ComponentModel.DataAnnotations;

namespace Messaging.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }

    [MaxLength(100)] public string ServiceName { get; set; } = null!;

    [MaxLength(200)] public string RoutingKey { get; set; } = null!;

    [MaxLength(8000)] public string PayloadJson { get; set; } = null!;

    public DateTime ReceivedAtUtc { get; set; }

    [MaxLength(100)] public string? CorrelationId { get; set; }
}
