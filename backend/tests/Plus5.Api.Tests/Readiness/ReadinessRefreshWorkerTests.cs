using Plus5.Api.Readiness;

namespace Plus5.Api.Tests.Readiness;

public sealed class ReadinessRefreshWorkerTests
{
    [Fact]
    public void SchedulesApproximatelyTwoAmEuropeZagrebAcrossStandardAndDstTime()
    {
        var winterDelay = ReadinessRefreshWorker.DelayUntilNextRun(
            new DateTimeOffset(2026, 1, 15, 0, 30, 0, TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromMinutes(30), winterDelay);

        var summerDelay = ReadinessRefreshWorker.DelayUntilNextRun(
            new DateTimeOffset(2026, 7, 15, 23, 30, 0, TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromMinutes(30), summerDelay);
    }
}
