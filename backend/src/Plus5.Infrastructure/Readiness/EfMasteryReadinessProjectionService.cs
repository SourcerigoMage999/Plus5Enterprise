using Microsoft.EntityFrameworkCore;
using Plus5.Application.Readiness;
using Plus5.Domain.Evidence;
using Plus5.Domain.Readiness;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Readiness;

public sealed class EfMasteryReadinessProjectionService(
    Plus5DbContext db,
    TimeProvider clock) : IMasteryReadinessProjectionService
{
    public Task RecalculateAffectedAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> knowledgeComponentIds,
        CancellationToken cancellationToken) =>
        MasteryProjectionWriter.RecalculateAsync(
            db,
            studentId,
            knowledgeComponentIds,
            clock.GetUtcNow(),
            cancellationToken);
}

internal static class MasteryProjectionWriter
{
    public static async Task RecalculateAsync(
        Plus5DbContext db,
        Guid studentId,
        IReadOnlyCollection<Guid> affectedKnowledgeComponentIds,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student identifier is required.", nameof(studentId));
        }

        ArgumentNullException.ThrowIfNull(affectedKnowledgeComponentIds);
        if (affectedKnowledgeComponentIds.Count == 0)
        {
            return;
        }

        var components = await db.KnowledgeComponents.AsNoTracking()
            .Select(component => new ComponentNode(
                component.Id,
                component.KnowledgeAreaId,
                component.ParentComponentId))
            .ToListAsync(cancellationToken);
        var componentById = components.ToDictionary(component => component.Id);
        if (affectedKnowledgeComponentIds.Any(id => !componentById.ContainsKey(id)))
        {
            throw new InvalidOperationException("Affected knowledge component no longer exists.");
        }

        var childrenByParent = components
            .Where(component => component.ParentId.HasValue)
            .GroupBy(component => component.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var affectedPath = BuildAffectedPath(affectedKnowledgeComponentIds, componentById);
        var affectedAreaIds = affectedPath
            .Select(id => componentById[id].AreaId)
            .Distinct()
            .ToArray();

        var effectiveRows = await (
            from evidence in db.EvidenceEvents.AsNoTracking()
            join mapping in db.EvidenceEventKnowledgeComponents.AsNoTracking()
                on evidence.Id equals mapping.EvidenceEventId
            where evidence.StudentId == studentId
                && evidence.Kind != EvidenceEventKind.Invalidation
                && !db.EvidenceEvents.Any(successor =>
                    successor.SupersedesEvidenceEventId == evidence.Id)
            select new EffectiveEvidenceRow(
                evidence.Id,
                mapping.KnowledgeComponentId,
                evidence.PerformanceScore,
                evidence.Difficulty,
                evidence.EvidenceType,
                evidence.AssistanceLevel,
                evidence.EvidenceContext,
                evidence.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var evidenceByComponent = effectiveRows
            .GroupBy(row => row.KnowledgeComponentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<MasteryEvidenceInput>)group.Select(ToInput).ToArray());
        var calculatedComponents = new Dictionary<Guid, MasteryCalculation>();
        var visiting = new HashSet<Guid>();

        MasteryCalculation CalculateComponent(Guid componentId)
        {
            if (calculatedComponents.TryGetValue(componentId, out var existing))
            {
                return existing;
            }

            if (!visiting.Add(componentId))
            {
                throw new InvalidOperationException("Knowledge component hierarchy contains a cycle.");
            }

            MasteryCalculation calculation;
            if (!childrenByParent.TryGetValue(componentId, out var children))
            {
                evidenceByComponent.TryGetValue(componentId, out var evidence);
                calculation = MasteryReadinessCalculator.CalculateLeaf(
                    evidence ?? [],
                    calculatedAtUtc);
            }
            else
            {
                calculation = MasteryReadinessCalculator.AggregateChildren(
                    children.Select(child => CalculateComponent(child.Id)).ToArray());
            }

            visiting.Remove(componentId);
            calculatedComponents[componentId] = calculation;
            return calculation;
        }

        foreach (var component in components)
        {
            CalculateComponent(component.Id);
        }

        await UpsertComponentsAsync(
            db,
            studentId,
            affectedPath,
            calculatedComponents,
            calculatedAtUtc,
            cancellationToken);

        var areaCalculations = components
            .Where(component => affectedAreaIds.Contains(component.AreaId)
                && component.ParentId == null)
            .GroupBy(component => component.AreaId)
            .ToDictionary(
                group => group.Key,
                group => MasteryReadinessCalculator.AggregateChildren(
                    group.Select(component => calculatedComponents[component.Id]).ToArray()));
        await UpsertAreasAsync(
            db,
            studentId,
            areaCalculations,
            calculatedAtUtc,
            cancellationToken);

        var outcomeMappings = await db.CurriculumOutcomeKnowledgeComponents.AsNoTracking()
            .Select(mapping => new OutcomeMapping(
                mapping.CurriculumOutcomeId,
                mapping.KnowledgeComponentId))
            .ToListAsync(cancellationToken);
        var affectedOutcomeIds = outcomeMappings
            .Where(mapping => affectedPath.Contains(mapping.KnowledgeComponentId))
            .Select(mapping => mapping.CurriculumOutcomeId)
            .Distinct()
            .ToArray();
        var outcomeCalculations = new Dictionary<Guid, MasteryCalculation>();
        foreach (var outcomeId in affectedOutcomeIds)
        {
            var leafIds = outcomeMappings
                .Where(mapping => mapping.CurriculumOutcomeId == outcomeId)
                .SelectMany(mapping => DescendantLeaves(mapping.KnowledgeComponentId, childrenByParent))
                .Distinct()
                .ToArray();
            outcomeCalculations[outcomeId] = MasteryReadinessCalculator.AggregateChildren(
                leafIds.Select(id => calculatedComponents[id]).ToArray());
        }

        await UpsertOutcomesAsync(
            db,
            studentId,
            outcomeCalculations,
            calculatedAtUtc,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static HashSet<Guid> BuildAffectedPath(
        IEnumerable<Guid> componentIds,
        IReadOnlyDictionary<Guid, ComponentNode> componentById)
    {
        var path = new HashSet<Guid>();
        foreach (var componentId in componentIds)
        {
            var current = componentById[componentId];
            while (path.Add(current.Id) && current.ParentId.HasValue)
            {
                current = componentById[current.ParentId.Value];
            }
        }

        return path;
    }

    private static IEnumerable<Guid> DescendantLeaves(
        Guid componentId,
        IReadOnlyDictionary<Guid, ComponentNode[]> childrenByParent)
    {
        if (!childrenByParent.TryGetValue(componentId, out var children))
        {
            yield return componentId;
            yield break;
        }

        foreach (var child in children)
        {
            foreach (var leafId in DescendantLeaves(child.Id, childrenByParent))
            {
                yield return leafId;
            }
        }
    }

    private static MasteryEvidenceInput ToInput(EffectiveEvidenceRow row)
    {
        if (!row.PerformanceScore.HasValue
            || !row.Difficulty.HasValue
            || !row.EvidenceType.HasValue
            || !row.AssistanceLevel.HasValue
            || !row.EvidenceContext.HasValue)
        {
            throw new InvalidOperationException("Effective evidence metadata snapshot is incomplete.");
        }

        return new(
            row.EvidenceEventId,
            row.PerformanceScore.Value,
            row.Difficulty.Value,
            row.EvidenceType.Value,
            row.AssistanceLevel.Value,
            row.EvidenceContext.Value,
            row.OccurredAtUtc);
    }

    private static async Task UpsertComponentsAsync(
        Plus5DbContext db,
        Guid studentId,
        HashSet<Guid> componentIds,
        Dictionary<Guid, MasteryCalculation> calculations,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await db.MasteryEstimates
            .Where(estimate => estimate.StudentId == studentId
                && componentIds.Contains(estimate.KnowledgeComponentId))
            .ToDictionaryAsync(estimate => estimate.KnowledgeComponentId, cancellationToken);
        foreach (var componentId in componentIds)
        {
            var calculation = calculations[componentId];
            if (existing.TryGetValue(componentId, out var estimate))
            {
                estimate.Refresh(
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion);
            }
            else
            {
                db.MasteryEstimates.Add(new MasteryEstimate(
                    studentId,
                    componentId,
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion));
            }
        }
    }

    private static async Task UpsertAreasAsync(
        Plus5DbContext db,
        Guid studentId,
        IReadOnlyDictionary<Guid, MasteryCalculation> calculations,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken)
    {
        var areaIds = calculations.Keys.ToArray();
        var existing = await db.KnowledgeAreaReadinessEstimates
            .Where(estimate => estimate.StudentId == studentId
                && areaIds.Contains(estimate.KnowledgeAreaId))
            .ToDictionaryAsync(estimate => estimate.KnowledgeAreaId, cancellationToken);
        foreach (var (areaId, calculation) in calculations)
        {
            if (existing.TryGetValue(areaId, out var estimate))
            {
                estimate.Refresh(
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion);
            }
            else
            {
                db.KnowledgeAreaReadinessEstimates.Add(new KnowledgeAreaReadinessEstimate(
                    studentId,
                    areaId,
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion));
            }
        }
    }

    private static async Task UpsertOutcomesAsync(
        Plus5DbContext db,
        Guid studentId,
        IReadOnlyDictionary<Guid, MasteryCalculation> calculations,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken)
    {
        var outcomeIds = calculations.Keys.ToArray();
        var existing = await db.CurriculumOutcomeReadinessEstimates
            .Where(estimate => estimate.StudentId == studentId
                && outcomeIds.Contains(estimate.CurriculumOutcomeId))
            .ToDictionaryAsync(estimate => estimate.CurriculumOutcomeId, cancellationToken);
        foreach (var (outcomeId, calculation) in calculations)
        {
            if (existing.TryGetValue(outcomeId, out var estimate))
            {
                estimate.Refresh(
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion);
            }
            else
            {
                db.CurriculumOutcomeReadinessEstimates.Add(new CurriculumOutcomeReadinessEstimate(
                    studentId,
                    outcomeId,
                    calculation,
                    calculatedAtUtc,
                    MasteryReadinessCalculator.AlgorithmVersion));
            }
        }
    }

    private sealed record ComponentNode(Guid Id, Guid AreaId, Guid? ParentId);

    private sealed record EffectiveEvidenceRow(
        Guid EvidenceEventId,
        Guid KnowledgeComponentId,
        decimal? PerformanceScore,
        int? Difficulty,
        EvidenceType? EvidenceType,
        AssistanceLevel? AssistanceLevel,
        EvidenceContext? EvidenceContext,
        DateTimeOffset OccurredAtUtc);

    private sealed record OutcomeMapping(Guid CurriculumOutcomeId, Guid KnowledgeComponentId);
}
