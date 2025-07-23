namespace AccountService.Common.Models.Domain.Results;

public class CommandResult<T>
{
    private CommandResult(bool isSuccess, T? data, CommandErrorResult? commandErrorResult)
    {
        IsSuccess = isSuccess;
        Data = data;
        CommandError = commandErrorResult;
    }

    public bool IsSuccess { get; }
    public T? Data { get; }
    public CommandErrorResult? CommandError { get; }

    public static CommandResult<T> Success(T data)
    {
        return new CommandResult<T>(true, data, null);
    }

    public static CommandResult<T> Success()
    {
        return new CommandResult<T>(true, default, null);
    }

    public static CommandResult<T> Failure(int statusCode, string message, Dictionary<string, string>? details = null)
    {
        return new CommandResult<T>(false, default, new CommandErrorResult(statusCode, message, details));
    }

    public class CommandErrorResult(int statusCode, string message, Dictionary<string, string>? details = null)
    {
        public int StatusCode { get; } = statusCode;
        public string Message { get; } = message;
        public Dictionary<string, string> Details { get; } = details ?? new Dictionary<string, string>();
    }
}