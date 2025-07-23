using System.Diagnostics;

namespace AccountService.Common.Middleware;

public class PerformanceLoggingMiddleware(
    RequestDelegate next,
    ILogger<PerformanceLoggingMiddleware> logger,
    int thresholdMs = 500)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var elapsedMs = sw.ElapsedMilliseconds;

            if (elapsedMs > thresholdMs)
                logger.LogWarning(
                    "Обнаружен медленный запрос: {Method} {Path} занял {ElapsedMs}ms (порог: {ThresholdMs}ms) ",
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs,
                    thresholdMs);
        }
    }
}

public static class PerformanceLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder builder, int thresholdMs = 500)
    {
        return builder.UseMiddleware<PerformanceLoggingMiddleware>(thresholdMs);
    }
}