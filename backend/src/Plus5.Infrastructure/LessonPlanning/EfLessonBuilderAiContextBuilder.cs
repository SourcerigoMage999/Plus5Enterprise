using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.LessonPlanning;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.LessonPlanning;

public sealed class EfLessonBuilderAiContextBuilder(Plus5DbContext db)
    : ILessonBuilderAiContextBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LessonBuilderAiContext?> BuildAsync(
        Guid teacherId, Guid lessonPlanId, Guid activityId, CancellationToken cancellationToken)
    {
        var row = await (from plan in db.LessonPlans.AsNoTracking()
                         join version in db.LessonPlanVersions.AsNoTracking() on plan.Id equals version.LessonPlanId
                         join activity in db.LessonActivities.AsNoTracking() on version.Id equals activity.LessonPlanVersionId
                         join blockVersion in db.KnowledgeBlockVersions.AsNoTracking()
                             on activity.KnowledgeBlockVersionId equals blockVersion.Id into blockVersions
                         from blockVersion in blockVersions.DefaultIfEmpty()
                         join block in db.KnowledgeBlocks.AsNoTracking()
                             on blockVersion.KnowledgeBlockId equals block.Id into blocks
                         from block in blocks.DefaultIfEmpty()
                         where plan.Id == lessonPlanId && plan.TeacherAccountId == teacherId
                             && plan.ArchivedAtUtc == null && version.Status == LessonPlanVersionStatus.Draft
                             && activity.Id == activityId
                         select new
                         {
                             version.Goal,
                             ParticipantType = plan.StudentId.HasValue ? "Student" : "Group",
                             Activity = activity,
                             KnowledgeBlockCode = block == null ? null : block.Code,
                             KnowledgeBlockName = blockVersion == null ? null : blockVersion.DisplayName,
                         }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var components = await (from mapping in db.LessonActivityKnowledgeComponents.AsNoTracking()
                                join component in db.KnowledgeComponents.AsNoTracking()
                                    on mapping.KnowledgeComponentId equals component.Id
                                join model in db.KnowledgeModels.AsNoTracking()
                                    on component.KnowledgeModelId equals model.Id
                                where mapping.LessonActivityId == activityId
                                orderby model.Code, model.Version, component.SortOrder
                                select new { component.Name, model.Code, model.Version })
            .ToListAsync(cancellationToken);

        // Deliberately excludes Teacher identity, Student/Group identity, Notes,
        // materials, attempts, evidence, mastery and readiness.
        var context = new
        {
            lessonGoal = row.Goal,
            participantType = row.ParticipantType,
            knowledgeBlock = row.KnowledgeBlockCode is null ? null : new
            {
                code = row.KnowledgeBlockCode,
                name = row.KnowledgeBlockName,
            },
            knowledgeComponents = components.Select(item => new
            {
                name = item.Name,
                modelCode = item.Code,
                modelVersion = item.Version,
            }),
            currentActivity = new
            {
                row.Activity.Name,
                purpose = row.Activity.PurposeDescription,
                row.Activity.Description,
                row.Activity.TeacherInstructions,
                row.Activity.StudentInstructions,
                workMode = row.Activity.WorkMode.ToString(),
            },
        };
        return new(JsonSerializer.Serialize(context, JsonOptions));
    }
}
