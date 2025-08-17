using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Models;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddAutoMapperServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile(new AccountMapperProfile());
            cfg.AddProfile(new TransactionMapperProfile());
        });
        
        return services;
    }
}
