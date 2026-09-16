using Plus5.Domain.Scheduling;

namespace Plus5.Api.Tests.Scheduling;

public sealed class ScheduleMaterializationIssueTests
{
    [Fact]
    public void RepeatedObservationAndResolutionPreserveAuditState()
    {
        var first = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);
        var issue = new ScheduleMaterializationIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 22),
            ScheduleMaterializationIssueType.TeacherConflict,
            first);

        issue.RecordAgain(first.AddHours(6));
        var changed = issue.Resolve(first.AddHours(7));
        var repeatedResolution = issue.Resolve(first.AddHours(8));

        Assert.Equal(2, issue.AttemptCount);
        Assert.Equal(first, issue.FirstSeenAtUtc);
        Assert.Equal(first.AddHours(6), issue.LastSeenAtUtc);
        Assert.Equal(first.AddHours(7), issue.ResolvedAtUtc);
        Assert.True(changed);
        Assert.False(repeatedResolution);
    }

    [Fact]
    public void ResolvedIssueCanBecomeActiveAgainWithoutCreatingNewIdentity()
    {
        var first = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);
        var issue = new ScheduleMaterializationIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 22),
            ScheduleMaterializationIssueType.InvalidLocalTime,
            first);

        issue.Resolve(first.AddHours(1));
        issue.RecordAgain(first.AddHours(6));

        Assert.Equal(2, issue.AttemptCount);
        Assert.Null(issue.ResolvedAtUtc);
        Assert.Equal(first.AddHours(6), issue.LastSeenAtUtc);
    }
}
