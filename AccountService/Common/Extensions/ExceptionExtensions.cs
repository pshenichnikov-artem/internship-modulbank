using Npgsql;

namespace AccountService.Common.Extensions;

public static class ExceptionExtensions
{
    public static bool IsConcurrencyException(this Exception? ex)
    {
        while (ex != null)
        {
            if (ex is PostgresException { SqlState: "40P01" or "40001" }) return true;
            ex = ex.InnerException;
        }
        return false;
    }
}
