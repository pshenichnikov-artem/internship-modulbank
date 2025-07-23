using AccountService.Common.Interfaces.Service;

namespace AccountService.Infrastructure.Services;

public class ClientService : IClientService
{
    public Task<bool> VerifyClientExistsAsync(Guid clientId)
    {
        return Task.FromResult(true);
    }
}