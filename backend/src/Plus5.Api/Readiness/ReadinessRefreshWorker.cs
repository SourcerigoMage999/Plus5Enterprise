using System.Diagnostics;
using Plus5.Application.Readiness;

namespace Plus5.Api.Readiness;

internal sealed partial class ReadinessRefreshWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<ReadinessRefreshWorker> logger) : BackgroundService
{
    internal const int LocalRunHour = 2;
    internal const string TimeZoneId = "Europe/Zagreb";
    private readonly string leaseOwnerId = CreateLeaseOwnerId();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(clock.GetUtcNow());
            await Task.Delay(delay, clock, stoppingToken);
            await RunOnceAsync(stoppingToken);
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        LogRunStarted(logger);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IReadinessRefreshService>();
            var result = await service.RunAsync(leaseOwnerId, cancellationToken);
            LogRunCompleted(
                logger,
                result.Status,
                result.StudentsRecalculated,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRunCancelled(logger, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
        catch (Exception exception)
        {
            LogRunFailed(logger, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, exception);
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTimeOffset nowUtc)
    {
        if (nowUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, zone);
        var localDate = localNow.TimeOfDay < TimeSpan.FromHours(LocalRunHour)
            ? localNow.Date
            : localNow.Date.AddDays(1);
        var candidate = DateTime.SpecifyKind(
            localDate.AddHours(LocalRunHour),
            DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(candidate))
        {
            candidate = candidate.AddHours(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(candidate, zone);
        return nextUtc - nowUtc.UtcDateTime;
    }

    private static string CreateLeaseOwnerId()
    {
        var machineName = Environment.MachineName;
        if (machineName.Length > 40)
        {
            machineName = machineName[..40];
        }

        return $"{machineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    [LoggerMessage(4600, LogLevel.Information, "Readiness refresh run started.")]
    private static partial void LogRunStarted(ILogger logger);

    [LoggerMessage(4601, LogLevel.Information,
        "Readiness refresh run completed with status {Status}: {StudentsRecalculated} students " +
        "recalculated in {DurationMilliseconds} ms.")]
    private static partial void LogRunCompleted(
        ILogger logger,
        ReadinessRefreshRunStatus status,
        int studentsRecalculated,
        double durationMilliseconds);

    [LoggerMessage(4602, LogLevel.Information,
        "Readiness refresh run was cancelled after {DurationMilliseconds} ms.")]
    private static partial void LogRunCancelled(ILogger logger, double durationMilliseconds);

    [LoggerMessage(4603, LogLevel.Error,
        "Readiness refresh run failed after {DurationMilliseconds} ms.")]
    private static partial void LogRunFailed(
        ILogger logger,
        double durationMilliseconds,
        Exception exception);
}
