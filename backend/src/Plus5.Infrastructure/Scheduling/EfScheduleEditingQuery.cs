using Microsoft.EntityFrameworkCore;
using Plus5.Application.Scheduling;
using Plus5.Domain.Scheduling;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed class EfScheduleEditingQuery(
    Plus5DbContext db,
    IScheduleSessionDetailQuery detailQuery) : IScheduleEditingQuery
{
    public async Task<ScheduleEditItem?> GetAsync(
        Guid owner,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(owner, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);

        var edit = await db.Sessions.AsNoTracking()
            .Where(session => session.TeacherAccountId == owner && session.Id == sessionId)
            .Select(session => new
            {
                session.OnlineMeetingUrl,
                session.RowVersion,
                session.Status,
                session.RecurringSessionSeriesId,
                session.SeriesOccurrenceDate,
                session.IsSeriesException,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (edit is null)
        {
            return null;
        }

        var detail = await detailQuery.GetAsync(owner, sessionId, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var activeSeries = edit.RecurringSessionSeriesId.HasValue
            && await db.RecurringSessionSeries.AsNoTracking().AnyAsync(
                series => series.TeacherAccountId == owner
                    && series.Id == edit.RecurringSessionSeriesId
                    && series.SupersededAtUtc == null
                    && (!series.EndsOn.HasValue || edit.SeriesOccurrenceDate <= series.EndsOn),
                cancellationToken);

        return new(
            detail,
            edit.OnlineMeetingUrl,
            edit.RowVersion,
            edit.Status == SessionStatus.Scheduled
                && edit.SeriesOccurrenceDate.HasValue
                && !edit.IsSeriesException
                && activeSeries);
    }
}
