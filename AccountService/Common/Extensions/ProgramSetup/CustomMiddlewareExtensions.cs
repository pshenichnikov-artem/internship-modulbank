using AccountService.Common.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class CustomMiddlewareExtensions
{
    public static WebApplication UseCustomLogging(this WebApplication app)
    {
        app.UsePerformanceLogging();
        app.UseGlobalExceptionHandler();

        return app;
    }

    public static WebApplication UseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        })
        .AllowAnonymous()
        .WithMetadata(new SwaggerOperationAttribute("Проверка жизнеспособности сервиса", "Возвращает статус работоспособности основных компонентов"));
        
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        })
        .AllowAnonymous()
        .WithMetadata(new SwaggerOperationAttribute("Проверка готовности сервиса", "Возвращает статус готовности к обработке запросов (RabbitMQ, Outbox)"));

        return app;
    }
}
