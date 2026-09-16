using Plus5.Domain.Scheduling;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

internal static class ScheduleMaterializationIssueTracker
{
    public static ScheduleMaterializationIssueOutcome ApplyOutcome(
        Plus5DbContext db,
        Guid seriesId,
        DateOnly date,
        IReadOnlyCollection<ScheduleMaterializationIssueType> activeTypes,
        DateTimeOffset now,
        List<ScheduleMaterializationIssue> loadedIssues)
    {
        var recorded = 0;
        var resolved = 0;
        var active = activeTypes.ToHashSet();
        var forDate = loadedIssues.Where(issue => issue.OccurrenceLocalDate == date).ToArray();

        foreach (var issue in forDate.Where(issue => !active.Contains(issue.IssueType)))
        {
            if (issue.Resolve(now))
            {
                resolved++;
            }
        }

        foreach (var issueType in active)
        {
            var issue = forDate.SingleOrDefault(item => item.IssueType == issueType);
            if (issue is null)
            {
                issue = new ScheduleMaterializationIssue(Guid.NewGuid(), seriesId, date, issueType, now);
                db.ScheduleMaterializationIssues.Add(issue);
                loadedIssues.Add(issue);
            }
            else
            {
                issue.RecordAgain(now);
            }

            recorded++;
        }

        return new ScheduleMaterializationIssueOutcome(recorded, resolved);
    }
}

internal sealed record ScheduleMaterializationIssueOutcome(int Recorded, int Resolved);
