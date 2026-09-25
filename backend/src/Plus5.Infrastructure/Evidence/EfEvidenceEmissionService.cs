using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Plus5.Application.Evidence;
using Plus5.Application.Readiness;
using Plus5.Domain.Evidence;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Evidence;

public sealed class EfEvidenceEmissionService(
    Plus5DbContext db,
    TimeProvider clock,
    IMasteryReadinessProjectionService readinessProjection) : IEvidenceEmissionService
{
    public async Task<EvidenceWriteResult> RecordObservationAsync(
        Guid owner,
        EvidenceObservationCommand command,
        CancellationToken cancellationToken)
    {
        if (owner == Guid.Empty
            || command.StudentId == Guid.Empty
            || command.SourceId == Guid.Empty
            || command.KnowledgeComponentIds is null)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await db.Students.AnyAsync(
                    student => student.Id == command.StudentId
                        && student.TeacherAccountId == owner,
                    cancellationToken))
            {
                return Failure(EvidenceWriteFailure.NotFound);
            }

            var normalizedSourceKind = NormalizeForLookup(command.SourceKind);
            if (normalizedSourceKind is null)
            {
                return Failure(EvidenceWriteFailure.Invalid);
            }

            if (await db.EvidenceEvents.AnyAsync(
                    evidence => evidence.StudentId == command.StudentId
                        && evidence.SourceKind == normalizedSourceKind
                        && evidence.SourceId == command.SourceId
                        && evidence.SupersedesEvidenceEventId == null,
                    cancellationToken))
            {
                return Failure(EvidenceWriteFailure.DuplicateSource);
            }

            var targets = await LoadTargetsAsync(
                command.KnowledgeComponentIds,
                cancellationToken);
            if (targets is null)
            {
                return Failure(EvidenceWriteFailure.InvalidKnowledgeTarget);
            }

            var evidenceEvent = EvidenceEvent.CreateObservation(
                Guid.NewGuid(),
                command.StudentId,
                normalizedSourceKind,
                command.SourceId,
                command.OccurredAtUtc,
                clock.GetUtcNow(),
                CreateMetadata(command),
                command.PerformanceScore,
                targets);
            AddEvidence(evidenceEvent);
            await db.SaveChangesAsync(cancellationToken);
            await readinessProjection.RecalculateAffectedAsync(
                command.StudentId,
                command.KnowledgeComponentIds,
                cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return Success(evidenceEvent.Id);
        }
        catch (ArgumentException)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
    }

    public async Task<EvidenceWriteResult> CorrectAsync(
        Guid owner,
        EvidenceCorrectionCommand command,
        CancellationToken cancellationToken)
    {
        if (owner == Guid.Empty
            || command.SupersededEvidenceEventId == Guid.Empty
            || command.KnowledgeComponentIds is null)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var predecessor = await FindOwnedEvidenceAsync(
                owner,
                command.SupersededEvidenceEventId,
                cancellationToken);
            if (predecessor is null)
            {
                return Failure(EvidenceWriteFailure.NotFound);
            }

            if (predecessor.Kind == EvidenceEventKind.Invalidation
                || await HasSuccessorAsync(predecessor.Id, cancellationToken))
            {
                return Failure(EvidenceWriteFailure.Conflict);
            }

            var previousTargetIds = await LoadEvidenceTargetIdsAsync(
                predecessor.Id,
                cancellationToken);

            var targets = await LoadTargetsAsync(
                command.KnowledgeComponentIds,
                cancellationToken);
            if (targets is null)
            {
                return Failure(EvidenceWriteFailure.InvalidKnowledgeTarget);
            }

            var evidenceEvent = EvidenceEvent.CreateCorrection(
                Guid.NewGuid(),
                predecessor,
                command.CorrectedOccurredAtUtc,
                clock.GetUtcNow(),
                command.ReasonCode,
                CreateMetadata(command),
                command.PerformanceScore,
                targets);
            AddEvidence(evidenceEvent);
            await db.SaveChangesAsync(cancellationToken);
            await readinessProjection.RecalculateAffectedAsync(
                predecessor.StudentId,
                previousTargetIds.Concat(command.KnowledgeComponentIds).Distinct().ToArray(),
                cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return Success(evidenceEvent.Id);
        }
        catch (ArgumentException)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }
        catch (InvalidOperationException)
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
    }

    public async Task<EvidenceWriteResult> InvalidateAsync(
        Guid owner,
        EvidenceInvalidationCommand command,
        CancellationToken cancellationToken)
    {
        if (owner == Guid.Empty || command.SupersededEvidenceEventId == Guid.Empty)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var predecessor = await FindOwnedEvidenceAsync(
                owner,
                command.SupersededEvidenceEventId,
                cancellationToken);
            if (predecessor is null)
            {
                return Failure(EvidenceWriteFailure.NotFound);
            }

            if (predecessor.Kind == EvidenceEventKind.Invalidation
                || await HasSuccessorAsync(predecessor.Id, cancellationToken))
            {
                return Failure(EvidenceWriteFailure.Conflict);
            }

            var previousTargetIds = await LoadEvidenceTargetIdsAsync(
                predecessor.Id,
                cancellationToken);

            var evidenceEvent = EvidenceEvent.CreateInvalidation(
                Guid.NewGuid(),
                predecessor,
                clock.GetUtcNow(),
                command.ReasonCode);
            AddEvidence(evidenceEvent);
            await db.SaveChangesAsync(cancellationToken);
            await readinessProjection.RecalculateAffectedAsync(
                predecessor.StudentId,
                previousTargetIds,
                cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return Success(evidenceEvent.Id);
        }
        catch (ArgumentException)
        {
            return Failure(EvidenceWriteFailure.Invalid);
        }
        catch (InvalidOperationException)
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return Failure(EvidenceWriteFailure.Conflict);
        }
    }

    private async Task<EvidenceEvent?> FindOwnedEvidenceAsync(
        Guid owner,
        Guid evidenceEventId,
        CancellationToken cancellationToken) =>
        await db.EvidenceEvents.SingleOrDefaultAsync(
            evidence => evidence.Id == evidenceEventId
                && db.Students.Any(student => student.Id == evidence.StudentId
                    && student.TeacherAccountId == owner),
            cancellationToken);

    private async Task<bool> HasSuccessorAsync(
        Guid evidenceEventId,
        CancellationToken cancellationToken) =>
        await db.EvidenceEvents.AnyAsync(
            evidence => evidence.SupersedesEvidenceEventId == evidenceEventId,
            cancellationToken);

    private async Task<Guid[]> LoadEvidenceTargetIdsAsync(
        Guid evidenceEventId,
        CancellationToken cancellationToken) =>
        await db.EvidenceEventKnowledgeComponents
            .Where(mapping => mapping.EvidenceEventId == evidenceEventId)
            .Select(mapping => mapping.KnowledgeComponentId)
            .ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyCollection<EvidenceKnowledgeTarget>?> LoadTargetsAsync(
        IReadOnlyList<Guid> knowledgeComponentIds,
        CancellationToken cancellationToken)
    {
        if (knowledgeComponentIds.Count == 0
            || knowledgeComponentIds.Any(id => id == Guid.Empty)
            || knowledgeComponentIds.Distinct().Count() != knowledgeComponentIds.Count)
        {
            return null;
        }

        var rows = await (
            from component in db.KnowledgeComponents
            join model in db.KnowledgeModels
                on component.KnowledgeModelId equals model.Id
            where knowledgeComponentIds.Contains(component.Id)
            select new { Component = component, Model = model })
            .ToListAsync(cancellationToken);
        if (rows.Count != knowledgeComponentIds.Count)
        {
            return null;
        }

        var parentIds = await db.KnowledgeComponents
            .Where(component => component.ParentComponentId.HasValue
                && knowledgeComponentIds.Contains(component.ParentComponentId.Value))
            .Select(component => component.ParentComponentId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var nonLeafIds = parentIds.ToHashSet();

        try
        {
            return rows.Select(row => new EvidenceKnowledgeTarget(
                    row.Component,
                    row.Model,
                    !nonLeafIds.Contains(row.Component.Id)))
                .ToArray();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void AddEvidence(EvidenceEvent evidenceEvent)
    {
        db.EvidenceEvents.Add(evidenceEvent);
        db.EvidenceEventKnowledgeComponents.AddRange(evidenceEvent.KnowledgeComponents);
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(
        CancellationToken cancellationToken) =>
        db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

    private static Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.CommitAsync(cancellationToken);

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static string? NormalizeForLookup(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static EvidenceMetadata CreateMetadata(EvidenceObservationCommand command) =>
        new(
            command.Difficulty,
            command.EvidenceType,
            command.AssistanceLevel,
            command.EvidenceContext);

    private static EvidenceMetadata CreateMetadata(EvidenceCorrectionCommand command) =>
        new(
            command.Difficulty,
            command.EvidenceType,
            command.AssistanceLevel,
            command.EvidenceContext);

    private static EvidenceWriteResult Success(Guid evidenceEventId) =>
        new(evidenceEventId, EvidenceWriteFailure.None);

    private static EvidenceWriteResult Failure(EvidenceWriteFailure failure) =>
        new(null, failure);
}
