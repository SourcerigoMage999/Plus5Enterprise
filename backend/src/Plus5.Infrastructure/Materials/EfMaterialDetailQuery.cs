using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Evidence;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialDetailQuery(Plus5DbContext dbContext) : IMaterialDetailQuery
{
    public async Task<MaterialDetail?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(materialId, Guid.Empty);

        var candidate = await (
            from material in dbContext.Materials.AsNoTracking()
            join version in dbContext.MaterialVersions.AsNoTracking()
                on material.CurrentVersionId equals version.Id
            join file in dbContext.MaterialFiles.AsNoTracking()
                on version.Id equals file.MaterialVersionId
            join programCandidate in dbContext.Programs.AsNoTracking()
                on version.ProgramId equals (Guid?)programCandidate.Id into programs
            from program in programs.DefaultIfEmpty()
            join gradeCandidate in dbContext.SchoolGrades.AsNoTracking()
                on version.SchoolGradeId equals (Guid?)gradeCandidate.Id into grades
            from grade in grades.DefaultIfEmpty()
            join levelCandidate in dbContext.ProficiencyLevels.AsNoTracking()
                on version.ProficiencyLevelId equals (Guid?)levelCandidate.Id into levels
            from level in levels.DefaultIfEmpty()
            where material.Id == materialId
                && material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Active
                && file.Status == MaterialFileStatus.Clean
                && (material.OwnerTeacherId == teacherAccountId
                    || (material.Visibility == MaterialVisibility.Shared
                        && dbContext.MaterialShares.Any(share =>
                            share.MaterialId == material.Id
                            && share.SharedWithTeacherId == teacherAccountId)))
            select new
            {
                material,
                version,
                file,
                ProgramId = program == null ? (Guid?)null : program.Id,
                ProgramName = program == null ? null : program.Name,
                SchoolGradeId = grade == null ? (Guid?)null : grade.Id,
                SchoolGradeCode = grade == null ? null : grade.Code,
                SchoolGradeName = grade == null ? null : grade.Name,
                ProficiencyLevelId = level == null ? (Guid?)null : level.Id,
                ProficiencyLevelCode = level == null ? null : level.Code,
                ProficiencyLevelName = level == null ? null : level.Name,
            }).SingleOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var isOwner = candidate.material.OwnerTeacherId == teacherAccountId;
        var sharePermission = isOwner
            ? null
            : await dbContext.MaterialShares
                .AsNoTracking()
                .Where(share => share.MaterialId == materialId
                    && share.SharedWithTeacherId == teacherAccountId)
                .Select(share => (int?)share.Permission)
                .SingleOrDefaultAsync(cancellationToken);
        if (!isOwner && !sharePermission.HasValue)
        {
            return null;
        }

        var shareAccess = sharePermission.HasValue
            ? (MaterialLibraryShareAccess?)sharePermission.Value
            : null;

        var tags = await dbContext.MaterialVersionTags
            .AsNoTracking()
            .Where(tag => tag.MaterialVersionId == candidate.version.Id)
            .OrderBy(tag => tag.Name)
            .Select(tag => tag.Name)
            .ToListAsync(cancellationToken);

        var knowledgeComponents = await (
            from mapping in dbContext.MaterialVersionKnowledgeComponents.AsNoTracking()
            join component in dbContext.KnowledgeComponents.AsNoTracking()
                on mapping.KnowledgeComponentId equals component.Id
            join area in dbContext.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in dbContext.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            where mapping.MaterialVersionId == candidate.version.Id
            orderby model.Code, model.Version, area.SortOrder, component.SortOrder, component.Name
            select new
            {
                component.Id,
                component.Name,
                KnowledgeAreaName = area.Name,
                model.Code,
                model.Version,
                model.Status,
            })
            .ToListAsync(cancellationToken);

        var curriculumOutcomes = await (
            from mapping in dbContext.MaterialVersionCurriculumOutcomes.AsNoTracking()
            join outcome in dbContext.CurriculumOutcomes.AsNoTracking()
                on mapping.CurriculumOutcomeId equals outcome.Id
            join curriculum in dbContext.Curricula.AsNoTracking()
                on outcome.CurriculumId equals curriculum.Id
            where mapping.MaterialVersionId == candidate.version.Id
            orderby curriculum.Code, curriculum.Version, outcome.SortOrder, outcome.Title
            select new MaterialDetailCurriculumOutcome(
                outcome.Id,
                outcome.OfficialCode,
                outcome.Title,
                curriculum.Code,
                curriculum.Name,
                curriculum.Version))
            .ToListAsync(cancellationToken);

        var taskRows = await (
            from taskVersion in dbContext.AssessableTaskVersions.AsNoTracking()
            join task in dbContext.AssessableTasks.AsNoTracking()
                on taskVersion.AssessableTaskId equals task.Id
            where taskVersion.MaterialVersionId == candidate.version.Id
                && task.MaterialId == candidate.material.Id
            orderby taskVersion.SortOrder, taskVersion.Id
            select new
            {
                TaskId = task.Id,
                TaskVersionId = taskVersion.Id,
                taskVersion.VersionNumber,
                taskVersion.SortOrder,
                taskVersion.Prompt,
                taskVersion.TaskTypeCode,
                taskVersion.Difficulty,
                taskVersion.EvidenceType,
                taskVersion.CorrectAnswer,
                taskVersion.EvaluationCriterion,
                taskVersion.MaxPoints,
            })
            .ToListAsync(cancellationToken);

        var taskVersionIds = taskRows.Select(task => task.TaskVersionId).ToList();
        var taskKnowledgeRows = taskVersionIds.Count == 0
            ? []
            : await (
                from mapping in dbContext.AssessableTaskVersionKnowledgeComponents.AsNoTracking()
                join component in dbContext.KnowledgeComponents.AsNoTracking()
                    on mapping.KnowledgeComponentId equals component.Id
                join area in dbContext.KnowledgeAreas.AsNoTracking()
                    on component.KnowledgeAreaId equals area.Id
                join model in dbContext.KnowledgeModels.AsNoTracking()
                    on component.KnowledgeModelId equals model.Id
                where taskVersionIds.Contains(mapping.AssessableTaskVersionId)
                orderby model.Code, model.Version, area.SortOrder, component.SortOrder, component.Name
                select new
                {
                    mapping.AssessableTaskVersionId,
                    component.Id,
                    component.Name,
                    KnowledgeAreaName = area.Name,
                    model.Code,
                    model.Version,
                    model.Status,
                })
                .ToListAsync(cancellationToken);

        var tasks = taskRows.Select(task => new MaterialDetailTask(
            task.TaskId,
            task.TaskVersionId,
            task.VersionNumber,
            task.SortOrder,
            task.Prompt,
            task.TaskTypeCode,
            task.Difficulty,
            task.EvidenceType switch
            {
                EvidenceType.Recognition => MaterialDetailEvidenceType.Recognition,
                EvidenceType.Understanding => MaterialDetailEvidenceType.Understanding,
                EvidenceType.Application => MaterialDetailEvidenceType.Application,
                EvidenceType.Production => MaterialDetailEvidenceType.Production,
                _ => throw new InvalidOperationException("Unknown task evidence type."),
            },
            task.CorrectAnswer,
            task.EvaluationCriterion,
            task.MaxPoints,
            taskKnowledgeRows
                .Where(mapping => mapping.AssessableTaskVersionId == task.TaskVersionId)
                .Select(mapping => new MaterialDetailKnowledgeComponent(
                    mapping.Id,
                    mapping.Name,
                    mapping.KnowledgeAreaName,
                    mapping.Code,
                    mapping.Version,
                    mapping.Status switch
                    {
                        KnowledgeModelStatus.Published => MaterialDetailKnowledgeModelStatus.Published,
                        KnowledgeModelStatus.Retired => MaterialDetailKnowledgeModelStatus.Retired,
                        _ => throw new InvalidOperationException(
                            "Material detail cannot expose a draft knowledge model."),
                    }))
                .ToList()))
            .ToList();

        return new MaterialDetail(
            candidate.material.Id,
            candidate.version.Id,
            candidate.version.VersionNumber,
            candidate.version.Title,
            candidate.version.Description,
            candidate.version.MaterialTypeCode,
            candidate.version.Subject,
            candidate.version.LanguageCode,
            candidate.ProgramId.HasValue && candidate.ProgramName is not null
                ? new MaterialDetailReference(candidate.ProgramId.Value, candidate.ProgramName, null)
                : null,
            candidate.SchoolGradeId.HasValue && candidate.SchoolGradeName is not null
                ? new MaterialDetailReference(
                    candidate.SchoolGradeId.Value,
                    candidate.SchoolGradeName,
                    candidate.SchoolGradeCode)
                : null,
            candidate.ProficiencyLevelId.HasValue && candidate.ProficiencyLevelName is not null
                ? new MaterialDetailReference(
                    candidate.ProficiencyLevelId.Value,
                    candidate.ProficiencyLevelName,
                    candidate.ProficiencyLevelCode)
                : null,
            candidate.version.LearningGoal,
            new MaterialDetailFile(
                (MaterialLibraryFileFormat)candidate.file.Format,
                candidate.file.OriginalFileName,
                candidate.file.DeclaredMediaType,
                candidate.file.ActualSizeBytes ?? candidate.file.DeclaredSizeBytes),
            candidate.version.ActivatedAtUtc ?? candidate.version.CreatedAtUtc,
            isOwner,
            shareAccess,
            tags,
            knowledgeComponents.Select(component => new MaterialDetailKnowledgeComponent(
                component.Id,
                component.Name,
                component.KnowledgeAreaName,
                component.Code,
                component.Version,
                component.Status switch
                {
                    KnowledgeModelStatus.Published => MaterialDetailKnowledgeModelStatus.Published,
                    KnowledgeModelStatus.Retired => MaterialDetailKnowledgeModelStatus.Retired,
                    _ => throw new InvalidOperationException(
                        "Material detail cannot expose a draft knowledge model."),
                })).ToList(),
            curriculumOutcomes,
            tasks);
    }
}
