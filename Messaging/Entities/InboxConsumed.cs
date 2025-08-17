using System.ComponentModel.DataAnnotations;

namespace Messaging.Entities;

public class InboxConsumed
{
    public Guid MessageId { get; set; }

    [MaxLength(100)] public string ServiceName { get; set; } = null!;

    [MaxLength(100)] public string Handler { get; set; } = null!;

    public DateTime ConsumedAtUtc { get; set; }
}
