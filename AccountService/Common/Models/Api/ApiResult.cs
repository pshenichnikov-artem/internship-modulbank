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
        if (result.IsSuccess)
        {
            var data = result.Data;

            if (createdAtActionName != null)
                return Created($"/api/{createdAtActionName}/{routeValues}", data);

            return Ok(data);
        }

        if (result.CommandError is null)
            return Problem("Operation failed but no error information was provided.");

        return FromStatusCode(result.CommandError.StatusCode, result.CommandError.Message, result.CommandError.Details);
    }

    public static ObjectResult FromStatusCode(int statusCode, string message,
        Dictionary<string, string>? details = null)
    {
        return statusCode switch
        {
            200 => Ok(message),
            201 => Created(string.Empty, message),
            400 => BadRequest(message, details),
            404 => NotFound(message),
            409 => Conflict(message),
            _ => new ObjectResult(new ErrorResponse(statusCode, message, details))
            {
                StatusCode = statusCode
            }
        };
    }
}