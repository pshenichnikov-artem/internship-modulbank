using AccountService.Common.Extensions.ProgramSetup;
using AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        // ReSharper disable once StringLiteralTypo Название файла
        .AddJsonFile("appsettings.json", false, true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Запуск AccountService");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddCustomAuthentication(builder.Configuration);
    builder.Services.AddCustomSwagger(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }

    app.UseCustomSwagger(builder.Configuration);
    app.UseCustomMiddleware();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение неожиданно завершилось");
}
finally
{
    Log.CloseAndFlush();
}