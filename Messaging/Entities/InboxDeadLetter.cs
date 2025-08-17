using System.ComponentModel.DataAnnotations;

namespace Messaging.Entities;


public class InboxDeadLetter
{
    public Guid MessageId { get; set; }

    [MaxLength(100)] public string ServiceName { get; set; } = null!;

    [MaxLength(200)] public string RoutingKey { get; set; } = null!;

    [MaxLength(8000)] public string PayloadJson { get; set; } = null!;

    [MaxLength(2000)] public string HeadersJson { get; set; } = "{}";

    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    [MaxLength(2000)] public string? ErrorMessage { get; set; }
}
