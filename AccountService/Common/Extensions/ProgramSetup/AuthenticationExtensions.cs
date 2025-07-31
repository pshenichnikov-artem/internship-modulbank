using System.Text.Json;
using AccountService.Common.Models.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var authenticationServerUrl = configuration["Authentication:AuthenticationServerUrl"];
        var audience = configuration["Authentication:Audience"];
        var realm = configuration["Authentication:Realm"] ?? "modulbank";

        if (string.IsNullOrWhiteSpace(authenticationServerUrl))
            throw new InvalidOperationException(
                "Настройка 'Authentication:AuthenticationServerUrl' не задана в конфигурации.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Настройка 'Authentication:Audience' не задана в конфигурации.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{authenticationServerUrl}/realms/{realm}";
                options.Audience = audience;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{authenticationServerUrl}/realms/{realm}",
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var result = ApiResult.Unauthorized("Требуется аутентификация");
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var json = JsonSerializer.Serialize(result.Value);
                        await context.Response.WriteAsync(json);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}