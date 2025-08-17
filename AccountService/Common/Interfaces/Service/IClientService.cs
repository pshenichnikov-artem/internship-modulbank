namespace AccountService.Common.Interfaces.Service;

public interface IClientService
{
    Task<bool> IsClientExistsAsync(Guid clientId, CancellationToken ct);
}
