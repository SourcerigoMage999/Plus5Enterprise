using System.Text;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationElementService(
    Plus5DbContext db,
    IPresentationImageValidator imageValidator,
    IMaterialObjectStorage storage,
    IMalwareScanner scanner,
    IOptions<MaterialStorageOptions> configuredStorage,
    TimeProvider timeProvider) : IPresentationElementService
{
    private const int TemporaryZIndexOffset = 1000;
    private readonly MaterialStorageOptions storageOptions = configuredStorage.Value;

    public async Task<PresentationAssetContent?> OpenAssetAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var asset = await (
            from material in db.Materials.AsNoTracking()
            join version in db.MaterialVersions.AsNoTracking()
                on material.Id equals version.MaterialId
            join candidate in db.PresentationAssets.AsNoTracking()
                on version.Id equals candidate.MaterialVersionId
            where material.Id == materialId
                && material.OwnerTeacherId == teacherAccountId
                && material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Draft
                && version.MaterialTypeCode == "PRESENTATION"
                && candidate.Id == assetId
                && candidate.Status == PresentationAssetStatus.Clean
            select candidate)
            .SingleOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var content = await storage.OpenCleanReadAsync(Reference(asset), cancellationToken);
        return new(content, asset.MediaType, asset.OriginalFileName);
    }

    public async Task<PresentationMutationResult> CreateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        CreatePresentationElementCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty
            || command.ElementTypeCode == "IMAGE"
            || !PresentationElementPayloadRegistry.TryNormalize(
                command.ElementTypeCode,
                command.PayloadJson,
                out var payload))
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            slideId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        if (state.Elements!.Count >= PresentationElement.MaximumElementsPerSlide)
        {
            return new(PresentationMutationOutcome.ElementLimitReached);
        }

        if (!WithinDocumentPayloadLimit(state.Document!.MaterialVersionId, null, payload))
        {
            return new(PresentationMutationOutcome.PayloadLimitReached);
        }

        PresentationElement element;
        try
        {
            element = new PresentationElement(
                Guid.NewGuid(),
                state.Slide!,
                command.ElementTypeCode,
                state.Elements.Count,
                command.X,
                command.Y,
                command.Width,
                command.Height,
                payload);
        }
        catch (ArgumentException)
        {
            return Invalid();
        }

        db.PresentationElements.Add(element);
        state.Document.RecordChange(timeProvider.GetUtcNow());
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success(element.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict();
        }
    }

    public async Task<PresentationMutationResult> UploadImageAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        UploadPresentationImageCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty
            || !PresentationElementPayloadRegistry.TryNormalize(
                "IMAGE",
                PresentationElementPayloadRegistry.Image(command.AltText, command.Fit),
                out var payload))
        {
            return Invalid();
        }

        PresentationImageValidationResult validation;
        try
        {
            await using var validationStream = command.OpenReadStream();
            validation = await imageValidator.ValidateAsync(
                command.OriginalFileName,
                command.DeclaredMediaType,
                command.DeclaredSizeBytes,
                validationStream,
                cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException)
        {
            return new(PresentationMutationOutcome.InvalidAsset);
        }

        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.Sha256Checksum))
        {
            return new(PresentationMutationOutcome.InvalidAsset);
        }

        var assetId = Guid.NewGuid();
        var versionId = await OwnedDraftVersionIdAsync(
            teacherAccountId,
            materialId,
            cancellationToken);
        if (!versionId.HasValue)
        {
            return NotFound();
        }

        PresentationAsset asset;
        try
        {
            asset = new PresentationAsset(
                assetId,
                versionId.Value,
                teacherAccountId,
                materialId,
                command.OriginalFileName,
                command.DeclaredMediaType,
                validation.ActualSizeBytes,
                storageOptions.ProviderCode,
                storageOptions.CleanBucket,
                validation.Sha256Checksum,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return new(PresentationMutationOutcome.InvalidAsset);
        }

        var reference = Reference(asset);
        try
        {
            await using var uploadStream = command.OpenReadStream();
            await storage.PutQuarantineAsync(
                new MaterialObjectWrite(
                    reference,
                    validation.ActualSizeBytes,
                    asset.MediaType,
                    asset.Sha256Checksum),
                uploadStream,
                cancellationToken);
            await using var scanStream = await storage.OpenQuarantineReadAsync(
                reference,
                cancellationToken);
            var scan = await scanner.ScanAsync(scanStream, cancellationToken);
            if (scan.IsMalware)
            {
                await TryDeleteAsync(reference);
                return new(PresentationMutationOutcome.MalwareDetected);
            }

            if (!scan.IsClean)
            {
                await TryDeleteAsync(reference);
                return new(PresentationMutationOutcome.ScannerUnavailable);
            }

            await storage.PromoteCleanAsync(reference, cancellationToken);
        }
        catch (AmazonS3Exception)
        {
            await TryDeleteAsync(reference);
            return new(PresentationMutationOutcome.StorageFailure);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            slideId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            await TryDeleteAsync(reference);
            return state.Result;
        }

        if (state.Elements!.Count >= PresentationElement.MaximumElementsPerSlide)
        {
            await TryDeleteAsync(reference);
            return new(PresentationMutationOutcome.ElementLimitReached);
        }

        if (!WithinDocumentPayloadLimit(state.Document!.MaterialVersionId, null, payload))
        {
            await TryDeleteAsync(reference);
            return new(PresentationMutationOutcome.PayloadLimitReached);
        }

        PresentationElement element;
        try
        {
            element = new PresentationElement(
                Guid.NewGuid(),
                state.Slide!,
                "IMAGE",
                state.Elements.Count,
                command.X,
                command.Y,
                command.Width,
                command.Height,
                payload,
                asset.Id);
        }
        catch (ArgumentException)
        {
            await TryDeleteAsync(reference);
            return Invalid();
        }

        db.PresentationAssets.Add(asset);
        db.PresentationElements.Add(element);
        state.Document.RecordChange(timeProvider.GetUtcNow());
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success(element.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await TryDeleteAsync(reference);
            return Conflict();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await TryDeleteAsync(reference);
            throw;
        }
    }

    public async Task<PresentationMutationResult> UpdateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        Guid elementId,
        UpdatePresentationElementCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty
            || elementId == Guid.Empty)
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            slideId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        var elements = state.Elements!;
        var element = elements.SingleOrDefault(candidate => candidate.Id == elementId);
        if (element is null
            || !PresentationElementPayloadRegistry.TryNormalize(
                element.ElementTypeCode,
                command.PayloadJson,
                out var payload))
        {
            return element is null ? NotFound() : Invalid();
        }

        if (command.ZIndex < 0 || command.ZIndex >= elements.Count
            || !WithinDocumentPayloadLimit(state.Document!.MaterialVersionId, element.Id, payload))
        {
            return command.ZIndex < 0 || command.ZIndex >= elements.Count
                ? Invalid()
                : new(PresentationMutationOutcome.PayloadLimitReached);
        }

        try
        {
            var ordered = elements.OrderBy(candidate => candidate.ZIndex).ToList();
            ordered.Remove(element);
            ordered.Insert(command.ZIndex, element);
            foreach (var candidate in ordered)
            {
                candidate.Update(
                    TemporaryZIndexOffset + candidate.ZIndex,
                    candidate.X,
                    candidate.Y,
                    candidate.Width,
                    candidate.Height,
                    candidate.PayloadJson);
            }

            await db.SaveChangesAsync(cancellationToken);
            for (var index = 0; index < ordered.Count; index++)
            {
                var candidate = ordered[index];
                candidate.Update(
                    index,
                    candidate.Id == element.Id ? command.X : candidate.X,
                    candidate.Id == element.Id ? command.Y : candidate.Y,
                    candidate.Id == element.Id ? command.Width : candidate.Width,
                    candidate.Id == element.Id ? command.Height : candidate.Height,
                    candidate.Id == element.Id ? payload : candidate.PayloadJson);
            }

            state.Document.RecordChange(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success(element.Id);
        }
        catch (ArgumentException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Invalid();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict();
        }
    }

    public async Task<PresentationMutationResult> DeleteAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        Guid elementId,
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty
            || elementId == Guid.Empty)
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            slideId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        var elements = state.Elements!;
        var element = elements.SingleOrDefault(candidate => candidate.Id == elementId);
        if (element is null)
        {
            return NotFound();
        }

        db.PresentationElements.Remove(element);
        var remaining = elements
            .Where(candidate => candidate.Id != element.Id)
            .OrderBy(candidate => candidate.ZIndex)
            .ToList();
        try
        {
            for (var index = 0; index < remaining.Count; index++)
            {
                remaining[index].Update(
                    TemporaryZIndexOffset + index,
                    remaining[index].X,
                    remaining[index].Y,
                    remaining[index].Width,
                    remaining[index].Height,
                    remaining[index].PayloadJson);
            }

            await db.SaveChangesAsync(cancellationToken);
            for (var index = 0; index < remaining.Count; index++)
            {
                remaining[index].Update(
                    index,
                    remaining[index].X,
                    remaining[index].Y,
                    remaining[index].Width,
                    remaining[index].Height,
                    remaining[index].PayloadJson);
            }

            state.Document!.RecordChange(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict();
        }
    }

    private async Task<MutableState> LoadMutableAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        var versionId = await OwnedDraftVersionIdAsync(
            teacherAccountId,
            materialId,
            cancellationToken);
        if (!versionId.HasValue)
        {
            return new(null, null, null, NotFound());
        }

        var document = await db.PresentationDocuments.SingleOrDefaultAsync(
            candidate => candidate.MaterialVersionId == versionId.Value,
            cancellationToken);
        if (document is null)
        {
            return new(null, null, null, NotFound());
        }

        if (!document.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            return new(null, null, null, Conflict());
        }

        var slide = await db.PresentationSlides.SingleOrDefaultAsync(
            candidate => candidate.Id == slideId
                && candidate.MaterialVersionId == versionId.Value,
            cancellationToken);
        if (slide is null)
        {
            return new(null, null, null, NotFound());
        }

        var elements = await db.PresentationElements
            .Where(candidate => candidate.PresentationSlideId == slideId)
            .OrderBy(candidate => candidate.ZIndex)
            .ToListAsync(cancellationToken);
        return new(document, slide, elements, null);
    }

    private Task<Guid?> OwnedDraftVersionIdAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken) => (
            from material in db.Materials
            join version in db.MaterialVersions on material.Id equals version.MaterialId
            where material.Id == materialId
                && material.OwnerTeacherId == teacherAccountId
                && material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Draft
                && version.MaterialTypeCode == "PRESENTATION"
            orderby version.VersionNumber descending
            select (Guid?)version.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private bool WithinDocumentPayloadLimit(Guid versionId, Guid? excludedId, string payload)
    {
        var databaseBytes = db.PresentationElements
            .AsNoTracking()
            .Where(element => element.MaterialVersionId == versionId
                && element.Id != excludedId)
            .Select(element => element.PayloadJson)
            .AsEnumerable()
            .Sum(elementPayload => Encoding.UTF8.GetByteCount(elementPayload));
        return databaseBytes + Encoding.UTF8.GetByteCount(payload)
            <= PresentationElement.MaximumDocumentPayloadBytes;
    }

    private async Task TryDeleteAsync(MaterialObjectReference reference)
    {
        try
        {
            await storage.DeleteAsync(reference, CancellationToken.None);
        }
        catch
        {
            // The authoritative write outcome is retained; storage cleanup is operationally observed.
        }
    }

    private static MaterialObjectReference Reference(PresentationAsset asset) =>
        new(asset.StorageProvider, asset.StorageContainer, asset.ObjectKey);

    private static bool TryRowVersion(string? value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value ?? string.Empty);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static PresentationMutationResult Success(Guid? elementId = null) =>
        new(PresentationMutationOutcome.Success, ElementId: elementId);

    private static PresentationMutationResult Invalid() =>
        new(PresentationMutationOutcome.InvalidInput);

    private static PresentationMutationResult NotFound() =>
        new(PresentationMutationOutcome.NotFound);

    private static PresentationMutationResult Conflict() =>
        new(PresentationMutationOutcome.Conflict);

    private sealed record MutableState(
        PresentationDocument? Document,
        PresentationSlide? Slide,
        List<PresentationElement>? Elements,
        PresentationMutationResult? Result);
}
