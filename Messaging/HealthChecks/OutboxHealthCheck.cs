using Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Messaging.HealthChecks;

public sealed class OutboxLiveHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
            await dbContext.OutboxMessages.AnyAsync(ct);
            return HealthCheckResult.Healthy("Таблица Outbox доступна");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Таблица Outbox недоступна", ex);
        }
    }
}

public sealed class OutboxHealthCheck(IServiceProvider serviceProvider, IConfiguration config)
    : IHealthCheck
{
    private readonly int _threshold = config.GetValue("Health:OutboxWarnThreshold", 100);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IMessagingDbContext>();
        var count = await dbContext.OutboxMessages.CountAsync(x => x.PublishedAtUtc == null, ct);

        return count > _threshold
            ? HealthCheckResult.Degraded($"Очередь Outbox: {count} сообщений")
            : HealthCheckResult.Healthy($"Очередь Outbox: {count} сообщений");
    }
}