using AccountService.Common.Interfaces.Jobs;
using AccountService.Features.Accounts.Jobs;
using Hangfire;
using Hangfire.PostgreSql;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class HangFireExtensions
{
    public static IServiceCollection AddHangFire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .UsePostgreSqlStorage(opt =>
                opt.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();
        services.AddScoped<IAccrueInterestJob, AccrueInterestJob>();

        return services;
    }

    public static IApplicationBuilder UseHangFire(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard();

        RecurringJob.AddOrUpdate<IAccrueInterestJob>(
            "daily-interest-accrual",
            job => job.ExecuteAsync(),
            Cron.Daily(2)); // Выполнение в 2:00 каждый день

        return app;
    }
}