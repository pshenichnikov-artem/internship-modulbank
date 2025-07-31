using AccountService.Common.Extensions.ProgramSetup;
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

    builder.Services.AddApplicationServices();
    builder.Services.AddCustomAuthentication(builder.Configuration);
    builder.Services.AddCustomSwagger(builder.Configuration);

    var app = builder.Build();

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