using System.ComponentModel.DataAnnotations;

namespace Messaging.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    [MaxLength(100)] public string ServiceName { get; set; } = null!;

    [MaxLength(200)] public string RoutingKey { get; set; } = null!;

    [MaxLength(200)] public string Exchange { get; set; } = null!;

    [MaxLength(8000)] public string PayloadJson { get; set; } = null!;

    [MaxLength(2000)] public string HeadersJson { get; set; } = "{}";

    public int PublishAttempts { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }

    [MaxLength(2000)] public string? LastError { get; set; }
    
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int FormatErrorCount { get; set; }
}
