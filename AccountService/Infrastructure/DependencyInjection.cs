using AccountService.Common.Interfaces;
using AccountService.Infrastructure.Storage;

namespace AccountService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IAccountRepository, InMemoryAccountStorage>();
            services.AddScoped<ITransactionRepository, InMemoryTransactionStorage>();

            return services;
        }
    }
}
