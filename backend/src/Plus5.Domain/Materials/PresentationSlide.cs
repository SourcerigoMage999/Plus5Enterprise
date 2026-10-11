namespace Plus5.Domain.Materials;

public sealed class PresentationSlide
{
    private PresentationSlide()
    {
    }

    public PresentationSlide(Guid id, PresentationDocument document, int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(document);
        MaterialGuard.Identifier(id, nameof(id));
        SetSortOrder(sortOrder);

        Id = id;
        MaterialVersionId = document.MaterialVersionId;
    }

    public Guid Id { get; private set; }

    public Guid MaterialVersionId { get; private set; }

    public int SortOrder { get; private set; }

    public void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Slide sort order cannot be negative.");
        }

        SortOrder = sortOrder;
    }
}
