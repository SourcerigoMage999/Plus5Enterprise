using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Plus5.Application.LessonSessions;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.LessonSessions;
using Plus5.Domain.Scheduling;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonSessions;

public sealed class EfLessonSessionService(Plus5DbContext db) : ILessonSessionService
{
    private const int MaximumActiveSessions = 20;

    public async Task<LessonSessionResult> StartAsync(Guid teacherId, Guid lessonPlanId,
        Guid lessonPlanVersionId, Guid? calendarSessionId, Guid operationId,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty
            || lessonPlanVersionId == Guid.Empty || operationId == Guid.Empty
            || calendarSessionId == Guid.Empty)
            return Failure(LessonSessionFailure.Invalid);

        var requestHash = Hash($"START|{lessonPlanId:D}|{lessonPlanVersionId:D}|{calendarSessionId?.ToString("D") ?? "NONE"}");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await AcquireLockAsync($"LessonSessionOperation:{teacherId:D}:{operationId:D}", cancellationToken);
            var priorOperation = await db.LessonSessionOperations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.TeacherAccountId == teacherId
                    && item.OperationId == operationId, cancellationToken);
            if (priorOperation is not null)
            {
                if (!string.Equals(priorOperation.RequestHash, requestHash, StringComparison.Ordinal))
                    return Failure(LessonSessionFailure.OperationMismatch);
                return new(await BuildViewAsync(teacherId, priorOperation.LessonSessionId, cancellationToken),
                    LessonSessionFailure.None);
            }

            var plan = await db.LessonPlans.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == lessonPlanId && item.TeacherAccountId == teacherId
                && item.ArchivedAtUtc == null, cancellationToken);
            if (plan is null) return Failure(LessonSessionFailure.NotFound);
            if (plan.CurrentReadyVersionId != lessonPlanVersionId)
                return Failure(LessonSessionFailure.NotReady);
            var version = await db.LessonPlanVersions.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == lessonPlanVersionId && item.LessonPlanId == lessonPlanId
                && item.Status == LessonPlanVersionStatus.Ready, cancellationToken);
            if (version is null) return Failure(LessonSessionFailure.NotReady);

            var contextResource = calendarSessionId.HasValue
                ? $"LessonSessionCalendar:{calendarSessionId.Value:D}"
                : $"LessonSessionUnscheduled:{teacherId:D}:{lessonPlanId:D}:{plan.StudentId?.ToString("D") ?? plan.GroupId!.Value.ToString("D")}";
            await AcquireLockAsync(contextResource, cancellationToken);

            Session? calendar = null;
            if (calendarSessionId.HasValue)
            {
                calendar = await db.Sessions.SingleOrDefaultAsync(item => item.Id == calendarSessionId
                    && item.TeacherAccountId == teacherId, cancellationToken);
                if (calendar is null) return Failure(LessonSessionFailure.NotFound);
                var sameParticipant = plan.StudentId.HasValue
                    ? calendar.StudentId == plan.StudentId && calendar.GroupId is null
                    : calendar.GroupId == plan.GroupId && calendar.StudentId is null;
                if (!sameParticipant) return Failure(LessonSessionFailure.CalendarUnavailable);

                if (calendar.Status == SessionStatus.InProgress)
                {
                    var existing = await db.LessonSessions.AsNoTracking().SingleOrDefaultAsync(item =>
                        item.CalendarSessionId == calendar.Id && item.Status != LessonSessionStatus.Voided,
                        cancellationToken);
                    if (existing is null) return Failure(LessonSessionFailure.CalendarUnavailable);
                    var now = DateTimeOffset.UtcNow;
                    db.Add(new LessonSessionOperation(Guid.NewGuid(), teacherId, existing.Id,
                        operationId, LessonSessionOperationType.Start, requestHash, 0,
                        existing.Revision, now));
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    db.ChangeTracker.Clear();
                    return new(await BuildViewAsync(teacherId, existing.Id, cancellationToken),
                        LessonSessionFailure.None);
                }
                if (calendar.Status != SessionStatus.Scheduled)
                    return Failure(LessonSessionFailure.CalendarUnavailable);
            }
            else
            {
                var activeExists = await db.LessonSessions.AsNoTracking().AnyAsync(item =>
                    item.TeacherAccountId == teacherId && item.LessonPlanId == lessonPlanId
                    && item.CalendarSessionId == null && item.Status == LessonSessionStatus.InProgress
                    && item.StudentId == plan.StudentId && item.GroupId == plan.GroupId,
                    cancellationToken);
                if (activeExists) return Failure(LessonSessionFailure.ActiveConflict);
            }

            var sourceActivities = await db.LessonActivities.AsNoTracking()
                .Where(item => item.LessonPlanVersionId == version.Id)
                .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
            if (sourceActivities.Count == 0) return Failure(LessonSessionFailure.NotReady);
            var sourceActivityIds = sourceActivities.Select(item => item.Id).ToArray();
            var sourceMaterials = await db.LessonActivityMaterialVersions.AsNoTracking()
                .Where(item => sourceActivityIds.Contains(item.LessonActivityId))
                .ToListAsync(cancellationToken);

            var nowUtc = DateTimeOffset.UtcNow;
            var lessonSession = new LessonSession(Guid.NewGuid(), teacherId, plan.Id, version.Id,
                calendar?.Id, plan.StudentId, plan.GroupId, nowUtc, calendar?.StartsAtUtc,
                calendar?.EndsAtUtc, calendar?.TimeZoneId);
            db.Add(lessonSession);
            foreach (var source in sourceActivities)
            {
                var runtime = new LessonSessionActivity(Guid.NewGuid(), lessonSession.Id,
                    source.Id, LessonSessionActivityOrigin.Planned, source.SortOrder,
                    source.KnowledgeBlockVersionId, source.Name, source.Purpose,
                    source.DurationMinutes, source.WorkMode, source.Description,
                    source.PurposeDescription, source.TeacherInstructions,
                    source.StudentInstructions, source.Notes,
                    source.ShowTeacherInstructionsOnBoard,
                    source.ShowStudentInstructionsOnBoard, source.ShowProgressOnBoard);
                db.Add(runtime);
                db.AddRange(sourceMaterials.Where(item => item.LessonActivityId == source.Id)
                    .Select(item => new LessonSessionActivityMaterialVersion(runtime.Id,
                        item.MaterialVersionId)));
            }
            db.Add(new LessonSessionOperation(Guid.NewGuid(), teacherId, lessonSession.Id,
                operationId, LessonSessionOperationType.Start, requestHash, 0, 1, nowUtc));
            db.Add(new LessonSessionAuditEvent(Guid.NewGuid(), lessonSession.Id, teacherId,
                operationId, "STARTED", null, LessonSessionStatus.InProgress, nowUtc));
            calendar?.Start(nowUtc);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return new(await BuildViewAsync(teacherId, lessonSession.Id, cancellationToken),
                LessonSessionFailure.None, true);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51010 or 51011)
        {
            return Failure(LessonSessionFailure.ActiveConflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(LessonSessionFailure.ConcurrencyConflict);
        }
    }

    public async Task<IReadOnlyList<ActiveLessonSessionView>> GetActiveAsync(Guid teacherId,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty) return [];
        var sessions = await db.LessonSessions.AsNoTracking()
            .Where(item => item.TeacherAccountId == teacherId
                && item.Status == LessonSessionStatus.InProgress)
            .OrderByDescending(item => item.LastCommittedAtUtc)
            .Take(MaximumActiveSessions).ToListAsync(cancellationToken);
        var result = new List<ActiveLessonSessionView>(sessions.Count);
        foreach (var session in sessions)
        {
            var planVersion = await db.LessonPlanVersions.AsNoTracking()
                .SingleAsync(item => item.Id == session.LessonPlanVersionId, cancellationToken);
            var participantName = await ParticipantNameAsync(session.StudentId, session.GroupId, cancellationToken);
            var count = await db.LessonSessionActivities.AsNoTracking()
                .CountAsync(item => item.LessonSessionId == session.Id, cancellationToken);
            result.Add(new(session.Id, session.LessonPlanId, session.LessonPlanVersionId,
                session.CalendarSessionId, session.StudentId.HasValue ? "student" : "group",
                participantName, planVersion.Goal, session.StartedAtUtc,
                session.LastCommittedAtUtc, session.Revision, count));
        }
        return result;
    }

    public async Task<LessonSessionResult> GetAsync(Guid teacherId, Guid lessonSessionId,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonSessionId == Guid.Empty)
            return Failure(LessonSessionFailure.Invalid);
        var view = await BuildViewAsync(teacherId, lessonSessionId, cancellationToken);
        return view is null ? Failure(LessonSessionFailure.NotFound)
            : new(view, LessonSessionFailure.None);
    }

    public async Task<LessonSessionResult> ReorderTimelineAsync(Guid teacherId,
        Guid lessonSessionId, Guid operationId, long expectedRevision,
        IReadOnlyList<Guid> activityIds, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonSessionId == Guid.Empty || operationId == Guid.Empty
            || expectedRevision <= 0 || activityIds.Count == 0
            || activityIds.Any(id => id == Guid.Empty) || activityIds.Distinct().Count() != activityIds.Count)
            return Failure(LessonSessionFailure.Invalid);
        var requestHash = Hash($"REORDER|{lessonSessionId:D}|{expectedRevision.ToString(CultureInfo.InvariantCulture)}|{string.Join(',', activityIds.Select(id => id.ToString("D")))}");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await AcquireLockAsync($"LessonSessionOperation:{teacherId:D}:{operationId:D}", cancellationToken);
            await AcquireLockAsync($"LessonSessionMutation:{lessonSessionId:D}", cancellationToken);
            var prior = await db.LessonSessionOperations.AsNoTracking().SingleOrDefaultAsync(item =>
                item.TeacherAccountId == teacherId && item.OperationId == operationId,
                cancellationToken);
            if (prior is not null)
            {
                if (prior.LessonSessionId != lessonSessionId
                    || !string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal))
                    return Failure(LessonSessionFailure.OperationMismatch);
                return new(await BuildViewAsync(teacherId, lessonSessionId, cancellationToken),
                    LessonSessionFailure.None);
            }

            var session = await db.LessonSessions.SingleOrDefaultAsync(item => item.Id == lessonSessionId
                && item.TeacherAccountId == teacherId, cancellationToken);
            if (session is null) return Failure(LessonSessionFailure.NotFound);
            if (session.Status != LessonSessionStatus.InProgress)
                return Failure(LessonSessionFailure.InvalidMutation);
            if (session.Revision != expectedRevision)
                return Failure(LessonSessionFailure.ConcurrencyConflict);
            var activities = await db.LessonSessionActivities.Where(item =>
                item.LessonSessionId == lessonSessionId).ToListAsync(cancellationToken);
            if (activities.Count != activityIds.Count
                || activities.Select(item => item.Id).ToHashSet().SetEquals(activityIds) is false)
                return Failure(LessonSessionFailure.InvalidMutation);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [LessonSessionActivities] SET [SortOrder] = [SortOrder] + 10000 WHERE [LessonSessionId] = {lessonSessionId}",
                cancellationToken);
            for (var index = 0; index < activityIds.Count; index++)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [LessonSessionActivities] SET [SortOrder] = {index} WHERE [Id] = {activityIds[index]} AND [LessonSessionId] = {lessonSessionId}",
                    cancellationToken);
            }
            var nowUtc = DateTimeOffset.UtcNow;
            session.CommitMutation(expectedRevision, nowUtc);
            db.Add(new LessonSessionOperation(Guid.NewGuid(), teacherId, session.Id,
                operationId, LessonSessionOperationType.ReorderTimeline, requestHash,
                expectedRevision, session.Revision, nowUtc));
            db.Add(new LessonSessionAuditEvent(Guid.NewGuid(), session.Id, teacherId,
                operationId, "TIMELINE_REORDERED", session.Status, session.Status, nowUtc));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return new(await BuildViewAsync(teacherId, session.Id, cancellationToken),
                LessonSessionFailure.None);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51010 or 51011)
        {
            return Failure(LessonSessionFailure.ActiveConflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(LessonSessionFailure.ConcurrencyConflict);
        }
    }

    private async Task<LessonSessionView?> BuildViewAsync(Guid teacherId, Guid lessonSessionId,
        CancellationToken cancellationToken)
    {
        var session = await db.LessonSessions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == lessonSessionId && item.TeacherAccountId == teacherId, cancellationToken);
        if (session is null) return null;
        var version = await db.LessonPlanVersions.AsNoTracking()
            .SingleAsync(item => item.Id == session.LessonPlanVersionId, cancellationToken);
        var activities = await db.LessonSessionActivities.AsNoTracking()
            .Where(item => item.LessonSessionId == session.Id)
            .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var ids = activities.Select(item => item.Id).ToArray();
        var materials = await db.LessonSessionActivityMaterialVersions.AsNoTracking()
            .Where(item => ids.Contains(item.LessonSessionActivityId)).ToListAsync(cancellationToken);
        var tasks = await db.LessonSessionActivityAssessableTaskVersions.AsNoTracking()
            .Where(item => ids.Contains(item.LessonSessionActivityId)).ToListAsync(cancellationToken);
        var views = activities.Select(item => new LessonSessionActivityView(item.Id,
            item.SourceLessonActivityId, item.SortOrder, item.Origin.ToString(), item.Status.ToString(),
            item.Name, item.Purpose.ToString(), item.DurationMinutes, item.WorkMode.ToString(),
            item.Description, item.StudentInstructions, item.ShowStudentInstructionsOnBoard,
            item.ShowProgressOnBoard, materials.Where(mapping => mapping.LessonSessionActivityId == item.Id)
                .Select(mapping => mapping.MaterialVersionId).ToArray(),
            tasks.Where(mapping => mapping.LessonSessionActivityId == item.Id)
                .Select(mapping => mapping.AssessableTaskVersionId).ToArray())).ToList();
        return new(session.Id, session.LessonPlanId, session.LessonPlanVersionId,
            version.VersionNumber, session.CalendarSessionId,
            session.StudentId.HasValue ? "student" : "group",
            await ParticipantNameAsync(session.StudentId, session.GroupId, cancellationToken),
            version.Goal, session.Status.ToString(), session.StartedAtUtc,
            session.ScheduledStartsAtUtc, session.ScheduledEndsAtUtc,
            session.ScheduledTimeZoneId, session.LastCommittedAtUtc, session.Revision,
            Convert.ToBase64String(session.RowVersion), views);
    }

    private async Task<string> ParticipantNameAsync(Guid? studentId, Guid? groupId,
        CancellationToken cancellationToken) => studentId.HasValue
        ? await db.Students.AsNoTracking().Where(item => item.Id == studentId)
            .Select(item => item.FirstName + " " + item.LastName).SingleAsync(cancellationToken)
        : await db.Groups.AsNoTracking().Where(item => item.Id == groupId)
            .Select(item => item.Name).SingleAsync(cancellationToken);

    private async Task AcquireLockAsync(string resource, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("A transaction is required for application locks.");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource, 'Exclusive', 'Transaction', 10000; SELECT @result;";
        command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = resource });
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (result < 0) throw new InvalidOperationException($"SQL application lock failed with code {result}.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static LessonSessionResult Failure(LessonSessionFailure failure) => new(null, failure);

}
