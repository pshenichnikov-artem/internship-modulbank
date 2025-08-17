using Messaging.Configuration;

namespace Messaging.Extensions;

public static class MessagingOptionsExtensions
{
    public static MessagingOptions UseFullMessaging(this MessagingOptions options)
    {
        return options
            .UseOutbox()
            .UseInbox()
            .UseAudit()
            .UseRabbitMq()
            .AddOutboxDispatcher()
            .AddAuditConsumer();
    }
}