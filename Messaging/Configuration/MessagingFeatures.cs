namespace Messaging.Configuration;

[Flags]
public enum MessagingFeatures
{
    None = 0,
    Outbox = 1 << 0,
    Inbox = 1 << 1,
    Audit = 1 << 2,
    Broker = 1 << 3,
    Consumers = 1 << 4
}
