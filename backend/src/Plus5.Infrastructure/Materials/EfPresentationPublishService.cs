using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationPublishService(
    Plus5DbContext db,
    IMaterialObjectStorage storage,
    TimeProvider timeProvider) : IPresentationPublishService
{
    private const string PackageName = "plus5-presentation.zip";

    public async Task<PresentationPublishResult> PublishAsync(
        Guid teacherAccountId,
        Guid materialId,
        PublishPresentationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedMaterialRowVersion, out var expectedMaterial)
            || !TryRowVersion(command.ExpectedDocumentRowVersion, out var expectedDocument))
        {
            return new(PresentationPublishOutcome.InvalidInput);
        }

        var snapshot = await LoadSnapshotAsync(
            teacherAccountId,
            materialId,
            command.MaterialVersionId,
            cancellationToken);
        if (snapshot is null)
        {
            return new(PresentationPublishOutcome.NotFound);
        }

        if (!snapshot.MaterialRowVersion.SequenceEqual(expectedMaterial)
            || !snapshot.DocumentRowVersion.SequenceEqual(expectedDocument))
        {
            return new(PresentationPublishOutcome.Conflict);
        }

        var errors = Validate(snapshot);
        if (errors.Count > 0)
        {
            return new(PresentationPublishOutcome.ValidationFailed, errors);
        }

        var package = CreatePackage(snapshot);
        var checksum = Convert.ToHexString(SHA256.HashData(package));
        var now = timeProvider.GetUtcNow();
        var generatedFile = new MaterialFile(
            Guid.NewGuid(),
            snapshot.Material,
            snapshot.Draft,
            MaterialFileFormat.Zip,
            PackageName,
            MaterialFilePolicy.PresentationPackageMediaType,
            package.LongLength,
            snapshot.StorageProvider,
            snapshot.StorageContainer,
            now);
        generatedFile.MarkServerGenerated(package.LongLength, checksum, now);
        var generatedReference = FileReference(generatedFile);

        try
        {
            await using (var content = new MemoryStream(package, writable: false))
            {
                await storage.PutCleanAsync(
                    new MaterialObjectWrite(
                        generatedReference,
                        package.LongLength,
                        MaterialFilePolicy.PresentationPackageMediaType,
                        checksum),
                    content,
                    cancellationToken);
            }
        }
        catch (AmazonS3Exception)
        {
            return new(PresentationPublishOutcome.StorageFailure);
        }

        MaterialObjectReference? oldReference = null;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var material = await db.Materials.SingleOrDefaultAsync(
                candidate => candidate.Id == materialId
                    && candidate.OwnerTeacherId == teacherAccountId
                    && candidate.Status == MaterialStatus.Active
                    && candidate.ArchivedAtUtc == null,
                cancellationToken);
            var draft = await db.MaterialVersions.SingleOrDefaultAsync(
                version => version.Id == command.MaterialVersionId
                    && version.MaterialId == materialId
                    && version.Status == MaterialVersionStatus.Draft
                    && version.MaterialTypeCode == "PRESENTATION",
                cancellationToken);
            var document = draft is null ? null : await db.PresentationDocuments.SingleOrDefaultAsync(
                candidate => candidate.MaterialVersionId == draft.Id,
                cancellationToken);
            var current = material?.CurrentVersionId is Guid currentVersionId
                ? await db.MaterialVersions.SingleOrDefaultAsync(
                    version => version.Id == currentVersionId
                        && version.Status == MaterialVersionStatus.Active,
                    cancellationToken)
                : null;
            var oldFile = draft is null ? null : await db.MaterialFiles.SingleOrDefaultAsync(
                file => file.MaterialVersionId == draft.Id,
                cancellationToken);

            if (material is null || draft is null || document is null
                || current is null || oldFile is null
                || !material.RowVersion.SequenceEqual(expectedMaterial)
                || !document.RowVersion.SequenceEqual(expectedDocument))
            {
                await transaction.RollbackAsync(cancellationToken);
                await TryDeleteAsync(generatedReference);
                return new(PresentationPublishOutcome.Conflict);
            }

            oldReference = FileReference(oldFile);
            db.MaterialFiles.Remove(oldFile);
            await db.SaveChangesAsync(cancellationToken);

            db.MaterialFiles.Add(generatedFile);
            await db.SaveChangesAsync(cancellationToken);

            material.RecordVersionChange(now);
            await db.SaveChangesAsync(cancellationToken);
            current.Supersede(now);
            await db.SaveChangesAsync(cancellationToken);
            draft.Activate(generatedFile, now);
            await db.SaveChangesAsync(cancellationToken);

            await db.Entry(material).ReloadAsync(cancellationToken);
            if (material.CurrentVersionId != draft.Id)
            {
                throw new InvalidOperationException(
                    "Published presentation version was not made current.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await TryDeleteAsync(generatedReference);
            return new(PresentationPublishOutcome.Conflict);
        }
        catch (AmazonS3Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            await TryDeleteAsync(generatedReference);
            return new(PresentationPublishOutcome.StorageFailure);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await TryDeleteAsync(generatedReference);
            throw;
        }

        if (oldReference is not null)
        {
            await TryDeleteAsync(oldReference);
        }

        return new(PresentationPublishOutcome.Success);
    }

    private async Task<PublishSnapshot?> LoadSnapshotAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == materialId
                && candidate.OwnerTeacherId == teacherAccountId
                && candidate.Status == MaterialStatus.Active
                && candidate.ArchivedAtUtc == null,
            cancellationToken);
        var draft = material is null ? null : await db.MaterialVersions.AsNoTracking()
            .SingleOrDefaultAsync(version => version.Id == versionId
                && version.MaterialId == materialId
                && version.Status == MaterialVersionStatus.Draft
                && version.MaterialTypeCode == "PRESENTATION",
                cancellationToken);
        var document = draft is null ? null : await db.PresentationDocuments.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.MaterialVersionId == draft.Id, cancellationToken);
        var currentExists = material?.CurrentVersionId is Guid currentVersionId
            && await db.MaterialVersions.AsNoTracking().AnyAsync(
                version => version.Id == currentVersionId
                    && version.Status == MaterialVersionStatus.Active,
                cancellationToken);
        var oldFile = draft is null ? null : await db.MaterialFiles.AsNoTracking()
            .SingleOrDefaultAsync(file => file.MaterialVersionId == draft.Id, cancellationToken);
        if (material is null || draft is null || document is null || !currentExists || oldFile is null)
        {
            return null;
        }

        var slides = await db.PresentationSlides.AsNoTracking()
            .Where(slide => slide.MaterialVersionId == draft.Id)
            .OrderBy(slide => slide.SortOrder)
            .ToListAsync(cancellationToken);
        var elements = await db.PresentationElements.AsNoTracking()
            .Where(element => element.MaterialVersionId == draft.Id)
            .OrderBy(element => element.PresentationSlideId)
            .ThenBy(element => element.ZIndex)
            .ThenBy(element => element.Id)
            .ToListAsync(cancellationToken);
        var assets = await db.PresentationAssets.AsNoTracking()
            .Where(asset => asset.MaterialVersionId == draft.Id)
            .OrderBy(asset => asset.Id)
            .ToListAsync(cancellationToken);
        var quickCheckVersionIds = elements
            .Where(element => element.AssessableTaskVersionId.HasValue)
            .Select(element => element.AssessableTaskVersionId!.Value)
            .Distinct()
            .ToList();
        var mappedQuickChecks = await db.AssessableTaskVersionKnowledgeComponents.AsNoTracking()
            .Where(mapping => quickCheckVersionIds.Contains(mapping.AssessableTaskVersionId))
            .Select(mapping => mapping.AssessableTaskVersionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return new(
            material,
            draft,
            document.RowVersion,
            slides,
            elements,
            assets,
            mappedQuickChecks.ToHashSet(),
            oldFile.StorageProvider,
            oldFile.StorageContainer);
    }

    private static List<string> Validate(PublishSnapshot snapshot)
    {
        var errors = new List<string>();
        if (snapshot.Slides.Count is < 1 or > PresentationDocument.MaximumSlides
            || snapshot.Slides.Select((slide, index) => slide.SortOrder == index).Any(valid => !valid))
        {
            errors.Add("presentation_slides_invalid");
        }

        if (snapshot.DocumentRowVersion.Length == 0)
        {
            errors.Add("presentation_document_version_invalid");
        }

        var assets = snapshot.Assets.ToDictionary(asset => asset.Id);
        foreach (var element in snapshot.Elements)
        {
            if (!PresentationElement.IsSupportedType(element.ElementTypeCode)
                || element.ElementSchemaVersion != PresentationElement.CurrentSchemaVersion)
            {
                errors.Add("presentation_element_schema_unsupported");
                continue;
            }

            if (PresentationElement.RequiresAsset(element.ElementTypeCode)
                && (!element.PresentationAssetId.HasValue
                    || !assets.TryGetValue(element.PresentationAssetId.Value, out var asset)
                    || asset.Status != PresentationAssetStatus.Clean
                    || asset.MaterialVersionId != snapshot.Draft.Id
                    || asset.AssetKindCode != element.ElementTypeCode))
            {
                errors.Add("presentation_asset_invalid");
            }

            if (PresentationElement.RequiresAssessment(element.ElementTypeCode)
                && (!element.AssessableTaskVersionId.HasValue
                    || !snapshot.MappedQuickChecks.Contains(element.AssessableTaskVersionId.Value)))
            {
                errors.Add("presentation_quick_check_mapping_required");
            }

            try
            {
                using var _ = JsonDocument.Parse(element.PayloadJson);
            }
            catch (JsonException)
            {
                errors.Add("presentation_payload_invalid");
            }
        }

        return errors.Distinct(StringComparer.Ordinal).ToList();
    }

    private static byte[] CreatePackage(PublishSnapshot snapshot)
    {
        var manifest = new PackageManifest(
            1,
            snapshot.Draft.Id,
            Convert.ToBase64String(snapshot.DocumentRowVersion),
            PresentationDocument.CurrentSchemaVersion,
            PresentationDocument.CanvasWidthValue,
            PresentationDocument.CanvasHeightValue,
            snapshot.Slides.Select(slide => new PackageSlide(
                slide.Id,
                slide.SortOrder,
                snapshot.Elements
                    .Where(element => element.PresentationSlideId == slide.Id)
                    .OrderBy(element => element.ZIndex)
                    .ThenBy(element => element.Id)
                    .Select(element => new PackageElement(
                        element.Id,
                        element.ElementTypeCode,
                        element.ElementSchemaVersion,
                        element.ZIndex,
                        element.X,
                        element.Y,
                        element.Width,
                        element.Height,
                        JsonDocument.Parse(element.PayloadJson).RootElement.Clone(),
                        element.PresentationAssetId,
                        element.AssessableTaskVersionId))
                    .ToList()))
                .ToList(),
            snapshot.Assets.Select(asset => new PackageAsset(
                asset.Id,
                asset.AssetKindCode,
                asset.MediaType,
                asset.SizeBytes,
                asset.Sha256Checksum))
                .ToList());
        var json = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("manifest.json", CompressionLevel.NoCompression);
            entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var entryStream = entry.Open();
            entryStream.Write(json);
        }

        return output.ToArray();
    }

    private async Task TryDeleteAsync(MaterialObjectReference reference)
    {
        try
        {
            await storage.DeleteAsync(reference, CancellationToken.None);
        }
        catch
        {
            // Compensation is best effort; object lifecycle cleanup handles an orphan.
        }
    }

    private static MaterialObjectReference FileReference(MaterialFile file) =>
        new(file.StorageProvider, file.StorageContainer, file.ObjectKey);

    private static bool TryRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private sealed record PublishSnapshot(
        Material Material,
        MaterialVersion Draft,
        byte[] DocumentRowVersion,
        IReadOnlyList<PresentationSlide> Slides,
        IReadOnlyList<PresentationElement> Elements,
        IReadOnlyList<PresentationAsset> Assets,
        IReadOnlySet<Guid> MappedQuickChecks,
        string StorageProvider,
        string StorageContainer)
    {
        public byte[] MaterialRowVersion => Material.RowVersion;
    }

    private sealed record PackageManifest(
        int SchemaVersion,
        Guid MaterialVersionId,
        string DocumentRowVersion,
        int DocumentSchemaVersion,
        int CanvasWidth,
        int CanvasHeight,
        IReadOnlyList<PackageSlide> Slides,
        IReadOnlyList<PackageAsset> Assets);

    private sealed record PackageSlide(
        Guid Id,
        int SortOrder,
        IReadOnlyList<PackageElement> Elements);

    private sealed record PackageElement(
        Guid Id,
        string TypeCode,
        int SchemaVersion,
        int ZIndex,
        decimal X,
        decimal Y,
        decimal Width,
        decimal Height,
        JsonElement Payload,
        Guid? PresentationAssetId,
        Guid? AssessableTaskVersionId);

    private sealed record PackageAsset(
        Guid Id,
        string KindCode,
        string MediaType,
        long SizeBytes,
        string Sha256Checksum);
}
