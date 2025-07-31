using AccountService.Common.Middleware;
using Serilog;
using Serilog.Events;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class MiddlewareExtensions
{
    public static WebApplication UseCustomMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UsePerformanceLogging();
        app.UseGlobalExceptionHandler();

        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value!);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                if (httpContext.Request.QueryString.HasValue)
                    diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value);
            };

            options.GetLevel = (httpContext, _, ex) =>
            {
                if (httpContext.Response.StatusCode >= 500 || ex != null)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                return LogEventLevel.Fatal + 1;
            };
        });

        app.UseCors();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}