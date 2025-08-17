using AccountService.Common.Filters;
using AccountService.Common.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Common.Extensions.ProgramSetup;

public static class WebApiExtensions
{
    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddControllers(options => { options.Filters.Add<ValidationExceptionFilter>(); });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => string.Join("; ", e.Value!.Errors.Select(err => err.ErrorMessage))
                    );

                var response = ApiResult.BadRequest("Ошибка валидации", errors);
                return new BadRequestObjectResult(response.Value);
            };
        });

        services.AddEndpointsApiExplorer();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddHttpContextAccessor();

        return services;
    }
}
