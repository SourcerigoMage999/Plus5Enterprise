namespace Plus5.Domain.Materials;

public sealed class PresentationDocument
{
    public const int CurrentSchemaVersion = 1;
    public const int CanvasWidthValue = 1600;
    public const int CanvasHeightValue = 900;
    public const int MaximumSlides = 200;

    private PresentationDocument()
    {
    }

    public PresentationDocument(MaterialVersion materialVersion, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(materialVersion);
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        materialVersion.EnsureDraft();

        if (!string.Equals(
                materialVersion.MaterialTypeCode,
                "PRESENTATION",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A presentation document requires a PRESENTATION material version.",
                nameof(materialVersion));
        }

        MaterialVersionId = materialVersion.Id;
        DocumentSchemaVersion = CurrentSchemaVersion;
        CanvasWidth = CanvasWidthValue;
        CanvasHeight = CanvasHeightValue;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid MaterialVersionId { get; private set; }

    public int DocumentSchemaVersion { get; private set; }

    public int CanvasWidth { get; private set; }

    public int CanvasHeight { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void RecordChange(DateTimeOffset updatedAtUtc)
    {
        MaterialGuard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "Document update cannot precede the previous update.");
        }

        UpdatedAtUtc = updatedAtUtc;
    }
}
