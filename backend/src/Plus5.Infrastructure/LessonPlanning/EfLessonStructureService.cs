using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonStructureService(Plus5DbContext db, TimeProvider clock) : ILessonStructureService
{
    private const string PolicyCode = "SYSTEM_BASELINE_V1";
    private static readonly string[] Names = ["Small Talk", "Aktivacija predznanja", "Objašnjenje novog gradiva", "Interaktivni zadaci", "Provjera razumijevanja"];
    private static readonly string?[] BlockCodes = ["KB-001", "KB-002", "KB-004", "KB-005", null];
    private static readonly string[] PublishedBlockCodes = ["KB-001", "KB-002", "KB-004", "KB-005"];
    private static readonly LessonActivityPurpose[] Purposes = [LessonActivityPurpose.Engagement,
        LessonActivityPurpose.ActivatePriorKnowledge, LessonActivityPurpose.Instruction,
        LessonActivityPurpose.Practice, LessonActivityPurpose.CheckForUnderstanding];

    public async Task<LessonStructureResult> GetAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(teacherId, lessonPlanId, true, cancellationToken);
        return state is null ? Failure(LessonStructureFailure.NotFound) : new(await BuildViewAsync(state, cancellationToken), LessonStructureFailure.None);
    }

    public async Task<LessonStructureResult> ChangeTargetDurationAsync(Guid teacherId, Guid lessonPlanId,
        int targetDurationMinutes, string rowVersion, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected) || targetDurationMinutes is < 5 or > 480)
            return Failure(LessonStructureFailure.Invalid);
        var state = await LoadStateAsync(teacherId, lessonPlanId, false, cancellationToken);
        if (state is null) return Failure(LessonStructureFailure.NotFound);
        if (state.Version.Status != LessonPlanVersionStatus.Draft) return Failure(LessonStructureFailure.NotDraft);
        db.Entry(state.Version).Property(item => item.RowVersion).OriginalValue = expected;
        state.Version.ChangeTargetDuration(targetDurationMinutes, clock.GetUtcNow());
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new(await BuildViewAsync(state, cancellationToken), LessonStructureFailure.None);
        }
        catch (DbUpdateConcurrencyException) { return Failure(LessonStructureFailure.Conflict); }
    }

    public async Task<LessonStructureResult> ApplyBaselineAsync(Guid teacherId, Guid lessonPlanId,
        string rowVersion, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected)) return Failure(LessonStructureFailure.Invalid);
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            var state = await LoadStateAsync(teacherId, lessonPlanId, false, cancellationToken);
            if (state is null) return Failure(LessonStructureFailure.NotFound);
            if (state.Version.Status != LessonPlanVersionStatus.Draft) return Failure(LessonStructureFailure.NotDraft);
            if (await db.LessonActivities.AnyAsync(item => item.LessonPlanVersionId == state.Version.Id, cancellationToken))
                return Failure(LessonStructureFailure.AlreadyApplied);

            var blocks = await (from block in db.KnowledgeBlocks
                                join version in db.KnowledgeBlockVersions on block.CurrentPublishedVersionId equals version.Id
                                where PublishedBlockCodes.Contains(block.Code)
                                    && block.Status == KnowledgeBlockStatus.Active
                                    && version.Status == KnowledgeBlockVersionStatus.Published
                                select new { block.Code, Version = version }).ToDictionaryAsync(item => item.Code, cancellationToken);
            if (blocks.Count != 4) return Failure(LessonStructureFailure.KnowledgeBlockUnavailable);

            var componentIds = await db.LessonPlanVersionKnowledgeComponents
                .Where(item => item.LessonPlanVersionId == state.Version.Id)
                .Select(item => item.KnowledgeComponentId).ToArrayAsync(cancellationToken);
            var durations = LessonStructurePolicy.AllocateDurations(state.Version.TargetDurationMinutes);
            for (var index = 0; index < Names.Length; index++)
            {
                var blockCode = BlockCodes[index];
                var activity = new LessonActivity(Guid.NewGuid(), state.Version.Id,
                    blockCode is null ? null : blocks[blockCode].Version.Id, index, Names[index], Purposes[index], durations[index]);
                db.LessonActivities.Add(activity);
                if (index > 0)
                    db.LessonActivityKnowledgeComponents.AddRange(componentIds.Select(componentId =>
                        new LessonActivityKnowledgeComponent(activity.Id, componentId)));
            }
            db.Entry(state.Version).Property(item => item.RowVersion).OriginalValue = expected;
            state.Version.RecordAuthoringChange(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(await BuildViewAsync(state, cancellationToken), LessonStructureFailure.None);
        }
        catch (DbUpdateConcurrencyException) { return Failure(LessonStructureFailure.Conflict); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 or 51311 or 51312 })
        { return Failure(LessonStructureFailure.Conflict); }
    }

    private async Task<State?> LoadStateAsync(Guid teacherId, Guid lessonPlanId, bool noTracking, CancellationToken cancellationToken)
    {
        var query = from plan in db.LessonPlans
                    join version in db.LessonPlanVersions on plan.Id equals version.LessonPlanId
                    where plan.Id == lessonPlanId && plan.TeacherAccountId == teacherId
                        && plan.ArchivedAtUtc == null && version.Status == LessonPlanVersionStatus.Draft
                    select new { Plan = plan, Version = version };
        if (noTracking) query = query.AsNoTracking();
        var item = await query.SingleOrDefaultAsync(cancellationToken);
        if (item is null) return null;
        var participantName = item.Plan.StudentId.HasValue
            ? await db.Students.Where(value => value.Id == item.Plan.StudentId && value.TeacherAccountId == teacherId)
                .Select(value => value.FirstName + " " + value.LastName).SingleAsync(cancellationToken)
            : await db.Groups.Where(value => value.Id == item.Plan.GroupId && value.TeacherAccountId == teacherId)
                .Select(value => value.Name).SingleAsync(cancellationToken);
        return new(item.Plan.Id, item.Version, participantName, item.Plan.StudentId.HasValue ? "student" : "group");
    }

    private async Task<LessonStructureView> BuildViewAsync(State state, CancellationToken cancellationToken)
    {
        var knowledge = await (from mapping in db.LessonPlanVersionKnowledgeComponents
                               join component in db.KnowledgeComponents on mapping.KnowledgeComponentId equals component.Id
                               join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
                               where mapping.LessonPlanVersionId == state.Version.Id
                               orderby component.Name
                               select new LessonStructureKnowledge(component.Id, component.Name, model.Code, model.Version))
            .AsNoTracking().ToListAsync(cancellationToken);
        var persisted = await (from activity in db.LessonActivities
                               join version in db.KnowledgeBlockVersions on activity.KnowledgeBlockVersionId equals version.Id into versions
                               from version in versions.DefaultIfEmpty()
                               join block in db.KnowledgeBlocks on version.KnowledgeBlockId equals block.Id into blocks
                               from block in blocks.DefaultIfEmpty()
                               where activity.LessonPlanVersionId == state.Version.Id
                               orderby activity.SortOrder
                               select new
                               {
                                   Activity = activity,
                                   BlockCode = block == null ? null : block.Code,
                                   BlockName = version == null ? null : version.DisplayName,
                                   Min = version == null ? null : version.MinDurationMinutes,
                                   Max = version == null ? null : version.MaxDurationMinutes
                               })
            .AsNoTracking().ToListAsync(cancellationToken);
        var isApplied = persisted.Count > 0;
        IReadOnlyList<LessonStructureActivity> activities;
        if (isApplied)
        {
            var mappings = await db.LessonActivityKnowledgeComponents.Where(item => persisted.Select(row => row.Activity.Id).Contains(item.LessonActivityId))
                .AsNoTracking().ToListAsync(cancellationToken);
            activities = persisted.Select(row => ToActivity(row.Activity.Id, row.Activity.SortOrder, row.Activity.Name,
                row.Activity.Purpose, row.Activity.DurationMinutes, row.BlockCode, row.BlockName,
                mappings.Where(item => item.LessonActivityId == row.Activity.Id).Select(item => item.KnowledgeComponentId).ToArray(), row.Min, row.Max)).ToArray();
        }
        else
        {
            var durations = LessonStructurePolicy.AllocateDurations(state.Version.TargetDurationMinutes);
            activities = Names.Select((name, index) => ToActivity(Guid.Empty, index, name, Purposes[index], durations[index],
                BlockCodes[index], BlockCodes[index] is null ? null : name, index == 0 ? [] : knowledge.Select(item => item.Id).ToArray(),
                index switch { 0 => 3, 1 => 5, 2 => 8, 3 => 10, _ => null },
                index switch { 0 => 10, 1 => 15, 2 => 25, 3 => 30, _ => null })).ToArray();
        }

        var materialCandidates = await CountMaterialCandidatesAsync(state.Version.Id, knowledge.Select(item => item.Id).ToArray(), cancellationToken);
        var balance = BuildBalance(state.Version.Goal, state.Version.TargetDurationMinutes, knowledge, activities);
        return new(state.PlanId, state.Version.Id, Convert.ToBase64String(state.Version.RowVersion), state.ParticipantName,
            state.ParticipantType, state.Version.Goal, state.Version.TargetDurationMinutes, knowledge, activities,
            balance, materialCandidates, isApplied, !isApplied, PolicyCode);
    }

    private async Task<int> CountMaterialCandidatesAsync(Guid lessonPlanVersionId, Guid[] componentIds, CancellationToken cancellationToken)
    {
        if (componentIds.Length == 0) return 0;
        var teacherId = await (from version in db.LessonPlanVersions
                               join plan in db.LessonPlans on version.LessonPlanId equals plan.Id
                               where version.Id == lessonPlanVersionId
                               select plan.TeacherAccountId).SingleAsync(cancellationToken);
        return await (from material in db.Materials
                      join version in db.MaterialVersions on material.CurrentVersionId equals version.Id
                      join file in db.MaterialFiles on version.Id equals file.MaterialVersionId
                      join mapping in db.MaterialVersionKnowledgeComponents on version.Id equals mapping.MaterialVersionId
                      where material.Status == MaterialStatus.Active && version.Status == MaterialVersionStatus.Active
                          && file.Status == MaterialFileStatus.Clean && componentIds.Contains(mapping.KnowledgeComponentId)
                          && (material.OwnerTeacherId == teacherId || db.MaterialShares.Any(share => share.MaterialId == material.Id
                              && share.SharedWithTeacherId == teacherId && share.Permission == MaterialShareAccess.Use))
                      select material.Id).Distinct().CountAsync(cancellationToken);
    }

    private static LessonStructureActivity ToActivity(Guid id, int sortOrder, string name, LessonActivityPurpose purpose,
        int duration, string? code, string? blockName, IReadOnlyList<Guid> componentIds, int? min, int? max)
    {
        var warnings = new List<string>();
        if (min.HasValue && duration < min) warnings.Add("KB_DURATION_BELOW_RECOMMENDED");
        if (max.HasValue && duration > max) warnings.Add("KB_DURATION_ABOVE_RECOMMENDED");
        return new(id, sortOrder, name, purpose.ToString(), duration, code, blockName, componentIds, warnings);
    }

    private static IReadOnlyList<LessonBalanceArea> BuildBalance(string goal, int target,
        List<LessonStructureKnowledge> knowledge, IReadOnlyList<LessonStructureActivity> activities)
    {
        var mapped = activities.SelectMany(item => item.KnowledgeComponentIds).Distinct().Count();
        var total = activities.Sum(item => item.DurationMinutes);
        var primaryTypes = activities.Where(item => item.KnowledgeBlockCode != null).Select(item => item.KnowledgeBlockCode).Distinct().Count();
        var custom = activities.Any(item => item.KnowledgeBlockCode == null);
        var check = activities.Any(item => item.Purpose == LessonActivityPurpose.CheckForUnderstanding.ToString());
        return
        [
            new("Goal", "Cilj", string.IsNullOrWhiteSpace(goal) ? LessonBalanceStatus.Warning : LessonBalanceStatus.Satisfied,
                string.IsNullOrWhiteSpace(goal) ? "Cilj nije postavljen." : "Plan ima valjan cilj sata."),
            new("KnowledgeCoverage", "Pokrivenost znanja", mapped == knowledge.Count ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                mapped == knowledge.Count ? "Sve odabrane komponente znanja pokrivene su aktivnostima." : "Neke komponente znanja nisu mapirane na aktivnost."),
            new("Time", "Vrijeme", total == target ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                total == target ? $"Planirano trajanje odgovara cilju od {target} min." : $"Plan je {Math.Abs(total - target)} min {(total < target ? "kraći" : "dulji")} od cilja."),
            new("Variety", "Raznolikost", activities.Count >= 3 && (primaryTypes >= 2 || primaryTypes >= 1 && custom)
                ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                activities.Count >= 3 && (primaryTypes >= 2 || primaryTypes >= 1 && custom) ? "Struktura sadrži raznolike vrste aktivnosti." : "Dodajte raznolikije aktivnosti."),
            new("UnderstandingCheck", "Provjera razumijevanja", check ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                check ? "Plan sadrži eksplicitnu provjeru razumijevanja." : "Plan nema provjeru razumijevanja."),
            new("MaterialRelevance", "Relevantnost materijala", LessonBalanceStatus.NotEvaluated,
                "Relevantnost se ne procjenjuje prije Phase 9.6; prikazana je samo dostupnost kandidata."),
        ];
    }

    private static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length == 8; }
        catch (FormatException) { bytes = []; return false; }
    }
    private static LessonStructureResult Failure(LessonStructureFailure failure) => new(null, failure);
    private sealed record State(Guid PlanId, LessonPlanVersion Version, string ParticipantName, string ParticipantType);
}
