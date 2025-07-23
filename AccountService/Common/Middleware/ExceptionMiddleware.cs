using System.Net;
using System.Text.Json;
using AccountService.Common.Models.Api;
using FluentValidation;

namespace AccountService.Common.Middleware;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => string.Join("\n", g.Select(e => e.ErrorMessage))
            );

        var response = ApiResult.BadRequest("Ошибка валидации", errors);

        await context.Response.WriteAsJsonAsync(response.Value);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
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