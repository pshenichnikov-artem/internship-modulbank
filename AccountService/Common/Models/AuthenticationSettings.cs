namespace AccountService.Common.Models;

public class AuthenticationSettings(IConfiguration configuration)
{
    public string AuthenticationServerUrl { get; } = configuration["Authentication:AuthenticationServerUrl"]
                                                     ?? throw new InvalidOperationException(
                                                         "Настройка 'Authentication:AuthenticationServerUrl' не задана в конфигурации.");

    public string Audience { get; } = configuration["Authentication:Audience"]
                                      ?? throw new InvalidOperationException(
                                          "Настройка 'Authentication:Audience' не задана в конфигурации.");

    public string Realm { get; } = configuration["Authentication:Realm"] ?? "modulbank";

    public string AdminUsername { get; } = configuration["Authentication:AdminUsername"]
                                           ?? throw new InvalidOperationException(
                                               "Настройка 'Authentication:AdminUsername' не задана в конфигурации.");

    public string AdminPassword { get; } = configuration["Authentication:AdminPassword"]
                                           ?? throw new InvalidOperationException(
                                               "Настройка 'Authentication:AdminPassword' не задана в конфигурации.");
}
