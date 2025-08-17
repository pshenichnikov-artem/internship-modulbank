using Messaging.Exceptions;
using Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Dispatchers;

public sealed class OutboxDispatcherService(
    IServiceProvider serviceProvider,
    ILogger<OutboxDispatcherService> logger)
    : BackgroundService
{
    private int _connectionErrorCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<Guid> batch = [];
            var hasConnectionError = false;

            try
            {
                using var scope = serviceProvider.CreateScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

                batch = await outboxService.GetUnpublishedAsync(stoppingToken);

                foreach (var eventId in batch)
                {
                    var started = DateTime.UtcNow;
                    try
                    {
                        await outboxService.PublishAsync(eventId, stoppingToken);
                        var latency = (DateTime.UtcNow - started).TotalMilliseconds;

                        _connectionErrorCount = 0;

                        logger.LogInformation(
                            "Отправлено из Outbox {EventId} за {LatencyMs}мс",
                            eventId, latency);
                    }
                    catch (MessagingConnectionException ex)
                    {
                        var latency = (DateTime.UtcNow - started).TotalMilliseconds;
                        _connectionErrorCount++;
                        hasConnectionError = true;

                        logger.LogWarning(
                            "Ошибка подключения к брокеру {EventId} за {LatencyMs}мс (попытка {ErrorCount}): {ErrorMessage}",
                            eventId, latency, _connectionErrorCount, ex.Message);

                        break;
                    }
                    catch (Exception ex)
                    {
                        var latency = (DateTime.UtcNow - started).TotalMilliseconds;

                        logger.LogError(
                            "Ошибка отправки из Outbox {EventId} за {LatencyMs}мс тип_ошибки={ExceptionType}: {ErrorMessage}",
                            eventId, latency, ex.GetType().Name, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Ошибка в цикле Outbox dispatcher: {ErrorMessage}", ex.Message);
            }

            var delay = CalculateDelay(batch.Count, hasConnectionError);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private TimeSpan CalculateDelay(int batchCount, bool hasConnectionError)
    {
        if (!hasConnectionError) return batchCount > 0 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5);

        var backoffSeconds = Math.Min(5 * Math.Pow(2, _connectionErrorCount - 1), 60);
        return TimeSpan.FromSeconds(backoffSeconds);
    }
}