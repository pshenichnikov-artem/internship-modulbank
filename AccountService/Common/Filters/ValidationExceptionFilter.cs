using AccountService.Common.Models.Api;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccountService.Common.Filters;

public class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException ex) return;

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(e => e.ErrorMessage)));

        context.Result = ApiResult.BadRequest("Ошибка валидации", errors);
        context.ExceptionHandled = true;
    }
}