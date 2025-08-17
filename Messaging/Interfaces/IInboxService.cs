namespace Messaging.Interfaces;

public interface IInboxService
{
    Task<bool> IsAlreadyConsumedAsync(Guid messageId, string handler, CancellationToken ct = default);
    Task MarkAsConsumedAsync(Guid messageId, string handler, CancellationToken ct = default);

    Task MoveToDeadLetterAsync(Guid messageId, string routingKey,
        string payloadJson, string headersJson, string errorMessage, CancellationToken ct = default);
}
