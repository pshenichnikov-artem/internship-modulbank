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