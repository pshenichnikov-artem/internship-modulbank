using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Messaging.HealthChecks;

public sealed class RabbitMqLiveHealthCheck(IConnectionFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await using var connection = await factory.CreateConnectionAsync(cts.Token);
            return HealthCheckResult.Healthy("Подключение к RabbitMQ доступно");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ошибка подключения к RabbitMQ", ex);
        }
    }
}

public sealed class RabbitMqHealthCheck(IConnectionFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await using var connection = await factory.CreateConnectionAsync(cts.Token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cts.Token);
            return HealthCheckResult.Healthy("Подключение к RabbitMQ успешно");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ошибка подключения к RabbitMQ", ex);
        }
    }
}