namespace Messaging.Configuration;

public static class MessagingValidator
{
    public static void ValidateConfiguration(MessagingOptions options)
    {
        var features = options.EnabledFeatures;
        
        // Consumers требуют Broker
        if (features.HasFlag(MessagingFeatures.Consumers) && !features.HasFlag(MessagingFeatures.Broker))
        {
            throw new InvalidOperationException("Consumers require Broker to be configured. Call UseRabbitMq() or another broker configuration before adding consumers.");
        }
        
        // Consumers требуют Inbox для обработки дубликатов
        if (features.HasFlag(MessagingFeatures.Consumers) && !features.HasFlag(MessagingFeatures.Inbox))
        {
            throw new InvalidOperationException("Consumers require Inbox pattern for duplicate detection. Call UseInbox() before adding consumers.");
        }
        
        // Outbox требует Broker для отправки сообщений
        if (features.HasFlag(MessagingFeatures.Outbox) && !features.HasFlag(MessagingFeatures.Broker))
        {
            throw new InvalidOperationException("Outbox pattern requires Broker to be configured for message publishing.");
        }
    }
}