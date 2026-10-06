using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialImportService(
    Plus5DbContext db,
    IMaterialFileValidator validator,
    IMaterialObjectStorage storage,
    IMalwareScanner scanner,
    IOptions<MaterialStorageOptions> configuredStorage,
    TimeProvider timeProvider) : IMaterialImportService
{
    private readonly MaterialStorageOptions storageOptions = configuredStorage.Value;

    public async Task<MaterialImportResult> ImportAsync(
        Guid teacherAccountId,
        MaterialImportCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.IsDefined(command.Format)
            || !Enum.IsDefined(command.Visibility)
            || command.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag) && tag.Trim().Length > 64))
        {
            return new MaterialImportResult(MaterialImportOutcome.InvalidInput);
        }

        var domainFormat = (MaterialFileFormat)(int)command.Format;
        var domainVisibility = (MaterialVisibility)(int)command.Visibility;

        var references = await LoadReferencesAsync(teacherAccountId, command, cancellationToken);
        if (references is null)
        {
            return new MaterialImportResult(MaterialImportOutcome.ReferenceNotFound);
        }

        MaterialFileValidationResult validation;
        try
        {
            await using var validationStream = command.OpenReadStream();
            validation = await validator.ValidateAsync(
                new MaterialFileValidationRequest(
                    domainFormat,
                    command.OriginalFileName,
                    command.DeclaredMediaType,
                    command.DeclaredSizeBytes),
                validationStream,
                cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException)
        {
            return new MaterialImportResult(MaterialImportOutcome.InvalidFile, Detail: "FILE_INVALID");
        }

        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.Sha256Checksum))
        {
            return new MaterialImportResult(
                MaterialImportOutcome.InvalidFile,
                Detail: validation.ResultCategory);
        }

        var now = timeProvider.GetUtcNow();
        Material material;
        MaterialVersion version;
        MaterialFile file;
        try
        {
            material = new Material(Guid.NewGuid(), teacherAccountId, now);
            material.SetVisibility(domainVisibility, now);
            version = new MaterialVersion(
                Guid.NewGuid(), material, 1, command.Title, command.MaterialTypeCode, now,
                command.Description, command.Subject, command.LanguageCode,
                references.Program, references.SchoolGrade, references.ProficiencyLevel,
                command.LearningGoal);
            file = new MaterialFile(
                Guid.NewGuid(), material, version, domainFormat, command.OriginalFileName,
                command.DeclaredMediaType, command.DeclaredSizeBytes,
                storageOptions.ProviderCode, storageOptions.CleanBucket, now);
        }
        catch (ArgumentException exception)
        {
            return new MaterialImportResult(MaterialImportOutcome.InvalidInput, Detail: exception.ParamName);
        }

        var objectReference = new MaterialObjectReference(
            file.StorageProvider,
            file.StorageContainer,
            file.ObjectKey);
        try
        {
            await using var uploadStream = command.OpenReadStream();
            await storage.PutQuarantineAsync(
                new MaterialObjectWrite(
                    objectReference,
                    validation.ActualSizeBytes,
                    command.DeclaredMediaType,
                    validation.Sha256Checksum),
                uploadStream,
                cancellationToken);
            var uploadedAt = timeProvider.GetUtcNow();
            file.MarkUploaded(validation.ActualSizeBytes, validation.Sha256Checksum, uploadedAt);
            file.StartScanning(uploadedAt);

            await using var scanStream = await storage.OpenQuarantineReadAsync(
                objectReference,
                cancellationToken);
            var scan = await scanner.ScanAsync(scanStream, cancellationToken);
            var scannedAt = timeProvider.GetUtcNow();

            if (scan.IsMalware)
            {
                file.MarkQuarantined(scan.ResultCategory, scannedAt);
                await PersistRejectedAsync(material, version, file, cancellationToken);
                return new MaterialImportResult(MaterialImportOutcome.MalwareDetected);
            }

            if (!scan.IsClean)
            {
                file.MarkFailed(scan.ResultCategory, scannedAt);
                await PersistRejectedAsync(material, version, file, cancellationToken);
                return new MaterialImportResult(MaterialImportOutcome.ScannerUnavailable);
            }

            await storage.PromoteCleanAsync(objectReference, cancellationToken);
            file.MarkClean(scannedAt);
            return await PersistCleanAsync(
                material,
                version,
                file,
                references,
                command,
                cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(objectReference);
            throw;
        }
    }

    private async Task<MaterialImportResult> PersistCleanAsync(
        Material material,
        MaterialVersion version,
        MaterialFile file,
        ImportReferences references,
        MaterialImportCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Materials.Add(material);
        db.MaterialVersions.Add(version);
        db.MaterialFiles.Add(file);
        db.MaterialVersionTags.AddRange(command.Tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(tag => new MaterialVersionTag(version, tag)));
        db.MaterialVersionKnowledgeComponents.AddRange(references.KnowledgeComponents
            .Select(component => new MaterialVersionKnowledgeComponent(
                version,
                component.Component,
                component.Model)));
        db.MaterialVersionCurriculumOutcomes.AddRange(references.CurriculumOutcomes
            .Select(outcome => new MaterialVersionCurriculumOutcome(version, outcome)));

        await db.SaveChangesAsync(cancellationToken);
        var activatedAt = timeProvider.GetUtcNow();
        version.Activate(file, activatedAt);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(material).ReloadAsync(cancellationToken);
        if (material.CurrentVersionId != version.Id)
        {
            throw new InvalidOperationException("The activated material version was not made current.");
        }

        await transaction.CommitAsync(cancellationToken);
        return new MaterialImportResult(MaterialImportOutcome.Created, material.Id);
    }

    private async Task PersistRejectedAsync(
        Material material,
        MaterialVersion version,
        MaterialFile file,
        CancellationToken cancellationToken)
    {
        db.Materials.Add(material);
        db.MaterialVersions.Add(version);
        db.MaterialFiles.Add(file);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ImportReferences?> LoadReferencesAsync(
        Guid teacherAccountId,
        MaterialImportCommand command,
        CancellationToken cancellationToken)
    {
        var program = command.ProgramId.HasValue
            ? await db.Programs.SingleOrDefaultAsync(
                candidate => candidate.Id == command.ProgramId
                    && candidate.TeacherAccountId == teacherAccountId,
                cancellationToken)
            : null;
        if (command.ProgramId.HasValue && program is null) return null;

        var grade = command.SchoolGradeId.HasValue
            ? await db.SchoolGrades.FindAsync([command.SchoolGradeId.Value], cancellationToken)
            : null;
        if (command.SchoolGradeId.HasValue && grade is null) return null;

        var level = command.ProficiencyLevelId.HasValue
            ? await db.ProficiencyLevels.FindAsync([command.ProficiencyLevelId.Value], cancellationToken)
            : null;
        if (command.ProficiencyLevelId.HasValue && level is null) return null;

        var componentIds = command.KnowledgeComponentIds.Distinct().ToList();
        var componentRows = await (
            from component in db.KnowledgeComponents
            join model in db.KnowledgeModels on component.KnowledgeModelId equals model.Id
            where componentIds.Contains(component.Id)
                && component.Status == KnowledgeComponentStatus.Active
                && model.Status != KnowledgeModelStatus.Draft
            select new ComponentReference(component, model))
            .ToListAsync(cancellationToken);
        if (componentRows.Count != componentIds.Count) return null;

        var outcomeIds = command.CurriculumOutcomeIds.Distinct().ToList();
        var outcomes = await db.CurriculumOutcomes
            .Where(outcome => outcomeIds.Contains(outcome.Id))
            .ToListAsync(cancellationToken);
        if (outcomes.Count != outcomeIds.Count) return null;

        return new ImportReferences(program, grade, level, componentRows, outcomes);
    }

    private async Task TryDeleteAsync(MaterialObjectReference reference)
    {
        try
        {
            await storage.DeleteAsync(reference, CancellationToken.None);
        }
        catch
        {
            // The original exception remains authoritative; orphan cleanup is operationally observable.
        }
    }

    private sealed record ComponentReference(KnowledgeComponent Component, KnowledgeModel Model);

    private sealed record ImportReferences(
        Plus5.Domain.Teaching.Program? Program,
        SchoolGrade? SchoolGrade,
        ProficiencyLevel? ProficiencyLevel,
        IReadOnlyList<ComponentReference> KnowledgeComponents,
        IReadOnlyList<CurriculumOutcome> CurriculumOutcomes);
}
