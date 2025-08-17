using Microsoft.AspNetCore.Http;

namespace Messaging.Middleware;

internal class CorrelationMiddleware(RequestDelegate next)
{
    private const string Correlation = "X-Correlation-Id";
    private const string Causation = "X-Causation-Id";

    // ReSharper disable once UnusedMember.Global зарегестрирован как middelware
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue(Correlation, out var value) && value is string s &&
                            Guid.TryParse(s, out _)
            ? value.ToString()
            : string.Empty;

        var causationId =
            context.Items.TryGetValue(Causation, out var causationValue) && value is string s2 &&
            Guid.TryParse(s2, out _)
                ? causationValue.ToString()
                : string.Empty;

        if (!string.IsNullOrEmpty(correlationId))
            context.Items[Correlation] = correlationId;
        if (!string.IsNullOrEmpty(causationId))
            context.Items[Causation] = causationId;

        await next(context);

        if (!string.IsNullOrWhiteSpace(correlationId) && !context.Response.Headers.ContainsKey(Correlation))
            context.Response.Headers[Correlation] = correlationId;

        if (!string.IsNullOrWhiteSpace(causationId) && !context.Response.Headers.ContainsKey(Causation))
            context.Response.Headers[Causation] = causationId;
    }
}
