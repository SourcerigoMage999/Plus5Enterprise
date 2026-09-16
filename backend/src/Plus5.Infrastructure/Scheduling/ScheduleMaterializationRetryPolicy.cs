using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Plus5.Infrastructure.Scheduling;

internal static class ScheduleMaterializationRetryPolicy
{
    public const int MaxAttempts = 3;

    public static TimeSpan DelayAfter(int completedAttempt) =>
        TimeSpan.FromMilliseconds(250 * Math.Pow(2, completedAttempt - 1));

    public static bool IsTransient(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            return true;
        }

        if (exception is SqlException sqlException
            && sqlException.Number is -2 or 2 or 53 or 1205 or 233 or 2601 or 2627
                or 40197 or 40501 or 4060 or 40613 or 10053 or 10054 or 10060 or 10928 or 10929
                or 49918 or 49919 or 49920)
        {
            return true;
        }

        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }

    public static bool IsDatabaseUnavailable(Exception exception)
    {
        if (exception is SqlException sqlException
            && sqlException.Number is -2 or 2 or 53 or 233 or 40197 or 40501 or 4060 or 40613
                or 10053 or 10054 or 10060 or 10928 or 10929 or 49918 or 49919 or 49920)
        {
            return true;
        }

        return exception.InnerException is not null && IsDatabaseUnavailable(exception.InnerException);
    }
}
