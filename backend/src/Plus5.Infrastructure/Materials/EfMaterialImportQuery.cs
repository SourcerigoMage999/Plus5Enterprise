using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialImportQuery(Plus5DbContext db) : IMaterialImportQuery
{
    private static readonly string[] MaterialTypes =
    [
        "PRESENTATION", "WORKSHEET", "CONVERSATION_CARDS", "INTERACTIVE_EXERCISE",
        "VIDEO", "AUDIO", "QUIZ", "IMAGE", "POSTER", "MAP", "OTHER",
    ];

    private static readonly string[] Languages = ["HR", "EN", "DE", "IT", "ES", "FR"];

    public async Task<MaterialImportOptions> GetOptionsAsync(
        Guid teacherAccountId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);

        var programs = await db.Programs.AsNoTracking()
            .Where(program => program.TeacherAccountId == teacherAccountId)
            .OrderBy(program => program.Name)
            .Select(program => new MaterialImportReference(program.Id, program.Name, null))
            .ToListAsync(cancellationToken);
        var grades = await db.SchoolGrades.AsNoTracking()
            .OrderBy(grade => grade.SortOrder)
            .Select(grade => new MaterialImportReference(grade.Id, grade.Name, grade.Code))
            .ToListAsync(cancellationToken);
        var levels = await db.ProficiencyLevels.AsNoTracking()
            .OrderBy(level => level.FrameworkCode).ThenBy(level => level.SortOrder)
            .Select(level => new MaterialImportReference(level.Id, level.Name, level.Code))
            .ToListAsync(cancellationToken);
        var components = await (
            from component in db.KnowledgeComponents.AsNoTracking()
            join area in db.KnowledgeAreas.AsNoTracking()
                on component.KnowledgeAreaId equals area.Id
            join model in db.KnowledgeModels.AsNoTracking()
                on component.KnowledgeModelId equals model.Id
            where model.Status != KnowledgeModelStatus.Draft
                && component.Status == KnowledgeComponentStatus.Active
            orderby model.Code, model.Version, area.SortOrder, component.SortOrder, component.Name
            select new MaterialImportKnowledgeComponent(
                component.Id,
                component.Name,
                area.Name,
                model.Code,
                model.Version))
            .ToListAsync(cancellationToken);
        var outcomes = await (
            from outcome in db.CurriculumOutcomes.AsNoTracking()
            join curriculum in db.Curricula.AsNoTracking()
                on outcome.CurriculumId equals curriculum.Id
            orderby curriculum.Code, curriculum.Version, outcome.SortOrder, outcome.Title
            select new MaterialImportCurriculumOutcome(
                outcome.Id,
                outcome.Title,
                outcome.OfficialCode,
                curriculum.Code,
                curriculum.Version))
            .ToListAsync(cancellationToken);

        return new MaterialImportOptions(
            programs,
            grades,
            levels,
            components,
            outcomes,
            MaterialTypes,
            Languages);
    }
}
