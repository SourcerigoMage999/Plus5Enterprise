using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonActivityEditingService(
    Plus5DbContext db,
    ILessonPlanEditorService editorService,
    IOptions<LessonBuilderAiOptions> aiOptions,
    TimeProvider clock) : ILessonActivityEditingService
{
    public async Task<LessonActivityDetailResult> GetAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty || activityId == Guid.Empty)
            return Failure(LessonActivityEditingFailure.Invalid);
        return await BuildDetailAsync(teacherId, lessonPlanId, activityId, cancellationToken);
    }

    public async Task<LessonActivityDetailResult> UpdateAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        UpdateLessonActivityCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.RowVersion, out var expected)
            || !Enum.TryParse<LessonActivityPurpose>(command.Purpose, true, out var purpose)
            || !Enum.TryParse<LessonActivityWorkMode>(command.WorkMode, true, out var workMode))
            return Failure(LessonActivityEditingFailure.Invalid);

        await using var transaction = await BeginAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, cancellationToken);
            if (state is null) return Failure(LessonActivityEditingFailure.NotFound);
            try
            {
                state.Activity.UpdateCore(command.Name, purpose, command.DurationMinutes, workMode,
                    command.Description, command.PurposeDescription, command.TeacherInstructions,
                    command.StudentInstructions, command.Notes,
                    command.ShowTeacherInstructionsOnBoard,
                    command.ShowStudentInstructionsOnBoard,
                    command.ShowProgressOnBoard);
            }
            catch (ArgumentException)
            {
                return Failure(LessonActivityEditingFailure.Invalid);
            }

            Touch(state.Version, expected);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return await BuildDetailAsync(teacherId, lessonPlanId, activityId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(LessonActivityEditingFailure.Conflict);
        }
    }

    public async Task<LessonActivityMutationResult> DuplicateAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected)) return MutationFailure(LessonActivityEditingFailure.Invalid);
        await using var transaction = await BeginAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, cancellationToken);
            if (state is null) return MutationFailure(LessonActivityEditingFailure.NotFound);
            var activities = await db.LessonActivities
                .Where(item => item.LessonPlanVersionId == state.Version.Id)
                .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
            var source = activities.Single(item => item.Id == activityId);
            var sourceKnowledge = await db.LessonActivityKnowledgeComponents
                .Where(item => item.LessonActivityId == source.Id).ToListAsync(cancellationToken);
            var sourceMaterials = await db.LessonActivityMaterialVersions
                .Where(item => item.LessonActivityId == source.Id).ToListAsync(cancellationToken);

            await MoveToTemporaryOrderAsync(activities, cancellationToken);
            var copy = source.Duplicate(Guid.NewGuid(), source.SortOrder + 1);
            db.LessonActivities.Add(copy);
            foreach (var item in activities)
                item.ChangeSortOrder(item.Id == source.Id || item.SortOrder < source.SortOrder
                    ? item.SortOrder : item.SortOrder + 1);
            db.LessonActivityKnowledgeComponents.AddRange(sourceKnowledge.Select(item =>
                new LessonActivityKnowledgeComponent(copy.Id, item.KnowledgeComponentId)));
            db.LessonActivityMaterialVersions.AddRange(sourceMaterials.Select(item =>
                new LessonActivityMaterialVersion(copy.Id, item.MaterialVersionId)));
            Touch(state.Version, expected);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return await EditorResultAsync(teacherId, lessonPlanId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MutationFailure(LessonActivityEditingFailure.Conflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException)
        {
            return MutationFailure(LessonActivityEditingFailure.Conflict);
        }
    }

    public async Task<LessonActivityMutationResult> DeleteAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected)) return MutationFailure(LessonActivityEditingFailure.Invalid);
        await using var transaction = await BeginAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, cancellationToken);
            if (state is null) return MutationFailure(LessonActivityEditingFailure.NotFound);
            var activities = await db.LessonActivities
                .Where(item => item.LessonPlanVersionId == state.Version.Id)
                .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
            var removedOrder = state.Activity.SortOrder;
            db.LessonActivityKnowledgeComponents.RemoveRange(db.LessonActivityKnowledgeComponents.Where(item => item.LessonActivityId == activityId));
            db.LessonActivityMaterialVersions.RemoveRange(db.LessonActivityMaterialVersions.Where(item => item.LessonActivityId == activityId));
            db.LessonActivities.Remove(state.Activity);
            await db.SaveChangesAsync(cancellationToken);
            var remaining = activities.Where(item => item.Id != activityId).ToList();
            await MoveToTemporaryOrderAsync(remaining, cancellationToken);
            foreach (var item in remaining.Where(item => item.SortOrder > removedOrder))
                item.ChangeSortOrder(item.SortOrder - 1);
            Touch(state.Version, expected);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return await EditorResultAsync(teacherId, lessonPlanId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MutationFailure(LessonActivityEditingFailure.Conflict);
        }
    }

    public async Task<LessonActivityMutationResult> ReorderAsync(
        Guid teacherId,
        Guid lessonPlanId,
        ReorderLessonActivitiesCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.RowVersion, out var expected)
            || command.ActivityIds.Count == 0
            || command.ActivityIds.Any(id => id == Guid.Empty)
            || command.ActivityIds.Distinct().Count() != command.ActivityIds.Count)
            return MutationFailure(LessonActivityEditingFailure.Invalid);
        await using var transaction = await BeginAsync(cancellationToken);
        try
        {
            var state = await LoadPlanAsync(teacherId, lessonPlanId, cancellationToken);
            if (state is null) return MutationFailure(LessonActivityEditingFailure.NotFound);
            var activities = await db.LessonActivities.Where(item => item.LessonPlanVersionId == state.Id).ToListAsync(cancellationToken);
            if (activities.Count != command.ActivityIds.Count
                || activities.Select(item => item.Id).ToHashSet().SetEquals(command.ActivityIds) is false)
                return MutationFailure(LessonActivityEditingFailure.Invalid);
            await MoveToTemporaryOrderAsync(activities, cancellationToken);
            var byId = activities.ToDictionary(item => item.Id);
            for (var index = 0; index < command.ActivityIds.Count; index++) byId[command.ActivityIds[index]].ChangeSortOrder(index);
            Touch(state, expected);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return await EditorResultAsync(teacherId, lessonPlanId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MutationFailure(LessonActivityEditingFailure.Conflict);
        }
    }

    public async Task<LessonActivityDetailResult> UpdateKnowledgeAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        UpdateLessonActivityKnowledgeCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.RowVersion, out var expected)
            || command.KnowledgeComponentIds.Any(id => id == Guid.Empty)
            || command.KnowledgeComponentIds.Distinct().Count() != command.KnowledgeComponentIds.Count)
            return Failure(LessonActivityEditingFailure.Invalid);
        await using var transaction = await BeginAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, cancellationToken);
            if (state is null) return Failure(LessonActivityEditingFailure.NotFound);
            var allowed = await db.LessonPlanVersionKnowledgeComponents
                .Where(item => item.LessonPlanVersionId == state.Version.Id)
                .Select(item => item.KnowledgeComponentId).ToListAsync(cancellationToken);
            if (!command.KnowledgeComponentIds.All(allowed.Contains)) return Failure(LessonActivityEditingFailure.Invalid);
            var existing = await db.LessonActivityKnowledgeComponents
                .Where(item => item.LessonActivityId == activityId).ToListAsync(cancellationToken);
            db.LessonActivityKnowledgeComponents.RemoveRange(existing.Where(item => !command.KnowledgeComponentIds.Contains(item.KnowledgeComponentId)));
            db.LessonActivityKnowledgeComponents.AddRange(command.KnowledgeComponentIds
                .Where(id => existing.All(item => item.KnowledgeComponentId != id))
                .Select(id => new LessonActivityKnowledgeComponent(activityId, id)));
            Touch(state.Version, expected);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return await BuildDetailAsync(teacherId, lessonPlanId, activityId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(LessonActivityEditingFailure.Conflict);
        }
    }

    private async Task<LessonActivityDetailResult> BuildDetailAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, cancellationToken, noTracking: true);
        if (state is null) return Failure(LessonActivityEditingFailure.NotFound);
        var editor = await editorService.GetAsync(teacherId, lessonPlanId, cancellationToken);
        if (editor.View is null) return Failure(LessonActivityEditingFailure.NotFound);
        var activityView = editor.View.Activities.Single(item => item.Id == activityId);
        var kb = await (from version in db.KnowledgeBlockVersions.AsNoTracking()
                        join block in db.KnowledgeBlocks.AsNoTracking() on version.KnowledgeBlockId equals block.Id
                        where version.Id == state.Activity.KnowledgeBlockVersionId
                        select new { block.Code, version.DisplayName }).SingleOrDefaultAsync(cancellationToken);
        return new(new LessonActivityDetail(
            lessonPlanId, state.Version.Id, Convert.ToBase64String(state.Version.RowVersion),
            editor.View.ParticipantName, editor.View.ParticipantType, editor.View.Session,
            editor.View.Goal, editor.View.TargetDurationMinutes, state.Activity.Id,
            state.Activity.SortOrder, state.Activity.Name, state.Activity.Purpose.ToString(),
            state.Activity.DurationMinutes, state.Activity.WorkMode.ToString(),
            state.Activity.Description, state.Activity.PurposeDescription,
            state.Activity.TeacherInstructions, state.Activity.StudentInstructions,
            state.Activity.Notes, state.Activity.ShowTeacherInstructionsOnBoard,
            state.Activity.ShowStudentInstructionsOnBoard, state.Activity.ShowProgressOnBoard,
            state.Activity.KnowledgeBlockVersionId, kb?.Code, kb?.DisplayName,
            editor.View.KnowledgeComponents, activityView.KnowledgeComponentIds,
            activityView.Materials, new(aiOptions.Value.Enabled, true, false, false)),
            LessonActivityEditingFailure.None);
    }

    private async Task<LessonActivityMutationResult> EditorResultAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken)
    {
        var result = await editorService.GetAsync(teacherId, lessonPlanId, cancellationToken);
        return result.View is null
            ? MutationFailure(LessonActivityEditingFailure.Conflict)
            : new(result.View, LessonActivityEditingFailure.None);
    }

    private async Task<State?> LoadStateAsync(Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken, bool noTracking = false)
    {
        var query = from plan in db.LessonPlans
                    join version in db.LessonPlanVersions on plan.Id equals version.LessonPlanId
                    join activity in db.LessonActivities on version.Id equals activity.LessonPlanVersionId
                    where plan.Id == lessonPlanId && plan.TeacherAccountId == teacherId
                        && plan.ArchivedAtUtc == null && version.Status == LessonPlanVersionStatus.Draft
                        && activity.Id == activityId
                    select new State(version, activity);
        if (noTracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<LessonPlanVersion?> LoadPlanAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken) =>
        await (from plan in db.LessonPlans
               join version in db.LessonPlanVersions on plan.Id equals version.LessonPlanId
               where plan.Id == lessonPlanId && plan.TeacherAccountId == teacherId
                   && plan.ArchivedAtUtc == null && version.Status == LessonPlanVersionStatus.Draft
               select version).SingleOrDefaultAsync(cancellationToken);

    private async Task MoveToTemporaryOrderAsync(List<LessonActivity> activities, CancellationToken cancellationToken)
    {
        if (activities.Count == 0) return;
        foreach (var item in activities) item.ChangeSortOrder(item.SortOrder + 1000);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in activities) item.ChangeSortOrder(item.SortOrder - 1000);
    }

    private void Touch(LessonPlanVersion version, byte[] expected)
    {
        db.Entry(version).Property(item => item.RowVersion).OriginalValue = expected;
        version.RecordAuthoringChange(clock.GetUtcNow());
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginAsync(CancellationToken cancellationToken) =>
        db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;

    private static Task CommitAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction, CancellationToken cancellationToken) =>
        transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);

    private static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length == 8; }
        catch (FormatException) { bytes = []; return false; }
    }

    private static LessonActivityDetailResult Failure(LessonActivityEditingFailure failure) => new(null, failure);
    private static LessonActivityMutationResult MutationFailure(LessonActivityEditingFailure failure) => new(null, failure);
    private sealed record State(LessonPlanVersion Version, LessonActivity Activity);
}
