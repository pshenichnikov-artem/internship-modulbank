using AccountService.Common.Behaviors;
using AccountService.Common.Middleware;
using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Models;
using AccountService.Infrastructure;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

app.UseGlobalExceptionHandler();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();