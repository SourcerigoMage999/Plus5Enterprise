using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Plus5.Api.Scheduling;
using Plus5.Application.Scheduling;

namespace Plus5.Api.Tests.Scheduling;

public sealed class ScheduleMaterializationWorkerTests
{
    [Fact]
    public async Task StartupRunsImmediateCatchUpAndCadenceIsSixHours()
    {
        var materializer = new SignallingMaterializer();
        var services = new ServiceCollection();
        services.AddSingleton<IScheduleMaterializationService>(materializer);
        await using var provider = services.BuildServiceProvider();
        using var worker = new ScheduleMaterializationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<ScheduleMaterializationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await materializer.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromHours(6), ScheduleMaterializationWorker.Cadence);
        Assert.Equal(1, materializer.CallCount);
    }

    private sealed class SignallingMaterializer : IScheduleMaterializationService
    {
        private int callCount;

        public TaskCompletionSource Invoked { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount => Volatile.Read(ref callCount);

        public Task<ScheduleMaterializationRunResult> RunAsync(
            string leaseOwnerId,
            CancellationToken cancellationToken)
        {
            Assert.False(string.IsNullOrWhiteSpace(leaseOwnerId));
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref callCount);
            Invoked.TrySetResult();
            return Task.FromResult(new ScheduleMaterializationRunResult(
                ScheduleMaterializationRunStatus.Completed,
                0,
                0,
                0,
                0,
                0,
                0));
        }
    }
}
