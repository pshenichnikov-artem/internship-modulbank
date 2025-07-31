using System.Net.Http.Headers;
using System.Text.Json;
using AccountService.Common.Interfaces.Service;
using AccountService.Common.Models;

namespace AccountService.Infrastructure.Services;

public class ClientService(HttpClient httpClient, AuthenticationSettings authSettings) : IClientService
{
    public async Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var token = await GetAdminAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            return false;

        var url = $"{authSettings.AuthenticationServerUrl}/admin/realms/{authSettings.Realm}/users/{clientId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = authSettings.AdminUsername,
            ["password"] = authSettings.AdminPassword
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{authSettings.AuthenticationServerUrl}/realms/master/protocol/openid-connect/token");
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch
        {
            return null;
        }
    }
}