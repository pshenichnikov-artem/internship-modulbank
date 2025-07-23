namespace AccountService.Common.Interfaces.Service;

public interface IClientService
{
    Task<bool> VerifyClientExistsAsync(Guid clientId);
}