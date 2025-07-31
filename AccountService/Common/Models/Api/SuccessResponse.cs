namespace AccountService.Common.Models.Api;

public sealed class SuccessResponse : ApiResponse
{
    public SuccessResponse(object? data)
    {
        Success = true;
        Data = data;
    }

    public object? Data { get; init; }
}

public sealed class SuccessResponse<T> : ApiResponse
{
    public SuccessResponse(T? data)
    {
        Success = true;
        Data = data;
    }

    public T? Data { get; init; }
}