using System.Net;
using System.Text.Json;
using AccountService.Common.Models.Api;

namespace AccountService.Common.Middleware;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            LogException(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private void LogException(HttpContext context, Exception exception)
    {
        var request = context.Request;
        logger.LogError(
            exception,
            "Необработанное исключение при обработке запроса {Method} {Path}{QueryString}. TraceId: {TraceId}",
            request.Method,
            request.Path,
            request.QueryString,
            context.TraceIdentifier);
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorDetails = new Dictionary<string, string>
        {
            { "exception", exception.GetType().Name },
            { "message", exception.Message }
        };
        var response = ApiResult.Problem("Внутренняя ошибка сервера", errorDetails);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}