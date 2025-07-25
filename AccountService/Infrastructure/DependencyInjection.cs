﻿using AccountService.Common.Interfaces.Repository;
using AccountService.Common.Interfaces.Service;
using AccountService.Features.Transactions;
using AccountService.Infrastructure.Data;
using AccountService.Infrastructure.Repositories;
using AccountService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("AccountServiceDb"));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddSingleton<IClientService, ClientService>();
        services.AddSingleton<ICurrencyService, CurrencyService>();
        services.AddScoped<IAccountService, Features.Accounts.AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();


        return services;
    }
}