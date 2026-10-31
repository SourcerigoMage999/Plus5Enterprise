using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Groups;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonGoalSelectionService(Plus5DbContext db, TimeProvider clock)
    : ILessonGoalSelectionService
{
    public async Task<CreateLessonGoalResult> CreateAsync(
        Guid teacherId,
        CreateLessonGoalCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsValid(command)) return Failure(CreateLessonGoalFailure.Invalid);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var studentId = command.ParticipantKind == LessonParticipantKind.Student ? command.ParticipantId : (Guid?)null;
            var groupId = command.ParticipantKind == LessonParticipantKind.Group ? command.ParticipantId : (Guid?)null;
            if (studentId.HasValue)
            {
                var exists = await db.Students.AnyAsync(item => item.Id == studentId
                    && item.TeacherAccountId == teacherId && item.ArchivedAtUtc == null
                    && item.Status != StudentStatus.Inactive, cancellationToken);
                if (!exists) return Failure(CreateLessonGoalFailure.ParticipantUnavailable);
            }
            else
            {
                var exists = await db.Groups.AnyAsync(item => item.Id == groupId
                    && item.TeacherAccountId == teacherId && item.ArchivedAtUtc == null
                    && item.Status != GroupStatus.Inactive, cancellationToken);
                if (!exists) return Failure(CreateLessonGoalFailure.ParticipantUnavailable);
            }

            if (command.SessionId.HasValue)
            {
                var sessionMatches = await db.Sessions.AnyAsync(item => item.Id == command.SessionId
                    && item.TeacherAccountId == teacherId && item.Status == SessionStatus.Scheduled
                    && item.StudentId == studentId && item.GroupId == groupId, cancellationToken);
                if (!sessionMatches) return Failure(CreateLessonGoalFailure.SessionMismatch);
            }

            var componentIds = command.KnowledgeComponentIds.Distinct().ToArray();
            var validComponentCount = await (
                from component in db.KnowledgeComponents
                join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
                where componentIds.Contains(component.Id)
                    && component.Status == KnowledgeComponentStatus.Active
                    && model.Status == KnowledgeModelStatus.Published
                select component.Id).CountAsync(cancellationToken);
            if (validComponentCount != componentIds.Length)
                return Failure(CreateLessonGoalFailure.KnowledgeUnavailable);

            var now = clock.GetUtcNow();
            var plan = new LessonPlan(Guid.NewGuid(), teacherId, studentId, groupId, command.SessionId, now);
            var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, command.Goal, now);
            db.LessonPlans.Add(plan);
            db.LessonPlanVersions.Add(version);
            db.LessonPlanVersionKnowledgeComponents.AddRange(componentIds.Select(id =>
                new LessonPlanVersionKnowledgeComponent(version.Id, id)));
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(plan.Id, version.Id, CreateLessonGoalFailure.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(CreateLessonGoalFailure.Conflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 or 1205 })
        {
            return Failure(CreateLessonGoalFailure.Conflict);
        }
    }

    private static bool IsValid(CreateLessonGoalCommand command) =>
        Enum.IsDefined(command.ParticipantKind)
        && command.ParticipantId != Guid.Empty
        && command.SessionId != Guid.Empty
        && !string.IsNullOrWhiteSpace(command.Goal)
        && command.Goal.Trim().Length <= LessonPlanVersion.GoalMaxLength
        && command.KnowledgeComponentIds is { Count: > 0 and <= 50 }
        && command.KnowledgeComponentIds.All(id => id != Guid.Empty)
        && command.KnowledgeComponentIds.Distinct().Count() == command.KnowledgeComponentIds.Count;

    private static CreateLessonGoalResult Failure(CreateLessonGoalFailure failure) => new(null, null, failure);
}
