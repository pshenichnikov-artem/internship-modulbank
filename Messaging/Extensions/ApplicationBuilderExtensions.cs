using Messaging.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Messaging.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMessaging(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationMiddleware>();

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}