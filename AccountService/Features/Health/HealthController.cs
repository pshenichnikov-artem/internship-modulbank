using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Health;

[ApiController]
[Route("api/health")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Проверка жизнеспособности сервиса
    /// </summary>
    /// <returns>Статус работоспособности основных компонентов</returns>
    [HttpGet("live")]
    [SwaggerOperation(Summary = "Проверка жизнеспособности сервиса")]
    [SwaggerResponse(StatusCodes.Status200OK, "Сервис работает")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Сервис недоступен")]
    public async Task<IActionResult> Live(CancellationToken ct)
    {
        var result = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("live"), ct);
        var response = CreateHealthResponse(result);
        return result.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Проверка готовности сервиса
    /// </summary>
    /// <returns>Статус готовности к обработке запросов (RabbitMQ, Outbox)</returns>
    [HttpGet("ready")]
    [SwaggerOperation(Summary = "Проверка готовности сервиса")]
    [SwaggerResponse(StatusCodes.Status200OK, "Сервис готов")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Сервис не готов")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"), ct);
        var response = CreateHealthResponse(result);
        return result.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }

    private static object CreateHealthResponse(HealthReport report)
    {
        var entries = report.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp => new
            {
                description = kvp.Value.Description,
                status = kvp.Value.Status.ToString(),
                exception = kvp.Value.Exception?.Message
            }
        );

        return new
        {
            status = report.Status.ToString(),
            entries
        };
    }
}