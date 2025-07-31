using AccountService.Common.Behaviors;
using AccountService.Common.Filters;
using AccountService.Common.Middleware;
using AccountService.Common.Models.Api;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Запуск AccountService");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddControllers(options => { options.Filters.Add<ValidationExceptionFilter>(); });
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => string.Join("; ", e.Value!.Errors.Select(err => err.ErrorMessage))
                );

            var response = ApiResult.BadRequest("Ошибка валидации", errors);
            return new BadRequestObjectResult(response.Value);
        };
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile(new AccountMapperProfile());
        cfg.AddProfile(new TransactionMapperProfile());
    });

    builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Version"),
                new MediaTypeApiVersionReader("X-Version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>() ?? throw new NullReferenceException();

    builder.Services.AddSwaggerGen(options =>
    {
        foreach (var description in provider.ApiVersionDescriptions)
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Version = description.GroupName,
                Title = $"Account Service API {description.GroupName.ToUpperInvariant()}",
                Description = "API для работы со счетами"
            });

        options.EnableAnnotations();
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAny", policy =>
        {
            policy.WithOrigins("*")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddInfrastructure();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Account API {description.GroupName.ToUpperInvariant()}");

            options.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();

    app.UsePerformanceLogging();
    app.UseGlobalExceptionHandler();

    app.UseCors("AllowAny");

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


    app.UseRouting();

    app.MapControllers();

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