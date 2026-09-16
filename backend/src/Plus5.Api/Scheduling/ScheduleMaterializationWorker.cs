using System.Diagnostics;
using Plus5.Application.Scheduling;

namespace Plus5.Api.Scheduling;

internal sealed partial class ScheduleMaterializationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<ScheduleMaterializationWorker> logger)
    : BackgroundService
{
    internal static readonly TimeSpan Cadence = TimeSpan.FromHours(6);
    private readonly string leaseOwnerId = CreateLeaseOwnerId();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(Cadence, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
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
            var materializer = scope.ServiceProvider
                .GetRequiredService<IScheduleMaterializationService>();
            var result = await materializer.RunAsync(leaseOwnerId, cancellationToken);
            LogRunCompleted(
                logger,
                result.Status,
                result.SeriesScanned,
                result.SessionsCreated,
                result.OccurrencesSkipped,
                result.ConflictsFound,
                result.IssuesRecorded,
                result.IssuesResolved,
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

    private static string CreateLeaseOwnerId()
    {
        var machineName = Environment.MachineName;
        if (machineName.Length > 40)
        {
            machineName = machineName[..40];
        }

        return $"{machineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    [LoggerMessage(4500, LogLevel.Information, "Schedule materialization run started.")]
    private static partial void LogRunStarted(ILogger logger);

    [LoggerMessage(4501, LogLevel.Information,
        "Schedule materialization run completed with status {Status}: {SeriesScanned} series scanned, "
        + "{SessionsCreated} sessions created, {OccurrencesSkipped} occurrences skipped, "
        + "{ConflictsFound} conflicts found, {IssuesRecorded} issues recorded, "
        + "{IssuesResolved} issues resolved in {DurationMilliseconds} ms.")]
    private static partial void LogRunCompleted(
        ILogger logger,
        ScheduleMaterializationRunStatus status,
        int seriesScanned,
        int sessionsCreated,
        int occurrencesSkipped,
        int conflictsFound,
        int issuesRecorded,
        int issuesResolved,
        double durationMilliseconds);

    [LoggerMessage(4502, LogLevel.Information,
        "Schedule materialization run was cancelled after {DurationMilliseconds} ms.")]
    private static partial void LogRunCancelled(ILogger logger, double durationMilliseconds);

    [LoggerMessage(4503, LogLevel.Error,
        "Schedule materialization run failed after {DurationMilliseconds} ms.")]
    private static partial void LogRunFailed(
        ILogger logger,
        double durationMilliseconds,
        Exception exception);
}
