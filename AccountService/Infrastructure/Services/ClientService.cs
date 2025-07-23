using AccountService.Common.Interfaces.Service;

namespace AccountService.Infrastructure.Services;

public class ClientService : IClientService
{
    public Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}