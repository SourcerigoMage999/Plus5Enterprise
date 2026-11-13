using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonPlanConfirmationService(Plus5DbContext db)
    : ILessonPlanConfirmationService
{
    public async Task<LessonPlanConfirmationResult> GetAsync(
        Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty)
            return Failure(LessonPlanConfirmationFailure.Invalid);

        var state = await LoadStateAsync(teacherId, lessonPlanId, cancellationToken);
        return state is null
            ? Failure(LessonPlanConfirmationFailure.NotFound)
            : new(await BuildViewAsync(state, cancellationToken), LessonPlanConfirmationFailure.None);
    }

    public async Task<LessonPlanConfirmationResult> ConfirmAsync(
        Guid teacherId,
        Guid lessonPlanId,
        string planRowVersion,
        string versionRowVersion,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty
            || !TryRowVersion(planRowVersion, out var expectedPlan)
            || !TryRowVersion(versionRowVersion, out var expectedVersion))
            return Failure(LessonPlanConfirmationFailure.Invalid);

        var state = await LoadStateAsync(teacherId, lessonPlanId, cancellationToken);
        if (state is null) return Failure(LessonPlanConfirmationFailure.NotFound);
        if (state.Version.Status != LessonPlanVersionStatus.Draft)
            return Failure(LessonPlanConfirmationFailure.NotDraft);

        var validation = await ValidateAsync(state.Version, cancellationToken);
        if (validation.Count > 0)
            return new(await BuildViewAsync(state, cancellationToken), LessonPlanConfirmationFailure.ValidationFailed);

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var locked = await db.Database.ExecuteSqlRawAsync(
                "UPDATE [LessonPlans] SET [LifecycleStatus] = [LifecycleStatus] WHERE [Id] = {0} AND [TeacherAccountId] = {1} AND [ArchivedAtUtc] IS NULL AND [RowVersion] = {2}",
                [lessonPlanId, teacherId, expectedPlan], cancellationToken);
            if (locked != 1) return Failure(LessonPlanConfirmationFailure.Conflict);

            if (state.Plan.CurrentReadyVersionId is Guid previousReadyId)
            {
                var superseded = await db.Database.ExecuteSqlRawAsync(
                    "UPDATE [LessonPlanVersions] SET [Status] = 3 WHERE [Id] = {0} AND [LessonPlanId] = {1} AND [Status] = 2",
                    [previousReadyId, lessonPlanId], cancellationToken);
                if (superseded != 1) return Failure(LessonPlanConfirmationFailure.Conflict);
            }

            var ready = await db.Database.ExecuteSqlRawAsync(
                "UPDATE [LessonPlanVersions] SET [Status] = 2 WHERE [Id] = {0} AND [LessonPlanId] = {1} AND [Status] = 1 AND [RowVersion] = {2}",
                [state.Version.Id, lessonPlanId, expectedVersion], cancellationToken);
            if (ready != 1) return Failure(LessonPlanConfirmationFailure.Conflict);

            var pointed = await db.Database.ExecuteSqlRawAsync(
                "UPDATE [LessonPlans] SET [CurrentReadyVersionId] = {0} WHERE [Id] = {1} AND [TeacherAccountId] = {2} AND [ArchivedAtUtc] IS NULL",
                [state.Version.Id, lessonPlanId, teacherId], cancellationToken);
            if (pointed != 1) return Failure(LessonPlanConfirmationFailure.Conflict);

            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return await GetAsync(teacherId, lessonPlanId, cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51303 or 51350 or 51351)
        {
            return Failure(exception.Number == 51350
                ? LessonPlanConfirmationFailure.ValidationFailed
                : LessonPlanConfirmationFailure.Conflict);
        }
    }

    public async Task<LessonPlanConfirmationResult> CreateEditableDraftAsync(
        Guid teacherId,
        Guid lessonPlanId,
        string planRowVersion,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty || lessonPlanId == Guid.Empty
            || !TryRowVersion(planRowVersion, out var expectedPlan))
            return Failure(LessonPlanConfirmationFailure.Invalid);

        var state = await LoadStateAsync(teacherId, lessonPlanId, cancellationToken);
        if (state is null) return Failure(LessonPlanConfirmationFailure.NotFound);
        if (state.Version.Status == LessonPlanVersionStatus.Draft)
            return new(await BuildViewAsync(state, cancellationToken), LessonPlanConfirmationFailure.None);
        if (state.Version.Status != LessonPlanVersionStatus.Ready)
            return Failure(LessonPlanConfirmationFailure.Conflict);

        var newVersionId = Guid.NewGuid();
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var locked = await db.Database.ExecuteSqlRawAsync(
                "UPDATE [LessonPlans] SET [LifecycleStatus] = [LifecycleStatus] WHERE [Id] = {0} AND [TeacherAccountId] = {1} AND [ArchivedAtUtc] IS NULL AND [RowVersion] = {2}",
                [lessonPlanId, teacherId, expectedPlan], cancellationToken);
            if (locked != 1) return Failure(LessonPlanConfirmationFailure.Conflict);

            var copied = await db.Database.ExecuteSqlRawAsync(CloneSql,
                [newVersionId, state.Version.Id, lessonPlanId], cancellationToken);
            if (copied == 0) return Failure(LessonPlanConfirmationFailure.Conflict);

            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return await GetAsync(teacherId, lessonPlanId, cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51311 or 51312 or 51316)
        {
            return Failure(LessonPlanConfirmationFailure.Conflict);
        }
    }

    private async Task<State?> LoadStateAsync(Guid teacherId, Guid lessonPlanId, CancellationToken cancellationToken)
    {
        var plan = await db.LessonPlans.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == lessonPlanId
                && item.TeacherAccountId == teacherId && item.ArchivedAtUtc == null, cancellationToken);
        if (plan is null) return null;

        var version = await db.LessonPlanVersions.AsNoTracking()
            .Where(item => item.LessonPlanId == lessonPlanId
                && (item.Status == LessonPlanVersionStatus.Draft
                    || item.Id == plan.CurrentReadyVersionId))
            .OrderBy(item => item.Status == LessonPlanVersionStatus.Draft ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);
        return version is null ? null : new(plan, version);
    }

    private async Task<LessonPlanConfirmationView> BuildViewAsync(State state, CancellationToken cancellationToken)
    {
        var plan = state.Plan;
        var version = state.Version;
        var participantName = plan.StudentId.HasValue
            ? await db.Students.AsNoTracking().Where(item => item.Id == plan.StudentId)
                .Select(item => item.FirstName + " " + item.LastName).SingleAsync(cancellationToken)
            : await db.Groups.AsNoTracking().Where(item => item.Id == plan.GroupId)
                .Select(item => item.Name).SingleAsync(cancellationToken);
        LessonPlanEditorSession? session = null;
        if (plan.SessionId is Guid sessionId)
        {
            session = await db.Sessions.AsNoTracking().Where(item => item.Id == sessionId)
                .Select(item => new LessonPlanEditorSession(item.Id, item.StartsAtUtc,
                    item.EndsAtUtc, item.TimeZoneId, item.Title)).SingleAsync(cancellationToken);
        }

        var knowledge = await (from mapping in db.LessonPlanVersionKnowledgeComponents.AsNoTracking()
                               join component in db.KnowledgeComponents.AsNoTracking() on mapping.KnowledgeComponentId equals component.Id
                               join model in db.KnowledgeModels.AsNoTracking() on component.KnowledgeModelId equals model.Id
                               where mapping.LessonPlanVersionId == version.Id
                               orderby component.Name
                               select new LessonStructureKnowledge(component.Id, component.Name, model.Code, model.Version))
            .ToListAsync(cancellationToken);
        var rows = await db.LessonActivities.AsNoTracking()
            .Where(item => item.LessonPlanVersionId == version.Id)
            .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var activityIds = rows.Select(item => item.Id).ToArray();
        var activityKnowledge = await db.LessonActivityKnowledgeComponents.AsNoTracking()
            .Where(item => activityIds.Contains(item.LessonActivityId)).ToListAsync(cancellationToken);
        var attachments = await (from mapping in db.LessonActivityMaterialVersions.AsNoTracking()
                                 join activity in db.LessonActivities.AsNoTracking() on mapping.LessonActivityId equals activity.Id
                                 join materialVersion in db.MaterialVersions.AsNoTracking() on mapping.MaterialVersionId equals materialVersion.Id
                                 join material in db.Materials.AsNoTracking() on materialVersion.MaterialId equals material.Id
                                 where activityIds.Contains(activity.Id)
                                 select new { activity.Id, Material = material, Version = materialVersion })
            .ToListAsync(cancellationToken);
        var materialIds = attachments.Select(item => item.Version.Id).Distinct().ToArray();
        var materialKnowledge = await db.MaterialVersionKnowledgeComponents.AsNoTracking()
            .Where(item => materialIds.Contains(item.MaterialVersionId)).ToListAsync(cancellationToken);

        var activities = rows.Select(activity =>
        {
            var componentIds = activityKnowledge.Where(item => item.LessonActivityId == activity.Id)
                .Select(item => item.KnowledgeComponentId).ToHashSet();
            var materials = attachments.Where(item => item.Id == activity.Id)
                .Select(item => new LessonActivityMaterialAttachment(item.Material.Id, item.Version.Id,
                    item.Version.VersionNumber, item.Version.Title, item.Version.MaterialTypeCode,
                    item.Version.Subject, item.Material.CurrentVersionId == item.Version.Id,
                    item.Material.OwnerTeacherId == plan.TeacherAccountId,
                    materialKnowledge.Count(mapping => mapping.MaterialVersionId == item.Version.Id
                        && componentIds.Contains(mapping.KnowledgeComponentId))))
                .OrderBy(item => item.Title).ToList();
            return new LessonPlanConfirmationActivity(activity.Id, activity.SortOrder, activity.Name,
                activity.Purpose.ToString(), activity.WorkMode.ToString(), activity.DurationMinutes, 0, materials);
        }).ToList();
        var allMaterials = activities.SelectMany(item => item.Materials)
            .GroupBy(item => item.MaterialVersionId).Select(group => group.First())
            .OrderBy(item => item.Title).ToList();
        var validation = await ValidateAsync(version, cancellationToken);
        var planned = activities.Sum(item => item.DurationMinutes);
        var balance = BuildBalance(version.TargetDurationMinutes, planned, knowledge.Count, rows, activityKnowledge);

        return new(plan.Id, version.Id, version.VersionNumber, version.Status.ToString(),
            Convert.ToBase64String(plan.RowVersion), Convert.ToBase64String(version.RowVersion),
            participantName, plan.StudentId.HasValue ? "student" : "group", session, version.Goal,
            version.TargetDurationMinutes, planned, knowledge, activities, allMaterials, balance,
            validation, version.Status == LessonPlanVersionStatus.Draft && validation.Count == 0,
            version.Status is LessonPlanVersionStatus.Draft or LessonPlanVersionStatus.Ready, false);
    }

    private async Task<List<string>> ValidateAsync(LessonPlanVersion version, CancellationToken cancellationToken)
    {
        var messages = new List<string>();
        var activities = await db.LessonActivities.AsNoTracking()
            .Where(item => item.LessonPlanVersionId == version.Id)
            .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        if (activities.Count == 0) messages.Add("Dodajte najmanje jednu aktivnost.");
        if (activities.Select((item, index) => item.SortOrder == index).Any(valid => !valid))
            messages.Add("Redoslijed aktivnosti mora biti neprekinut.");
        if (activities.Sum(item => item.DurationMinutes) <= 0)
            messages.Add("Ukupno trajanje mora biti veće od nule.");
        var activityIds = activities.Select(item => item.Id).ToArray();
        var invalidMaterial = await (from mapping in db.LessonActivityMaterialVersions.AsNoTracking()
                                     join activity in db.LessonActivities.AsNoTracking() on mapping.LessonActivityId equals activity.Id
                                     where activityIds.Contains(activity.Id)
                                         && !db.MaterialFiles.Any(file => file.MaterialVersionId == mapping.MaterialVersionId
                                             && file.Status == MaterialFileStatus.Clean)
                                     select mapping.MaterialVersionId).AnyAsync(cancellationToken);
        if (invalidMaterial) messages.Add("Svi povezani materijali moraju imati Clean datoteku.");
        return messages;
    }

    private static IReadOnlyList<LessonBalanceArea> BuildBalance(int target, int planned, int knowledgeCount,
        IReadOnlyList<LessonActivity> activities, IReadOnlyList<LessonActivityKnowledgeComponent> mappings)
    {
        var covered = mappings.Select(item => item.KnowledgeComponentId).Distinct().Count();
        return
        [
            new("Time", "Vrijeme", planned == target ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                planned == target ? $"Planirano trajanje odgovara cilju od {target} min." : $"Planirano je {planned} od {target} min."),
            new("KnowledgeCoverage", "Pokrivenost znanja", covered == knowledgeCount ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                covered == knowledgeCount ? "Sve komponente znanja pokrivene su aktivnostima." : "Neke komponente znanja nisu pokrivene."),
            new("UnderstandingCheck", "Provjera razumijevanja", activities.Any(item => item.Purpose == LessonActivityPurpose.CheckForUnderstanding)
                ? LessonBalanceStatus.Satisfied : LessonBalanceStatus.Warning,
                activities.Any(item => item.Purpose == LessonActivityPurpose.CheckForUnderstanding)
                    ? "Plan sadrži provjeru razumijevanja." : "Plan nema eksplicitnu provjeru razumijevanja."),
        ];
    }

    private static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length == 8; }
        catch (FormatException) { bytes = []; return false; }
    }

    private static LessonPlanConfirmationResult Failure(LessonPlanConfirmationFailure failure) => new(null, failure);
    private sealed record State(LessonPlan Plan, LessonPlanVersion Version);

    private const string CloneSql = """
        DECLARE @Now datetimeoffset(7) = SYSUTCDATETIME();
        DECLARE @ActivityMap TABLE (OldId uniqueidentifier NOT NULL, NewId uniqueidentifier NOT NULL);

        INSERT INTO [LessonPlanVersions] ([Id], [LessonPlanId], [VersionNumber], [Status], [Goal], [TargetDurationMinutes], [CreatedAtUtc], [UpdatedAtUtc])
        SELECT {0}, [LessonPlanId], (SELECT MAX([VersionNumber]) + 1 FROM [LessonPlanVersions] WHERE [LessonPlanId] = {2}), 1,
               [Goal], [TargetDurationMinutes], @Now, @Now
        FROM [LessonPlanVersions] WHERE [Id] = {1} AND [LessonPlanId] = {2} AND [Status] = 2;

        INSERT INTO [LessonPlanVersionKnowledgeComponents] ([LessonPlanVersionId], [KnowledgeComponentId])
        SELECT {0}, [KnowledgeComponentId] FROM [LessonPlanVersionKnowledgeComponents] WHERE [LessonPlanVersionId] = {1};

        MERGE [LessonActivities] AS target
        USING (SELECT [Id] AS [OldId], NEWID() AS [NewId], [KnowledgeBlockVersionId], [SortOrder], [Name], [Purpose],
                      [DurationMinutes], [WorkMode], [Description], [PurposeDescription], [TeacherInstructions],
                      [StudentInstructions], [Notes], [ShowTeacherInstructionsOnBoard], [ShowStudentInstructionsOnBoard], [ShowProgressOnBoard]
               FROM [LessonActivities] WHERE [LessonPlanVersionId] = {1}) AS source
        ON 1 = 0
        WHEN NOT MATCHED THEN INSERT ([Id], [LessonPlanVersionId], [KnowledgeBlockVersionId], [SortOrder], [Name], [Purpose],
            [DurationMinutes], [WorkMode], [Description], [PurposeDescription], [TeacherInstructions], [StudentInstructions],
            [Notes], [ShowTeacherInstructionsOnBoard], [ShowStudentInstructionsOnBoard], [ShowProgressOnBoard])
        VALUES (source.[NewId], {0}, source.[KnowledgeBlockVersionId], source.[SortOrder], source.[Name], source.[Purpose],
            source.[DurationMinutes], source.[WorkMode], source.[Description], source.[PurposeDescription], source.[TeacherInstructions],
            source.[StudentInstructions], source.[Notes], source.[ShowTeacherInstructionsOnBoard], source.[ShowStudentInstructionsOnBoard], source.[ShowProgressOnBoard])
        OUTPUT source.[OldId], inserted.[Id] INTO @ActivityMap ([OldId], [NewId]);

        INSERT INTO [LessonActivityKnowledgeComponents] ([LessonActivityId], [KnowledgeComponentId])
        SELECT map.[NewId], mapping.[KnowledgeComponentId]
        FROM [LessonActivityKnowledgeComponents] mapping JOIN @ActivityMap map ON map.[OldId] = mapping.[LessonActivityId];

        INSERT INTO [LessonActivityMaterialVersions] ([LessonActivityId], [MaterialVersionId])
        SELECT map.[NewId], mapping.[MaterialVersionId]
        FROM [LessonActivityMaterialVersions] mapping JOIN @ActivityMap map ON map.[OldId] = mapping.[LessonActivityId];
        """;
}
