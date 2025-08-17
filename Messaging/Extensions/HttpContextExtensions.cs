using Microsoft.AspNetCore.Http;

namespace Messaging.Extensions;

public static class HttpContextExtensions
{
    private const string CorrelationKey = "X-Correlation-Id";

    public static string? GetCorrelationId(this HttpContext? context)
    {
        if (context == null) return string.Empty;

        if (context.Items.TryGetValue(CorrelationKey, out var value) && value is string s && Guid.TryParse(s, out _))
            return s;

        if (context.Request.Headers.TryGetValue(CorrelationKey, out var header) && Guid.TryParse(header, out _))
            return header.ToString();

        return null;
    }

    public static string? GetCorrelationId(this IHttpContextAccessor accessor)
    {
        var context = accessor.HttpContext;
        if (context == null) return null;

        if (context.Items.TryGetValue(CorrelationKey, out var value) && value is string s && Guid.TryParse(s, out _))
            return s;

        if (context.Request.Headers.TryGetValue(CorrelationKey, out var header) && Guid.TryParse(header, out _))
            return header.ToString();

        return null;
    }
}
