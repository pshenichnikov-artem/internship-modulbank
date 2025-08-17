using Messaging.Configuration;
using Messaging.Entities;
using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messaging.Services;

internal class InboxService(IMessagingDbContext context, ServerSettings settings, ILogger<InboxService> logger)
    : IInboxService
{
    public async Task MoveToDeadLetterAsync(Guid messageId, string routingKey,
        string payloadJson, string headersJson, string errorMessage, CancellationToken ct = default)
    {
        try
        {
            var existing = await context.InboxDeadLetters.FindAsync([messageId], ct);
            if (existing != null)
            {
                existing.LastAttemptAtUtc = DateTime.UtcNow;
                existing.ErrorMessage = errorMessage;
            }
            else
            {
                context.InboxDeadLetters.Add(new InboxDeadLetter
                {
                    MessageId = messageId,
                    ServiceName = settings.ServiceName,
                    RoutingKey = routingKey,
                    PayloadJson = payloadJson,
                    HeadersJson = headersJson,
                    ReceivedAtUtc = DateTime.UtcNow,
                    LastAttemptAtUtc = DateTime.UtcNow,
                    ErrorMessage = errorMessage
                });
            }

            await context.SaveChangesAsync(ct);
            logger.LogWarning("DeadLetter сохранен {MessageId} {RoutingKey}", messageId, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError("Ошибка сохранения DeadLetter {MessageId} {RoutingKey}: {Error}", messageId, routingKey, ex.Message);
            throw;
        }
    }

    public async Task MarkAsConsumedAsync(Guid messageId, string handler,
        CancellationToken ct = default)
    {
        try
        {
            var existing = await context.InboxConsumed
                .FirstOrDefaultAsync(
                    p => p.MessageId == messageId && p.ServiceName == settings.ServiceName && p.Handler == handler,
                    ct);
            if (existing != null)
            {
                logger.LogWarning("Попытка повторной отметки сообщения {MessageId} {ServiceName} {Handler}", messageId,
                    settings.ServiceName, handler);
                var outbox = await context.OutboxMessages.FirstAsync(om => om.Id == messageId, ct);
                await MoveToDeadLetterAsync(messageId, outbox.RoutingKey, outbox.PayloadJson, outbox.HeadersJson,
                    "Attempt to mark already consumed message", ct);
                return;
            }

            context.InboxConsumed.Add(new InboxConsumed
            {
                MessageId = messageId,
                ServiceName = settings.ServiceName,
                Handler = handler,
                ConsumedAtUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError("Ошибка сохранения InboxConsumed {MessageId} {ServiceName} {Handler}: {Error}", messageId,
                settings.ServiceName, handler, ex.Message);
            throw;
        }
    }

    public async Task<bool> IsAlreadyConsumedAsync(Guid messageId, string handler,
        CancellationToken ct = default)
    {
        return await context.InboxConsumed.FirstOrDefaultAsync(
                   ic => ic.MessageId == messageId && ic.ServiceName == settings.ServiceName && ic.Handler == handler,
                   ct) !=
               null;
    }
}