using AccountService.Common.Extensions.ProgramSetup;
using AccountService.Infrastructure;
using AccountService.Infrastructure.Data;
using Messaging.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddEnvironmentVariables();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    Log.Information("Запуск AccountService");

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Host.UseSerilog();

    builder.Services
        .AddWebApi()
        .AddMediatRServices()
        .AddAutoMapperServices()
        .AddApiVersioningServices()
        .AddInfrastructure(builder.Configuration)
        .UseMessaging<ApplicationDbContext>(builder.Configuration, opt =>
        {
            opt.ServiceName = builder.Configuration["Messaging:ServiceName"]!;
            opt.Exchange = builder.Configuration["Messaging:Exchange"]!;
            opt.UseFullMessaging();
        })
        .AddCustomAuthentication(builder.Configuration)
        .AddCustomSwagger(builder.Configuration)
        .AddHangFire(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }

    app.UseCustomSwagger(builder.Configuration);

    app.UseMessaging();

    app.UseHttpsRedirection();
    app.UseCustomLogging();

    app.UseCors();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHealthChecks();
    app.MapControllers();

    app.UseHangFire();

    // Логи конфигурации
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:80";
    var environment = app.Environment.EnvironmentName;
    var authServerUrl = app.Configuration["Authentication:AuthenticationServerUrl"];
    var dbConnection = app.Configuration.GetConnectionString("DefaultConnection");
    var rabbitHost = app.Configuration["RabbitMq:Host"];
    var exchange = app.Configuration["Messaging:Exchange"];
    var serviceName = app.Configuration["Messaging:ServiceName"];
    var keycloakPort = app.Configuration["KeycloakPort"];

    Log.Information("=== AccountService Configuration ===");
    Log.Information("Environment: {Environment}", environment);
    Log.Information("URLs: {Urls}", urls);
    Log.Information("Auth Server: {AuthServer}", authServerUrl);
    Log.Information("Database: {Database}", dbConnection?.Split(';')[1]);
    Log.Information("RabbitMQ Host: {RabbitHost}", rabbitHost);
    Log.Information("Exchange: {Exchange}", exchange);
    Log.Information("Service Name: {ServiceName}", serviceName);
    Log.Information("Keycloak Port: {Keycloak}", keycloakPort);
    Log.Information("=== AccountService запущен и готов к работе! ===");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal("Приложение неожиданно завершилось: {Error}", ex.Message);
}
finally
{
    Log.CloseAndFlush();
}

namespace AccountService
{
    public class Program;
}