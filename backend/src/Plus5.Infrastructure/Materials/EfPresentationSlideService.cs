using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfPresentationSlideService(
    Plus5DbContext db,
    TimeProvider timeProvider) : IPresentationSlideService
{
    private const int TemporaryOrderOffset = 1000;

    public async Task<PresentationMutationResult> InitializeAsync(
        Guid teacherAccountId,
        Guid materialId,
        InitializePresentationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedMaterialRowVersion, out var expected)
            || command.MaterialVersionId == Guid.Empty)
        {
            return Invalid();
        }

        var material = await db.Materials.SingleOrDefaultAsync(
            item => item.Id == materialId
                && item.OwnerTeacherId == teacherAccountId
                && item.Status == MaterialStatus.Active
                && item.ArchivedAtUtc == null,
            cancellationToken);
        if (material is null)
        {
            return NotFound();
        }

        if (!material.RowVersion.AsSpan().SequenceEqual(expected))
        {
            return Conflict();
        }

        var version = await db.MaterialVersions.SingleOrDefaultAsync(
            item => item.Id == command.MaterialVersionId
                && item.MaterialId == materialId
                && item.Status == MaterialVersionStatus.Draft
                && item.MaterialTypeCode == "PRESENTATION",
            cancellationToken);
        if (version is null)
        {
            return NotFound();
        }

        if (await db.PresentationDocuments.AnyAsync(
                item => item.MaterialVersionId == version.Id,
                cancellationToken))
        {
            return Conflict();
        }

        var now = timeProvider.GetUtcNow();
        var document = new PresentationDocument(version, now);
        var firstSlide = new PresentationSlide(Guid.NewGuid(), document, 0);
        db.PresentationDocuments.Add(document);
        db.PresentationSlides.Add(firstSlide);
        material.RecordVersionChange(now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Success(firstSlide.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }
    }

    public async Task<PresentationMutationResult> AddAsync(
        Guid teacherAccountId,
        Guid materialId,
        AddPresentationSlideCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected))
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        var slides = state.Slides!;
        if (slides.Count >= PresentationDocument.MaximumSlides)
        {
            return new(PresentationMutationOutcome.SlideLimitReached);
        }

        var insertIndex = slides.Count;
        if (command.AfterSlideId.HasValue)
        {
            var sourceIndex = slides.FindIndex(slide => slide.Id == command.AfterSlideId.Value);
            if (sourceIndex < 0)
            {
                return Invalid();
            }

            insertIndex = sourceIndex + 1;
        }

        var slide = new PresentationSlide(
            Guid.NewGuid(),
            state.Document!,
            TemporaryOrderOffset + slides.Count);
        slides.Insert(insertIndex, slide);
        db.PresentationSlides.Add(slide);

        try
        {
            await ApplyOrderAsync(state.Document!, slides, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success(slide.Id);
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
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty)
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        if (state.Slides!.Count == 1)
        {
            return new(PresentationMutationOutcome.LastSlide);
        }

        var slide = state.Slides.SingleOrDefault(item => item.Id == slideId);
        if (slide is null)
        {
            return NotFound();
        }

        state.Slides.Remove(slide);
        var slideElements = await db.PresentationElements
            .Where(element => element.PresentationSlideId == slide.Id)
            .ToListAsync(cancellationToken);
        var taskVersionIds = slideElements
            .Where(element => element.AssessableTaskVersionId.HasValue)
            .Select(element => element.AssessableTaskVersionId!.Value)
            .ToList();
        var taskVersions = await db.AssessableTaskVersions
            .Where(version => taskVersionIds.Contains(version.Id))
            .ToListAsync(cancellationToken);
        var mappings = await db.AssessableTaskVersionKnowledgeComponents
            .Where(mapping => taskVersionIds.Contains(mapping.AssessableTaskVersionId))
            .ToListAsync(cancellationToken);
        var taskIds = taskVersions.Select(version => version.AssessableTaskId).ToList();
        var removableTasks = await db.AssessableTasks
            .Where(task => taskIds.Contains(task.Id)
                && !db.AssessableTaskVersions.Any(version =>
                    version.AssessableTaskId == task.Id
                    && !taskVersionIds.Contains(version.Id)))
            .ToListAsync(cancellationToken);
        db.AssessableTaskVersionKnowledgeComponents.RemoveRange(mappings);
        db.PresentationElements.RemoveRange(slideElements);
        db.AssessableTaskVersions.RemoveRange(taskVersions);
        db.AssessableTasks.RemoveRange(removableTasks);
        db.PresentationSlides.Remove(slide);

        try
        {
            await ApplyOrderAsync(state.Document!, state.Slides, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict();
        }
    }

    public async Task<PresentationMutationResult> DuplicateAsync(
        Guid teacherAccountId,
        Guid materialId,
        Guid slideId,
        PresentationConcurrencyCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || slideId == Guid.Empty)
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        if (state.Slides!.Count >= PresentationDocument.MaximumSlides)
        {
            return new(PresentationMutationOutcome.SlideLimitReached);
        }

        var sourceIndex = state.Slides.FindIndex(slide => slide.Id == slideId);
        if (sourceIndex < 0)
        {
            return NotFound();
        }

        var duplicate = new PresentationSlide(
            Guid.NewGuid(),
            state.Document!,
            TemporaryOrderOffset + state.Slides.Count);
        state.Slides.Insert(sourceIndex + 1, duplicate);
        db.PresentationSlides.Add(duplicate);
        var sourceElements = await db.PresentationElements
            .AsNoTracking()
            .Where(element => element.PresentationSlideId == slideId)
            .OrderBy(element => element.ZIndex)
            .ToListAsync(cancellationToken);
        var sourceTaskVersionIds = sourceElements
            .Where(element => element.AssessableTaskVersionId.HasValue)
            .Select(element => element.AssessableTaskVersionId!.Value)
            .ToList();
        var sourceTaskVersions = await db.AssessableTaskVersions
            .AsNoTracking()
            .Where(version => sourceTaskVersionIds.Contains(version.Id))
            .ToDictionaryAsync(version => version.Id, cancellationToken);
        var material = await db.Materials.SingleAsync(
            candidate => candidate.Id == materialId,
            cancellationToken);
        var materialVersion = await db.MaterialVersions.SingleAsync(
            candidate => candidate.Id == state.Document!.MaterialVersionId,
            cancellationToken);
        var maximumSortOrder = await db.AssessableTaskVersions
            .Where(version => version.MaterialVersionId == materialVersion.Id)
            .Select(version => (int?)version.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;
        var now = timeProvider.GetUtcNow();
        var taskVersionMap = new Dictionary<Guid, Guid>();
        foreach (var sourceTaskVersion in sourceTaskVersions.Values.OrderBy(version => version.SortOrder))
        {
            var task = new AssessableTask(Guid.NewGuid(), material, now);
            var taskVersion = new AssessableTaskVersion(
                Guid.NewGuid(),
                task,
                materialVersion,
                1,
                ++maximumSortOrder,
                sourceTaskVersion.Prompt,
                sourceTaskVersion.TaskTypeCode,
                sourceTaskVersion.Difficulty,
                sourceTaskVersion.EvidenceType,
                sourceTaskVersion.MaxPoints,
                now,
                sourceTaskVersion.CorrectAnswer,
                sourceTaskVersion.EvaluationCriterion,
                sourceTaskVersion.AuthoringPayloadJson);
            db.AddRange(task, taskVersion);
            taskVersionMap.Add(sourceTaskVersion.Id, taskVersion.Id);
        }

        db.PresentationElements.AddRange(sourceElements.Select(element =>
            new PresentationElement(
                Guid.NewGuid(),
                duplicate,
                element.ElementTypeCode,
                element.ZIndex,
                element.X,
                element.Y,
                element.Width,
                element.Height,
                element.PayloadJson,
                element.PresentationAssetId,
                element.AssessableTaskVersionId.HasValue
                    ? taskVersionMap[element.AssessableTaskVersionId.Value]
                    : null)));

        try
        {
            await ApplyOrderAsync(state.Document!, state.Slides, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Success(duplicate.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict();
        }
    }

    public async Task<PresentationMutationResult> ReorderAsync(
        Guid teacherAccountId,
        Guid materialId,
        ReorderPresentationSlidesCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(command.ExpectedDocumentRowVersion, out var expected)
            || command.OrderedSlideIds is null
            || command.OrderedSlideIds.Count is < 1 or > PresentationDocument.MaximumSlides
            || command.OrderedSlideIds.Any(id => id == Guid.Empty)
            || command.OrderedSlideIds.Distinct().Count() != command.OrderedSlideIds.Count)
        {
            return Invalid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadMutableAsync(
            teacherAccountId,
            materialId,
            expected,
            cancellationToken);
        if (state.Result is not null)
        {
            return state.Result;
        }

        var byId = state.Slides!.ToDictionary(slide => slide.Id);
        if (byId.Count != command.OrderedSlideIds.Count
            || command.OrderedSlideIds.Any(id => !byId.ContainsKey(id)))
        {
            return Invalid();
        }

        var ordered = command.OrderedSlideIds.Select(id => byId[id]).ToList();
        try
        {
            await ApplyOrderAsync(state.Document!, ordered, cancellationToken);
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
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        var materialVersionId = await (
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
        if (!materialVersionId.HasValue)
        {
            return new MutableState(null, null, NotFound());
        }

        var document = await db.PresentationDocuments.SingleOrDefaultAsync(
            item => item.MaterialVersionId == materialVersionId.Value,
            cancellationToken);
        if (document is null)
        {
            return new MutableState(null, null, NotFound());
        }

        if (!document.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            return new MutableState(null, null, Conflict());
        }

        var slides = await db.PresentationSlides
            .Where(slide => slide.MaterialVersionId == materialVersionId.Value)
            .OrderBy(slide => slide.SortOrder)
            .ToListAsync(cancellationToken);
        if (slides.Count == 0)
        {
            return new MutableState(null, null, Conflict());
        }

        document.RecordChange(timeProvider.GetUtcNow());
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new MutableState(null, null, Conflict());
        }

        return new MutableState(document, slides, null);
    }

    private async Task ApplyOrderAsync(
        PresentationDocument document,
        List<PresentationSlide> slides,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < slides.Count; index++)
        {
            slides[index].SetSortOrder(TemporaryOrderOffset + index);
        }

        await db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < slides.Count; index++)
        {
            slides[index].SetSortOrder(index);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryRowVersion(string? value, out byte[] rowVersion)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            rowVersion = [];
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static PresentationMutationResult Success(Guid? slideId = null) =>
        new(PresentationMutationOutcome.Success, slideId);

    private static PresentationMutationResult Invalid() =>
        new(PresentationMutationOutcome.InvalidInput);

    private static PresentationMutationResult NotFound() =>
        new(PresentationMutationOutcome.NotFound);

    private static PresentationMutationResult Conflict() =>
        new(PresentationMutationOutcome.Conflict);

    private sealed record MutableState(
        PresentationDocument? Document,
        List<PresentationSlide>? Slides,
        PresentationMutationResult? Result);
}
