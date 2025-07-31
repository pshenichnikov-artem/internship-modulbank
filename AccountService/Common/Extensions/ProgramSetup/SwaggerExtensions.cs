using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = services.BuildServiceProvider()
            .GetRequiredService<IApiVersionDescriptionProvider>();

        var authenticationServerUrl = configuration["Authentication:AuthenticationServerUrl"];
        var realm = configuration["Authentication:Realm"] ?? "modulbank";

        services.AddSwaggerGen(options =>
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
                            new Uri($"{authenticationServerUrl}/realms/{realm}/protocol/openid-connect/auth"),
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

        return services;
    }

    public static WebApplication UseCustomSwagger(this WebApplication app, IConfiguration configuration)
    {
        if (!app.Environment.IsDevelopment()) return app;

        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Account API {description.GroupName.ToUpperInvariant()}");

            options.RoutePrefix = string.Empty;
            options.OAuthClientId("account-service");

            var baseUrl = configuration["urls"] ?? $"http://localhost:{configuration["ACCOUNT_SERVICE_PORT"] ?? "80"}";
            options.OAuth2RedirectUrl($"{baseUrl}/oauth2-redirect.html");
            options.OAuthAppName("Account Service API");
            options.OAuthUsePkce();
            options.OAuthScopeSeparator(" ");
            options.OAuthScopes("openid profile");
        });

        return app;
    }
}