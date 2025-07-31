using System.Text.Json;
using AccountService.Common.Models;
using AccountService.Common.Models.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var authSettings = new AuthenticationSettings(configuration);
        services.AddSingleton(authSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{authSettings.AuthenticationServerUrl}/realms/{authSettings.Realm}";
                options.Audience = authSettings.Audience;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{authSettings.AuthenticationServerUrl}/realms/{authSettings.Realm}",
                    ValidateAudience = true,
                    ValidAudience = authSettings.Audience,
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