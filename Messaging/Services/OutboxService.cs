using System.Reflection;
using System.Text.Json;
using Messaging.Attribute;
using Messaging.Configuration;
using Messaging.Entities;
using Messaging.Events;
using Messaging.Exceptions;
using Messaging.Extensions;
using Messaging.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messaging.Services;

internal class OutboxService(
    IMessagingDbContext context,
    IMessageBroker messageBroker,
    ServerSettings settings,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OutboxService> logger) : IOutboxService
{
    private readonly string _exchange = settings.Exchange;
    private readonly string _serviceName = settings.ServiceName;


    public async Task AddAsync<T>(T payload, CancellationToken ct = default) where T : IEvent
    {
        var routingKey = typeof(T).GetCustomAttribute<RoutingKeyAttribute>();
        if (routingKey is null)
            throw new InvalidOperationException($"Тип {typeof(T)} не имеет атрибута RoutingKeyAttribute");
        await AddAsync(payload, routingKey.Key, ct);
    }

    public async Task AddAsync<T>(T payload, string routingKey, CancellationToken ct = default) where T : IEvent
    {
        try
        {
            var correlationId = httpContextAccessor.GetCorrelationId() ?? Guid.NewGuid().ToString();
            var causationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, object?>
            {
                ["X-Correlation-Id"] = correlationId,
                ["X-Causation-Id"] = causationId
            };

            var payloadJson = JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                ServiceName = _serviceName,
                RoutingKey = routingKey,
                Exchange = _exchange,
                PayloadJson = payloadJson,
                HeadersJson = JsonSerializer.Serialize(headers)
            };

            await context.OutboxMessages.AddAsync(outboxMessage, ct);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError("Ошибка сохранения сообщения в Outbox {RoutingKey}: {Error}", routingKey, ex.Message);
            throw;
        }
    }

    public async Task PublishAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var message = await context.OutboxMessages.FirstOrDefaultAsync(om => om.Id == eventId, ct);
            if (message == null || message.Status == OutboxMessageStatus.Sent ||
                message.Status == OutboxMessageStatus.Blocked)
                return;

            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(message.HeadersJson)?
                .ToDictionary(kvp => kvp.Key, object? (kvp) => kvp.Value) ?? new Dictionary<string, object?>();

            try
            {
                var correlationId = headers.TryGetValue("X-Correlation-Id", out var corrId) &&
                                    corrId is string corrStr && Guid.TryParse(corrStr, out _)
                    ? corrStr
                    : httpContextAccessor.HttpContext.GetCorrelationId() ?? Guid.NewGuid().ToString();
                var causationId = Guid.NewGuid().ToString();

                var envelope = MessageEnvelope.Create(
                    message.Id,
                    message.CreatedAtUtc,
                    message.ServiceName,
                    correlationId,
                    causationId,
                    message.PayloadJson
                );

                await messageBroker.PublishEnvelopeAsync(message.Exchange, message.RoutingKey, envelope, ct);
                message.PublishedAtUtc = DateTime.UtcNow;
                message.Status = OutboxMessageStatus.Sent;
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message;
                message.LastAttemptAtUtc = DateTime.UtcNow;
                message.Status = OutboxMessageStatus.Error;

                if (IsFormatError(ex))
                {
                    message.FormatErrorCount++;
                    if (message is { PublishAttempts: >= 10, FormatErrorCount: >= 10 })
                    {
                        message.Status = OutboxMessageStatus.Blocked;
                        logger.LogWarning("Сообщение {EventId} заблокировано после 10 ошибок формата", eventId);
                    }
                }

                if (IsRabbitMqConnectionError(ex))
                    throw new MessagingConnectionException($"Ошибка подключения к брокеру сообщений: {ex.Message}");

                throw;
            }
            finally
            {
                message.PublishAttempts += 1;
                await context.SaveChangesAsync(ct);
            }
        }
        catch (MessagingConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Ошибка публикации сообщения {EventId}: {Error}", eventId, ex.Message);
            throw;
        }
    }

    public async Task<List<Guid>> GetUnpublishedAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.OutboxMessages
                .Where(x => x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Error)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError("Ошибка получения неопубликованных сообщений: {Error}", ex.Message);
            throw;
        }
    }

    private static bool IsRabbitMqConnectionError(Exception ex)
    {
        return ex.GetType().Name.Contains("BrokerUnreachable") ||
               ex.GetType().Name.Contains("ConnectFailure") ||
               ex.Message.Contains("Connection refused") ||
               ex.Message.Contains("No connection could be made") ||
               ex.Message.Contains("None of the specified endpoints were reachable");
    }

    private static bool IsFormatError(Exception ex)
    {
        return ex.GetType().Name.Contains("JsonException") ||
               ex.GetType().Name.Contains("JsonReaderException") ||
               ex.GetType().Name.Contains("SerializationException") ||
               ex.GetType().Name.Contains("ArgumentException") ||
               ex.Message.Contains("json", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("format") ||
               ex.Message.Contains("serialize") ||
               ex.Message.Contains("deserialize") ||
               ex.Message.Contains("invalid start of a property name") ||
               ex.Message.Contains("Expected a");
    }
}