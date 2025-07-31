using AccountService.Common.Interfaces.Service;

namespace AccountService.Infrastructure.Services;

public class ClientService: IClientService
{
    public async Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await Task.FromResult(true);
    }
}