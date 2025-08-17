namespace Messaging.Entities;

public enum OutboxMessageStatus
{
    Pending = 0,
    Sent = 1,
    Error = 2,
    Blocked = 3
}
