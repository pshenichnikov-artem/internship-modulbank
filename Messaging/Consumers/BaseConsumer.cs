using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Messaging.Configuration;
using Messaging.Events;
using Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging.Consumers;

public abstract class BaseConsumer(
    IServiceProvider serviceProvider,
    ILogger logger,
    IConnectionFactory connectionFactory,
    string queue,
    ServerSettings settings,
    string expectedVersion = "v1") : BackgroundService
{
    protected abstract string HandlerName { get; }

    protected abstract Task HandlePayloadAsync(
        IServiceProvider scope,
        string routingKey,
        MessageEnvelope envelope,
        CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Запуск консьюмера {HandlerName} для очереди {Queue} сервиса {ServiceName}",
            HandlerName, queue, settings.ServiceName);

        var retryCount = 0;
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await using var conn = await connectionFactory.CreateConnectionAsync(stoppingToken);
                await using var channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);

                retryCount = 0;
                await StartConsumingAsync(channel, stoppingToken);
            }
            catch (Exception ex)
            {
                retryCount++;
                var baseDelay = Math.Min(Math.Pow(2, retryCount), 60);
                var jitter = random.NextDouble() * 0.1 * baseDelay;
                var delay = baseDelay + jitter;

                logger.LogWarning(
                    "Ошибка подключения к RabbitMQ (попытка {Attempt}): {Error}. Повтор через {Delay:F1}с",
                    retryCount, ex.Message, delay);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Консьюмер {HandlerName} остановлен во время ожидания", HandlerName);
                }
            }
    }

    private async Task StartConsumingAsync(IChannel channel, CancellationToken stoppingToken)
    {
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var sw = Stopwatch.StartNew();
            var body = Encoding.UTF8.GetString(ea.Body.Span);

            logger.LogInformation("Получено сообщение {RoutingKey} в {HandlerName}: {Body}",
                ea.RoutingKey, HandlerName, body);

            try
            {
                var envelope = MessageEnvelope.FromJson(body);
                ValidateEnvelope(envelope, ea.RoutingKey);
                await ProcessMessageAsync(channel, ea, envelope, sw, stoppingToken);
            }
            catch (JsonException jsonEx)
            {
                sw.Stop();
                logger.LogError("Ошибка парсинга JSON {RoutingKey}: {Body}, Error: {Error}", ea.RoutingKey, body, jsonEx.Message);
                await MoveToDeadLetterAsync(channel, ea, Guid.NewGuid(), body, jsonEx.Message, stoppingToken);
            }
            catch (InvalidOperationException validationEx)
            {
                sw.Stop();
                logger.LogError("Ошибка валидации {RoutingKey}: {Error}", ea.RoutingKey, validationEx.Message);
                var envelope = MessageEnvelope.FromJson(body);
                await MoveToDeadLetterAsync(channel, ea, envelope.EventId, body, validationEx.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError("Ошибка обработки {RoutingKey} за {LatencyMs}мс: {ErrorMessage}", ea.RoutingKey,
                    sw.ElapsedMilliseconds, ex.Message);
                var envelope = MessageEnvelope.FromJson(body);
                await MoveToDeadLetterAsync(channel, ea, envelope.EventId, body, ex.Message, stoppingToken);
            }
        };

        var consumerTag = await channel.BasicConsumeAsync(queue, false, consumer, stoppingToken);
        logger.LogInformation("Консьюмер {HandlerName} активен для {Queue} с тегом {ConsumerTag}", HandlerName, queue,
            consumerTag);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ValidateEnvelope(MessageEnvelope envelope, string routingKey)
    {
        if (envelope.Meta.Version != expectedVersion)
            throw new InvalidOperationException(
                $"Некорректная версия envelope {envelope.EventId}: {envelope.Meta.Version}, ожидалось {expectedVersion}. {routingKey}");
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        MessageEnvelope envelope,
        Stopwatch sw,
        CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();

        if (await inboxService.IsAlreadyConsumedAsync(envelope.EventId, HandlerName, ct))
        {
            sw.Stop();
            logger.LogWarning("Дубликат {EventId} {RoutingKey} за {LatencyMs}мс {CorrelationId}",
                envelope.EventId, ea.RoutingKey, sw.ElapsedMilliseconds, envelope.Meta.CorrelationId);

            await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            return;
        }

        const int maxRetries = 10;
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            attempt++;
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            try
            {
                await HandlePayloadAsync(scope.ServiceProvider, ea.RoutingKey, envelope, ct);

                await inboxService.MarkAsConsumedAsync(envelope.EventId, HandlerName, ct);
                await transaction.CommitAsync(ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);

                sw.Stop();
                logger.LogInformation(
                    "Обработано {EventId} {RoutingKey} {HandlerName} {ServiceName} за {LatencyMs}мс с попытки {Attempt} {CorrelationId}",
                    envelope.EventId, ea.RoutingKey, HandlerName, settings.ServiceName, sw.ElapsedMilliseconds, attempt,
                    envelope.Meta.CorrelationId);
                return;
            }
            catch (Exception handleEx)
            {
                await transaction.RollbackAsync(ct);
                lastException = handleEx;

                if (attempt < maxRetries)
                {
                    var delay = Math.Pow(2, attempt - 1);
                    logger.LogWarning(
                        "Ошибка обработки {EventId} {RoutingKey} попытка {Attempt}/{MaxRetries}, повтор через {Delay}с: {Error}",
                        envelope.EventId, ea.RoutingKey, attempt, maxRetries, delay, handleEx.Message);

                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }
            }
        }

        sw.Stop();
        logger.LogError(
            "Исчерпаны все попытки обработки {EventId} {RoutingKey} за {LatencyMs}мс {CorrelationId}: {Error}",
            envelope.EventId, ea.RoutingKey, sw.ElapsedMilliseconds, envelope.Meta.CorrelationId, lastException?.Message ?? "Неизвестная ошибка");

        throw lastException ?? new Exception("Неизвестная ошибка");
    }

    private async Task MoveToDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        Guid eventId,
        string body,
        string reason,
        CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();
        var headersJson =
            JsonSerializer.Serialize(ea.BasicProperties.Headers ?? new Dictionary<string, object>()!);

        await inboxService.MoveToDeadLetterAsync(eventId, ea.RoutingKey, body, headersJson, reason, ct);
        await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
    }
}