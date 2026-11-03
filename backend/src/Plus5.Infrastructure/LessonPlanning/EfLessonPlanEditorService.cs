using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonPlanEditorService(
    Plus5DbContext db,
    ILessonStructureService structureService,
    TimeProvider clock) : ILessonPlanEditorService
{
    public async Task<LessonPlanEditorResult> GetAsync(
        Guid teacherId,
        Guid lessonPlanId,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty)
            return Failure(LessonPlanEditorFailure.Invalid);

        var structure = await structureService.GetAsync(teacherId, lessonPlanId, cancellationToken);
        if (structure.Failure != LessonStructureFailure.None || structure.View is null)
            return Failure(LessonPlanEditorFailure.NotFound);

        return new(await BuildViewAsync(teacherId, structure.View, cancellationToken), LessonPlanEditorFailure.None);
    }

    public async Task<LessonMaterialCandidateResult> GetMaterialCandidatesAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty || activityId == Guid.Empty
            || page < 1 || pageSize is < 1 or > 50 || search?.Length > 120)
            return CandidateFailure(LessonPlanEditorFailure.Invalid);

        var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, true, cancellationToken);
        if (state is null) return CandidateFailure(LessonPlanEditorFailure.NotFound);

        var activityComponentIds = await db.LessonActivityKnowledgeComponents.AsNoTracking()
            .Where(item => item.LessonActivityId == activityId)
            .Select(item => item.KnowledgeComponentId)
            .ToArrayAsync(cancellationToken);
        var query = AccessibleCurrentMaterials(teacherId);
        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(item => item.Version.Title.Contains(normalizedSearch)
                || (item.Version.Subject != null && item.Version.Subject.Contains(normalizedSearch))
                || item.Version.MaterialTypeCode.Contains(normalizedSearch));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
            return new(new([], page, pageSize, total), LessonPlanEditorFailure.None);

        var rows = await query
            .Select(item => new
            {
                Material = item.Material,
                Version = item.Version,
                MatchingCount = db.MaterialVersionKnowledgeComponents.Count(mapping =>
                    mapping.MaterialVersionId == item.Version.Id
                    && activityComponentIds.Contains(mapping.KnowledgeComponentId)),
                IsAttached = db.LessonActivityMaterialVersions.Any(mapping =>
                    mapping.LessonActivityId == activityId
                    && mapping.MaterialVersionId == item.Version.Id),
            })
            .OrderByDescending(item => item.MatchingCount)
            .ThenBy(item => item.Version.Title)
            .ThenBy(item => item.Version.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new(new(rows.Select(item => new LessonMaterialCandidate(
            item.Material.Id,
            item.Version.Id,
            item.Version.VersionNumber,
            item.Version.Title,
            item.Version.MaterialTypeCode,
            item.Version.Subject,
            item.Material.OwnerTeacherId == teacherId,
            item.MatchingCount,
            item.IsAttached)).ToList(), page, pageSize, total), LessonPlanEditorFailure.None);
    }

    public Task<LessonPlanEditorResult> AttachMaterialAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeAttachmentAsync(teacherId, lessonPlanId, activityId, materialVersionId,
            rowVersion, attach: true, cancellationToken);

    public Task<LessonPlanEditorResult> DetachMaterialAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeAttachmentAsync(teacherId, lessonPlanId, activityId, materialVersionId,
            rowVersion, attach: false, cancellationToken);

    private async Task<LessonPlanEditorResult> ChangeAttachmentAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        Guid materialVersionId,
        string rowVersion,
        bool attach,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty || activityId == Guid.Empty
            || materialVersionId == Guid.Empty || !TryRowVersion(rowVersion, out var expected))
            return Failure(LessonPlanEditorFailure.Invalid);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, activityId, false, cancellationToken);
            if (state is null) return Failure(LessonPlanEditorFailure.NotFound);
            if (state.Version.Status != LessonPlanVersionStatus.Draft)
                return Failure(LessonPlanEditorFailure.NotDraft);

            var existing = await db.LessonActivityMaterialVersions.SingleOrDefaultAsync(item =>
                item.LessonActivityId == activityId && item.MaterialVersionId == materialVersionId,
                cancellationToken);
            if (attach)
            {
                if (existing is not null) return Failure(LessonPlanEditorFailure.AlreadyAttached);
                if (!await AccessibleCurrentMaterials(teacherId)
                    .AnyAsync(item => item.Version.Id == materialVersionId, cancellationToken))
                    return Failure(LessonPlanEditorFailure.MaterialUnavailable);

                db.LessonActivityMaterialVersions.Add(
                    new LessonActivityMaterialVersion(activityId, materialVersionId));
            }
            else
            {
                if (existing is null) return Failure(LessonPlanEditorFailure.AttachmentNotFound);
                db.LessonActivityMaterialVersions.Remove(existing);
            }

            db.Entry(state.Version).Property(item => item.RowVersion).OriginalValue = expected;
            state.Version.RecordAuthoringChange(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);

            var structure = await structureService.GetAsync(teacherId, lessonPlanId, cancellationToken);
            if (structure.View is null) return Failure(LessonPlanEditorFailure.Conflict);
            return new(await BuildViewAsync(teacherId, structure.View, cancellationToken), LessonPlanEditorFailure.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(LessonPlanEditorFailure.Conflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
            { Number: 2601 or 2627 or 51320 or 51321 })
        {
            return Failure(LessonPlanEditorFailure.Conflict);
        }
    }

    private async Task<LessonPlanEditorView> BuildViewAsync(
        Guid teacherId,
        LessonStructureView structure,
        CancellationToken cancellationToken)
    {
        var plan = await db.LessonPlans.AsNoTracking()
            .SingleAsync(item => item.Id == structure.LessonPlanId
                && item.TeacherAccountId == teacherId, cancellationToken);
        LessonPlanEditorSession? session = null;
        if (plan.SessionId is Guid sessionId)
        {
            session = await db.Sessions.AsNoTracking()
                .Where(item => item.Id == sessionId && item.TeacherAccountId == teacherId)
                .Select(item => new LessonPlanEditorSession(
                    item.Id, item.StartsAtUtc, item.EndsAtUtc, item.TimeZoneId, item.Title))
                .SingleAsync(cancellationToken);
        }

        var activityIds = structure.Activities.Select(item => item.Id).Where(item => item != Guid.Empty).ToArray();
        var attachmentRows = await (
            from mapping in db.LessonActivityMaterialVersions.AsNoTracking()
            join activity in db.LessonActivities.AsNoTracking() on mapping.LessonActivityId equals activity.Id
            join version in db.MaterialVersions.AsNoTracking() on mapping.MaterialVersionId equals version.Id
            join material in db.Materials.AsNoTracking() on version.MaterialId equals material.Id
            where activityIds.Contains(activity.Id)
            select new { mapping.LessonActivityId, Material = material, Version = version })
            .ToListAsync(cancellationToken);
        var activityMappings = await db.LessonActivityKnowledgeComponents.AsNoTracking()
            .Where(item => activityIds.Contains(item.LessonActivityId))
            .ToListAsync(cancellationToken);
        var materialVersionIds = attachmentRows.Select(item => item.Version.Id).Distinct().ToArray();
        var materialMappings = await db.MaterialVersionKnowledgeComponents.AsNoTracking()
            .Where(item => materialVersionIds.Contains(item.MaterialVersionId))
            .ToListAsync(cancellationToken);

        var activities = structure.Activities.Select(activity =>
        {
            var activityKnowledge = activityMappings
                .Where(item => item.LessonActivityId == activity.Id)
                .Select(item => item.KnowledgeComponentId)
                .ToHashSet();
            var materials = attachmentRows.Where(item => item.LessonActivityId == activity.Id)
                .Select(item => new LessonActivityMaterialAttachment(
                    item.Material.Id,
                    item.Version.Id,
                    item.Version.VersionNumber,
                    item.Version.Title,
                    item.Version.MaterialTypeCode,
                    item.Version.Subject,
                    item.Material.CurrentVersionId == item.Version.Id,
                    item.Material.OwnerTeacherId == teacherId,
                    materialMappings.Count(mapping => mapping.MaterialVersionId == item.Version.Id
                        && activityKnowledge.Contains(mapping.KnowledgeComponentId))))
                .OrderBy(item => item.Title)
                .ToList();
            return new LessonPlanEditorActivity(activity.Id, activity.SortOrder, activity.Name,
                activity.Purpose, activity.DurationMinutes, activity.KnowledgeBlockCode,
                activity.KnowledgeComponentIds, materials);
        }).ToList();

        var allAttachments = activities.SelectMany(item => item.Materials)
            .GroupBy(item => item.MaterialVersionId)
            .Select(group => group.First())
            .OrderBy(item => item.Title)
            .ToList();
        var balance = structure.Balance.Select(area => area.Code == "MaterialRelevance"
            ? BuildMaterialBalance(activities)
            : area).ToList();

        return new(structure.LessonPlanId, structure.LessonPlanVersionId, structure.RowVersion,
            structure.ParticipantName, structure.ParticipantType, session, structure.Goal,
            structure.TargetDurationMinutes, structure.KnowledgeComponents, activities,
            balance, allAttachments);
    }

    private static LessonBalanceArea BuildMaterialBalance(IReadOnlyList<LessonPlanEditorActivity> activities)
    {
        var materials = activities.SelectMany(item => item.Materials).ToList();
        if (materials.Count == 0)
            return new("MaterialRelevance", "Relevantnost materijala", LessonBalanceStatus.NotEvaluated,
                "Materijali nisu obavezni; relevantnost će se prikazati nakon odabira.");

        var unmatched = activities.Any(activity => activity.KnowledgeComponentIds.Count > 0
            && activity.Materials.Any(material => material.MatchingKnowledgeComponentCount == 0));
        return unmatched
            ? new("MaterialRelevance", "Relevantnost materijala", LessonBalanceStatus.Warning,
                "Neki povezani materijali nemaju exact podudaranje s komponentama znanja aktivnosti.")
            : new("MaterialRelevance", "Relevantnost materijala", LessonBalanceStatus.Satisfied,
                "Povezani materijali podudaraju se s komponentama znanja svojih aktivnosti.");
    }

    private async Task<State?> LoadStateAsync(
        Guid teacherId,
        Guid lessonPlanId,
        Guid activityId,
        bool noTracking,
        CancellationToken cancellationToken)
    {
        var query = from plan in db.LessonPlans
                    join version in db.LessonPlanVersions on plan.Id equals version.LessonPlanId
                    join activity in db.LessonActivities on version.Id equals activity.LessonPlanVersionId
                    where plan.Id == lessonPlanId && plan.TeacherAccountId == teacherId
                        && plan.ArchivedAtUtc == null && activity.Id == activityId
                    select new State(version, activity);
        if (noTracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<MaterialAccessRow> AccessibleCurrentMaterials(Guid teacherId) =>
        from material in db.Materials.AsNoTracking()
        join version in db.MaterialVersions.AsNoTracking()
            on material.CurrentVersionId equals version.Id
        where material.Status == MaterialStatus.Active
            && material.ArchivedAtUtc == null
            && version.Status == MaterialVersionStatus.Active
            && db.MaterialFiles.Any(file => file.MaterialVersionId == version.Id
                && file.Status == MaterialFileStatus.Clean)
            && (material.OwnerTeacherId == teacherId
                || material.Visibility == MaterialVisibility.Shared
                && db.MaterialShares.Any(share => share.MaterialId == material.Id
                    && share.SharedWithTeacherId == teacherId
                    && share.Permission == MaterialShareAccess.Use))
        select new MaterialAccessRow { Material = material, Version = version };

    private static bool TryRowVersion(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length == 8;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static LessonPlanEditorResult Failure(LessonPlanEditorFailure failure) => new(null, failure);
    private static LessonMaterialCandidateResult CandidateFailure(LessonPlanEditorFailure failure) => new(null, failure);

    private sealed record State(LessonPlanVersion Version, LessonActivity Activity);
    private sealed class MaterialAccessRow
    {
        public required Material Material { get; init; }
        public required MaterialVersion Version { get; init; }
    }
}
