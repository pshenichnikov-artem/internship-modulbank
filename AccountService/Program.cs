using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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

    var authenticationServerUrl = builder.Configuration["Authentication:AuthenticationServerUrl"];
    var audience = builder.Configuration["Authentication:Audience"];

    if (string.IsNullOrWhiteSpace(authenticationServerUrl))
        throw new InvalidOperationException(
            "Настройка 'Authentication:AuthenticationServerUrl' не задана в конфигурации.");
    if (string.IsNullOrWhiteSpace(audience))
        throw new InvalidOperationException("Настройка 'Authentication:Audience' не задана в конфигурации.");

    builder.Services.AddSwaggerGen(options =>
    {
        foreach (var description in provider.ApiVersionDescriptions)
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Version = description.GroupName,
                Title = $"Account Service API {description.GroupName.ToUpperInvariant()}",
                Description = "API для работы со счетами"
            });


        options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

        options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow
                {
                    AuthorizationUrl =
                        new Uri($"{authenticationServerUrl}/realms/modulbank/protocol/openid-connect/auth"),
                    Scopes = new Dictionary<string, string> { { "openid", "openid" }, { "profile", "profile" } }
                }
            }
        });

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Keycloak" },
                    In = ParameterLocation.Header,
                    Name = "Bearer",
                    Scheme = "Bearer"
                },
                []
            }
        };

        options.AddSecurityRequirement(securityRequirement);
        options.EnableAnnotations();
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "http://localhost:8080/realms/modulbank";
        options.Audience = "account";
        options.RequireHttpsMetadata = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogDebug("OnMessageReceived: Token = {Token}", context.Token ?? "(null)");
                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogInformation("OnTokenValidated: User {User}",
                        context.Principal.Identity?.Name ?? "(anonymous)");
                    return Task.CompletedTask;
                },

                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");

                    logger.LogError(context.Exception, "OnAuthenticationFailed: Ошибка аутентификации");

                    // Лог токена
                    if (!string.IsNullOrEmpty(context.Request.Headers.Authorization))
                    {
                        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                        logger.LogWarning("Получен токен: {Token}", token);

                        try
                        {
                            // Попробуем распарсить JWT без валидации
                            var handler = new JwtSecurityTokenHandler();
                            if (handler.CanReadToken(token))
                            {
                                var jwt = handler.ReadJwtToken(token);

                                logger.LogInformation("Parsed JWT Claims:");
                                foreach (var claim in jwt.Claims)
                                    logger.LogInformation("  {Type}: {Value}", claim.Type, claim.Value);

                                logger.LogInformation("JWT Header:");
                                foreach (var kvp in jwt.Header)
                                    logger.LogInformation("  {Key}: {Value}", kvp.Key, kvp.Value);

                                logger.LogInformation("JWT Issuer: {Issuer}", jwt.Issuer);
                                logger.LogInformation("JWT Audience: {Audience}", string.Join(", ", jwt.Audiences));
                                logger.LogInformation("JWT ValidFrom: {ValidFrom}", jwt.ValidFrom);
                                logger.LogInformation("JWT ValidTo: {ValidTo}", jwt.ValidTo);
                            }
                            else
                            {
                                logger.LogWarning("Не удалось прочитать токен JWT");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Ошибка при разборе JWT токена вручную");
                        }
                    }
                    else
                    {
                        logger.LogWarning("Заголовок Authorization отсутствует или пуст");
                    }

                    // Лог текущих настроек
                    var options = context.Options;
                    logger.LogInformation("JWT Settings:");
                    logger.LogInformation("  Authority: {Authority}", options.Authority);
                    logger.LogInformation("  Audience: {Audience}", options.Audience);
                    logger.LogInformation("  MetadataAddress: {MetadataAddress}", options.MetadataAddress);
                    logger.LogInformation("  RequireHttpsMetadata: {RequireHttpsMetadata}",
                        options.RequireHttpsMetadata);

                    return Task.CompletedTask;
                },

                OnChallenge = async context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning("OnChallenge: Аутентификация не прошла, статус {StatusCode}",
                        context.Response.StatusCode);

                    context.HandleResponse();

                    var result = ApiResult.Unauthorized("Требуется аутентификация");
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var json = JsonSerializer.Serialize(result.Value);
                    await context.Response.WriteAsync(json);
                }
            };
        });

    builder.Services.AddAuthorization();

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

            options.OAuthClientId("account-service");
            options.OAuth2RedirectUrl("http://localhost:80/oauth2-redirect.html");
            options.OAuthAppName("Account Service API");

            options.OAuthUsePkce();

            options.OAuthScopeSeparator(" ");
            options.OAuthScopes("openid profile");
        });
    }

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


    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

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