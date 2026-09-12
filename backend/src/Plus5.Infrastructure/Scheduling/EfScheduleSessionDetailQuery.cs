using Microsoft.EntityFrameworkCore;
using Plus5.Application.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Scheduling;

public sealed class EfScheduleSessionDetailQuery(Plus5DbContext db) : IScheduleSessionDetailQuery
{
    public async Task<ScheduleSessionDetail?> GetAsync(
        Guid owner,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(owner, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);

        var session = await db.Sessions.AsNoTracking()
            .Where(item => item.TeacherAccountId == owner && item.Id == sessionId)
            .Select(item => new SessionProjection(
                item.Id,
                item.DeliveryMode,
                item.GroupId,
                item.StudentId,
                item.Title,
                item.Notes,
                item.StartsAtUtc,
                item.EndsAtUtc,
                item.TimeZoneId,
                item.LocationId,
                item.OnlineMeetingUrl,
                item.Status,
                item.RecurringSessionSeriesId,
                item.IsSeriesException,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.CancelledAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        var locationName = session.LocationId.HasValue
            ? await db.Locations.AsNoTracking()
                .Where(location => location.TeacherAccountId == owner && location.Id == session.LocationId)
                .Select(location => location.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        if (session.DeliveryMode == DeliveryMode.Group)
        {
            var context = await (
                from sourceGroup in db.Groups.AsNoTracking()
                join program in db.Programs.AsNoTracking() on sourceGroup.ProgramId equals program.Id
                join grade in db.SchoolGrades.AsNoTracking() on sourceGroup.SchoolGradeId equals grade.Id
                where sourceGroup.TeacherAccountId == owner && sourceGroup.Id == session.GroupId
                    && program.TeacherAccountId == owner
                select new
                {
                    sourceGroup.Id,
                    sourceGroup.Name,
                    sourceGroup.ProgramId,
                    ProgramName = program.Name,
                    sourceGroup.SchoolGradeId,
                    SchoolGrade = grade.Name,
                    GroupStatus = (int)sourceGroup.Status,
                    sourceGroup.Capacity,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (context is null)
            {
                return null;
            }

            var participants = await (
                from membership in db.GroupMemberships.AsNoTracking()
                join student in db.Students.AsNoTracking() on membership.StudentId equals student.Id
                join grade in db.SchoolGrades.AsNoTracking() on student.SchoolGradeId equals grade.Id
                where membership.TeacherAccountId == owner
                    && membership.GroupId == context.Id
                    && student.TeacherAccountId == owner
                    && membership.JoinedAtUtc <= session.StartsAtUtc
                    && (membership.LeftAtUtc == null || membership.LeftAtUtc > session.StartsAtUtc)
                orderby student.LastName, student.FirstName, student.Id
                select new ScheduleSessionParticipant(
                    student.Id,
                    student.FirstName,
                    student.LastName,
                    student.SchoolGradeId,
                    grade.Name,
                    (int)student.Status))
                .ToListAsync(cancellationToken);

            return Map(session, context.Id, null, context.Name, context.ProgramId, context.ProgramName,
                context.SchoolGradeId, context.SchoolGrade, context.GroupStatus, context.Capacity,
                locationName, participants);
        }

        var studentContext = await (
            from student in db.Students.AsNoTracking()
            join grade in db.SchoolGrades.AsNoTracking() on student.SchoolGradeId equals grade.Id
            join program in db.Programs.AsNoTracking() on student.ProgramId equals (Guid?)program.Id into programs
            from program in programs.DefaultIfEmpty()
            where student.TeacherAccountId == owner && student.Id == session.StudentId
                && (program == null || program.TeacherAccountId == owner)
            select new
            {
                student.Id,
                student.FirstName,
                student.LastName,
                student.ProgramId,
                ProgramName = program == null ? null : program.Name,
                student.SchoolGradeId,
                SchoolGrade = grade.Name,
                StudentStatus = (int)student.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (studentContext is null)
        {
            return null;
        }

        ScheduleSessionParticipant[] individualParticipant =
        [
            new(studentContext.Id, studentContext.FirstName, studentContext.LastName,
                studentContext.SchoolGradeId, studentContext.SchoolGrade, studentContext.StudentStatus),
        ];

        return Map(session, null, studentContext.Id,
            $"{studentContext.FirstName} {studentContext.LastName}", studentContext.ProgramId,
            studentContext.ProgramName, studentContext.SchoolGradeId, studentContext.SchoolGrade,
            null, null, locationName, individualParticipant);
    }

    private static ScheduleSessionDetail Map(
        SessionProjection session,
        Guid? groupId,
        Guid? studentId,
        string contextName,
        Guid? programId,
        string? programName,
        Guid schoolGradeId,
        string schoolGrade,
        int? groupStatus,
        int? capacity,
        string? locationName,
        IReadOnlyList<ScheduleSessionParticipant> participants) => new(
            session.Id,
            (int)session.DeliveryMode,
            groupId,
            studentId,
            contextName,
            programId,
            programName,
            schoolGradeId,
            schoolGrade,
            groupStatus,
            capacity,
            session.Title,
            session.Notes,
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.TimeZoneId,
            session.LocationId,
            locationName,
            session.OnlineMeetingUrl is not null,
            (int)session.Status,
            session.RecurringSessionSeriesId is not null,
            session.IsSeriesException,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            session.CancelledAtUtc,
            participants);

    private sealed record SessionProjection(
        Guid Id,
        DeliveryMode DeliveryMode,
        Guid? GroupId,
        Guid? StudentId,
        string? Title,
        string? Notes,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        string TimeZoneId,
        Guid? LocationId,
        string? OnlineMeetingUrl,
        Plus5.Domain.Scheduling.SessionStatus Status,
        Guid? RecurringSessionSeriesId,
        bool IsSeriesException,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? CancelledAtUtc);
}
