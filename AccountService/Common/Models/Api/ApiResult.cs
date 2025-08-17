using AccountService.Common.Models.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Common.Models.Api;

public static class ApiResult
{
    public static ObjectResult Ok(object? data)
    {
        return new OkObjectResult(new SuccessResponse(data));
    }

    public static ObjectResult Created(string location, object? data)
    {
        return new CreatedResult(location, new SuccessResponse(data));
    }

    public static ObjectResult NotFound(string message)
    {
        return new NotFoundObjectResult(new ErrorResponse(404, message));
    }

    public static ObjectResult BadRequest(string message, Dictionary<string, string>? details = null)
    {
        return new BadRequestObjectResult(new ErrorResponse(400, message, details));
    }

    public static ObjectResult Conflict(string message)
    {
        return new ConflictObjectResult(new ErrorResponse(409, message));
    }

    public static ObjectResult Forbidden(string message)
    {
        return new ObjectResult(new ErrorResponse(403, message));
    }

    public static ObjectResult Unauthorized(string message)
    {
        return new UnauthorizedObjectResult(new ErrorResponse(401, message));
    }

    public static ObjectResult Problem(string message, Dictionary<string, string>? details = null)
    {
        return new ObjectResult(new ErrorResponse(500, message, details))
        {
            StatusCode = 500
        };
    }

    public static ObjectResult FromCommandResult<T>(CommandResult<T> result, string? createdAtActionName = null,
        object? routeValues = null)
    {
        if (!result.IsSuccess)
            return result.CommandError is null
                ? Problem("Операция завершилась неудачно, но информация об ошибке не была предоставлена.")
                : FromStatusCode(result.CommandError.StatusCode, result.CommandError.Message,
                    result.CommandError.Details);

        var data = result.Data;

        return createdAtActionName != null ? Created($"/api/{createdAtActionName}/{routeValues}", data) : Ok(data);
    }

    public static ObjectResult FromStatusCode(int statusCode, string message,
        Dictionary<string, string>? details = null)
    {
        return statusCode switch
        {
            200 => Ok(message),
            201 => Created(string.Empty, message),
            400 => BadRequest(message, details),
            401 => Unauthorized(message),
            404 => NotFound(message),
            403 => Forbidden(message),
            409 => Conflict(message),
            _ => new ObjectResult(new ErrorResponse(statusCode, message, details))
            {
                StatusCode = statusCode
            }
        };
    }
}
