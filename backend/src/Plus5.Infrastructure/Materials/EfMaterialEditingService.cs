using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialEditingService(
    Plus5DbContext db,
    IMaterialObjectStorage storage,
    TimeProvider timeProvider) : IMaterialEditingService
{
    public async Task<MaterialEditResult> SaveDraftAsync(Guid teacherAccountId, Guid materialId, MaterialEditCommand command, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedRowVersion, out var expected)) return Invalid();
        var aggregate = await LoadOwnedMaterialAsync(teacherAccountId, materialId, cancellationToken);
        if (aggregate is null) return NotFound();
        if (!aggregate.RowVersion.SequenceEqual(expected)) return Conflict();

        var references = await LoadReferencesAsync(teacherAccountId, command, cancellationToken);
        if (references is null) return new(MaterialEditOutcome.ReferenceNotFound);

        var draft = command.DraftVersionId.HasValue
            ? await db.MaterialVersions.SingleOrDefaultAsync(version => version.Id == command.DraftVersionId && version.MaterialId == materialId && version.Status == MaterialVersionStatus.Draft, cancellationToken)
            : null;
        if (command.DraftVersionId.HasValue && draft is null) return Conflict();

        MaterialObjectReference? copiedObject = null;
        try
        {
            if (draft is null)
            {
                var existingDraft = await db.MaterialVersions.AnyAsync(version => version.MaterialId == materialId && version.Status == MaterialVersionStatus.Draft, cancellationToken);
                if (existingDraft) return new(MaterialEditOutcome.DraftAlreadyExists);
                var source = await LoadVersionSourceAsync(aggregate.CurrentVersionId!.Value, cancellationToken);
                if (source is null) return Conflict();
                draft = await CloneVersionAsync(aggregate, source.Value.Version, source.Value.File, cancellationToken);
                copiedObject = FileReference(db.MaterialFiles.Local.Single(file => file.MaterialVersionId == draft.Id));
            }

            draft.UpdateDraft(command.Title, command.MaterialTypeCode, command.Description, command.Subject,
                command.LanguageCode, references.Program, references.Grade, references.Level, command.LearningGoal);
            await ReplaceMappingsAsync(draft, command, references, cancellationToken);
            aggregate.RecordVersionChange(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return new(MaterialEditOutcome.Success, draft.Id);
        }
        catch (ArgumentException)
        {
            if (copiedObject is not null) await TryDeleteAsync(copiedObject);
            return Invalid();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (copiedObject is not null) await TryDeleteAsync(copiedObject);
            return Conflict();
        }
        catch (AmazonS3Exception)
        {
            if (copiedObject is not null) await TryDeleteAsync(copiedObject);
            return new(MaterialEditOutcome.StorageFailure);
        }
        catch
        {
            if (copiedObject is not null) await TryDeleteAsync(copiedObject);
            throw;
        }
    }

    public async Task<MaterialEditResult> PublishDraftAsync(Guid teacherAccountId, Guid materialId, MaterialPublishCommand command, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedRowVersion, out var expected)) return Invalid();
        var material = await LoadOwnedMaterialAsync(teacherAccountId, materialId, cancellationToken);
        if (material is null) return NotFound();
        if (!material.RowVersion.SequenceEqual(expected)) return Conflict();
        var draft = await db.MaterialVersions.SingleOrDefaultAsync(version => version.Id == command.DraftVersionId
            && version.MaterialId == materialId && version.Status == MaterialVersionStatus.Draft, cancellationToken);
        var file = draft is null ? null : await db.MaterialFiles.SingleOrDefaultAsync(candidate => candidate.MaterialVersionId == draft.Id && candidate.Status == MaterialFileStatus.Clean, cancellationToken);
        var current = material.CurrentVersionId.HasValue ? await db.MaterialVersions.SingleOrDefaultAsync(version => version.Id == material.CurrentVersionId, cancellationToken) : null;
        if (draft is null || file is null || current is null) return Conflict();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            material.RecordVersionChange(now);
            await db.SaveChangesAsync(cancellationToken);
            current.Supersede(now);
            await db.SaveChangesAsync(cancellationToken);
            draft.Activate(file, now);
            await db.SaveChangesAsync(cancellationToken);
            await db.Entry(material).ReloadAsync(cancellationToken);
            if (material.CurrentVersionId != draft.Id) throw new InvalidOperationException("Published version was not made current.");
            await transaction.CommitAsync(cancellationToken);
            return new(MaterialEditOutcome.Success, draft.Id);
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(cancellationToken); return Conflict(); }
    }

    public async Task<MaterialEditResult> RestoreAsync(Guid teacherAccountId, Guid materialId, Guid versionId, MaterialRestoreCommand command, CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedRowVersion, out var expected)) return Invalid();
        var material = await LoadOwnedMaterialAsync(teacherAccountId, materialId, cancellationToken);
        if (material is null) return NotFound();
        if (!material.RowVersion.SequenceEqual(expected)) return Conflict();
        if (material.CurrentVersionId == versionId) return Invalid();
        if (await db.MaterialVersions.AnyAsync(version => version.MaterialId == materialId && version.Status == MaterialVersionStatus.Draft, cancellationToken))
            return new(MaterialEditOutcome.DraftAlreadyExists);
        var source = await LoadVersionSourceAsync(versionId, cancellationToken);
        if (source is null || source.Value.Version.MaterialId != materialId || source.Value.Version.Status == MaterialVersionStatus.Draft) return NotFound();
        MaterialObjectReference? copied = null;
        try
        {
            var draft = await CloneVersionAsync(material, source.Value.Version, source.Value.File, cancellationToken);
            copied = FileReference(db.MaterialFiles.Local.Single(file => file.MaterialVersionId == draft.Id));
            material.RecordVersionChange(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return new(MaterialEditOutcome.Success, draft.Id);
        }
        catch (DbUpdateConcurrencyException) { if (copied is not null) await TryDeleteAsync(copied); return Conflict(); }
        catch (AmazonS3Exception) { if (copied is not null) await TryDeleteAsync(copied); return new(MaterialEditOutcome.StorageFailure); }
        catch { if (copied is not null) await TryDeleteAsync(copied); throw; }
    }

    private async Task<MaterialVersion> CloneVersionAsync(Material material, MaterialVersion source, MaterialFile sourceFile, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var nextNumber = await db.MaterialVersions.Where(version => version.MaterialId == material.Id).MaxAsync(version => version.VersionNumber, cancellationToken) + 1;
        var draft = source.CreateRestoredDraft(Guid.NewGuid(), material, nextNumber, now);
        var file = new MaterialFile(Guid.NewGuid(), material, draft, sourceFile.Format, sourceFile.OriginalFileName,
            sourceFile.DeclaredMediaType, sourceFile.DeclaredSizeBytes, sourceFile.StorageProvider, sourceFile.StorageContainer, now);
        var destination = FileReference(file);
        try
        {
            await storage.CopyCleanAsync(FileReference(sourceFile), destination, cancellationToken);
            file.MarkCleanCopy(sourceFile, now);
            db.MaterialVersions.Add(draft);
            db.MaterialFiles.Add(file);

            var tags = await db.MaterialVersionTags.AsNoTracking().Where(x => x.MaterialVersionId == source.Id).Select(x => x.Name).ToListAsync(cancellationToken);
            db.MaterialVersionTags.AddRange(tags.Select(tag => new MaterialVersionTag(draft, tag)));
            var components = await (from mapping in db.MaterialVersionKnowledgeComponents.AsNoTracking()
                                    join component in db.KnowledgeComponents on mapping.KnowledgeComponentId equals component.Id
                                    join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
                                    where mapping.MaterialVersionId == source.Id
                                    select new { component, model }).ToListAsync(cancellationToken);
            db.MaterialVersionKnowledgeComponents.AddRange(components.Select(row => new MaterialVersionKnowledgeComponent(draft, row.component, row.model)));
            var outcomes = await (from mapping in db.MaterialVersionCurriculumOutcomes.AsNoTracking()
                                  join outcome in db.CurriculumOutcomes on mapping.CurriculumOutcomeId equals outcome.Id
                                  where mapping.MaterialVersionId == source.Id
                                  select outcome).ToListAsync(cancellationToken);
            db.MaterialVersionCurriculumOutcomes.AddRange(outcomes.Select(outcome => new MaterialVersionCurriculumOutcome(draft, outcome)));
            await CloneTasksAsync(source.Id, draft, now, cancellationToken);
            return draft;
        }
        catch
        {
            await TryDeleteAsync(destination);
            throw;
        }
    }

    private async Task CloneTasksAsync(Guid sourceVersionId, MaterialVersion draft, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sources = await db.AssessableTaskVersions.AsNoTracking().Where(version => version.MaterialVersionId == sourceVersionId).OrderBy(version => version.SortOrder).ToListAsync(cancellationToken);
        foreach (var source in sources)
        {
            var task = await db.AssessableTasks.SingleAsync(candidate => candidate.Id == source.AssessableTaskId, cancellationToken);
            var next = await db.AssessableTaskVersions.Where(version => version.AssessableTaskId == task.Id).MaxAsync(version => version.VersionNumber, cancellationToken) + 1;
            var clone = new AssessableTaskVersion(Guid.NewGuid(), task, draft, next, source.SortOrder, source.Prompt,
                source.TaskTypeCode, source.Difficulty, source.EvidenceType, source.MaxPoints, now, source.CorrectAnswer, source.EvaluationCriterion);
            db.AssessableTaskVersions.Add(clone);
            var targets = await (from mapping in db.AssessableTaskVersionKnowledgeComponents.AsNoTracking()
                                 join component in db.KnowledgeComponents on mapping.KnowledgeComponentId equals component.Id
                                 join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
                                 where mapping.AssessableTaskVersionId == source.Id
                                 select new { component, model, IsLeaf = !db.KnowledgeComponents.Any(child => child.ParentComponentId == component.Id) }).ToListAsync(cancellationToken);
            db.AssessableTaskVersionKnowledgeComponents.AddRange(targets.Select(target => new AssessableTaskVersionKnowledgeComponent(clone, draft, target.component, target.model, target.IsLeaf)));
        }
    }

    private async Task ReplaceMappingsAsync(MaterialVersion draft, MaterialEditCommand command, EditReferences references, CancellationToken cancellationToken)
    {
        db.MaterialVersionTags.RemoveRange(await db.MaterialVersionTags.Where(x => x.MaterialVersionId == draft.Id).ToListAsync(cancellationToken));
        db.MaterialVersionKnowledgeComponents.RemoveRange(await db.MaterialVersionKnowledgeComponents.Where(x => x.MaterialVersionId == draft.Id).ToListAsync(cancellationToken));
        db.MaterialVersionCurriculumOutcomes.RemoveRange(await db.MaterialVersionCurriculumOutcomes.Where(x => x.MaterialVersionId == draft.Id).ToListAsync(cancellationToken));
        db.MaterialVersionTags.AddRange(command.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Select(tag => new MaterialVersionTag(draft, tag)));
        db.MaterialVersionKnowledgeComponents.AddRange(references.Components.Select(row => new MaterialVersionKnowledgeComponent(draft, row.Component, row.Model)));
        db.MaterialVersionCurriculumOutcomes.AddRange(references.Outcomes.Select(outcome => new MaterialVersionCurriculumOutcome(draft, outcome)));
    }

    private async Task<EditReferences?> LoadReferencesAsync(Guid teacherId, MaterialEditCommand command, CancellationToken cancellationToken)
    {
        var program = command.ProgramId.HasValue ? await db.Programs.SingleOrDefaultAsync(x => x.Id == command.ProgramId && x.TeacherAccountId == teacherId, cancellationToken) : null;
        var grade = command.SchoolGradeId.HasValue ? await db.SchoolGrades.FindAsync([command.SchoolGradeId.Value], cancellationToken) : null;
        var level = command.ProficiencyLevelId.HasValue ? await db.ProficiencyLevels.FindAsync([command.ProficiencyLevelId.Value], cancellationToken) : null;
        if (command.ProgramId.HasValue && program is null || command.SchoolGradeId.HasValue && grade is null || command.ProficiencyLevelId.HasValue && level is null) return null;
        var componentIds = command.KnowledgeComponentIds.Distinct().ToList();
        var components = await (from component in db.KnowledgeComponents
                                join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
                                where componentIds.Contains(component.Id) && model.Status != KnowledgeModelStatus.Draft
                                select new ComponentReference(component, model)).ToListAsync(cancellationToken);
        var outcomeIds = command.CurriculumOutcomeIds.Distinct().ToList();
        var outcomes = await db.CurriculumOutcomes.Where(outcome => outcomeIds.Contains(outcome.Id)).ToListAsync(cancellationToken);
        return components.Count == componentIds.Count && outcomes.Count == outcomeIds.Count ? new(program, grade, level, components, outcomes) : null;
    }

    private Task<Material?> LoadOwnedMaterialAsync(Guid teacherId, Guid materialId, CancellationToken token) => db.Materials.SingleOrDefaultAsync(material => material.Id == materialId && material.OwnerTeacherId == teacherId && material.Status == MaterialStatus.Active && material.ArchivedAtUtc == null, token);
    private async Task<(MaterialVersion Version, MaterialFile File)?> LoadVersionSourceAsync(Guid versionId, CancellationToken token)
    {
        var version = await db.MaterialVersions.SingleOrDefaultAsync(x => x.Id == versionId, token);
        var file = version is null ? null : await db.MaterialFiles.SingleOrDefaultAsync(x => x.MaterialVersionId == versionId && x.Status == MaterialFileStatus.Clean, token);
        return version is null || file is null ? null : (version, file);
    }
    private static MaterialObjectReference FileReference(MaterialFile file) => new(file.StorageProvider, file.StorageContainer, file.ObjectKey);
    private async Task TryDeleteAsync(MaterialObjectReference reference) { try { await storage.DeleteAsync(reference, CancellationToken.None); } catch { } }
    private static bool TryRowVersion(string value, out byte[] rowVersion) { try { rowVersion = Convert.FromBase64String(value); return rowVersion.Length > 0; } catch (FormatException) { rowVersion = []; return false; } }
    private static MaterialEditResult Invalid() => new(MaterialEditOutcome.InvalidInput);
    private static MaterialEditResult NotFound() => new(MaterialEditOutcome.NotFound);
    private static MaterialEditResult Conflict() => new(MaterialEditOutcome.Conflict);
    private sealed record ComponentReference(KnowledgeComponent Component, KnowledgeModel Model);
    private sealed record EditReferences(Plus5.Domain.Teaching.Program? Program, SchoolGrade? Grade, ProficiencyLevel? Level, IReadOnlyList<ComponentReference> Components, IReadOnlyList<CurriculumOutcome> Outcomes);
}
