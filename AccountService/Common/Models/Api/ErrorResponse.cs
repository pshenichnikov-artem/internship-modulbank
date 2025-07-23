namespace AccountService.Common.Models.Api;

public sealed class ErrorResponse : ApiResponse
{
    public ErrorResponse(int statusCode, string message, Dictionary<string, string>? details = null)
    {
        Success = false;
        StatusCode = statusCode;
        ErrorMessage = message;
        Details = details ?? new Dictionary<string, string>();
    }

    public int StatusCode { get; init; }
    public string ErrorMessage { get; init; }
    public Dictionary<string, string> Details { get; init; }
}