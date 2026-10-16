using System.Text;

namespace Plus5.Domain.Materials;

public sealed class PresentationElement
{
    public const int MaximumElementsPerSlide = 200;
    public const int MaximumPayloadBytes = 256 * 1024;
    public const int MaximumDocumentPayloadBytes = 8 * 1024 * 1024;
    public const int CurrentSchemaVersion = 1;

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        "TEXT",
        "IMAGE",
        "SHAPE",
        "TABLE",
    };

    private PresentationElement()
    {
    }

    public PresentationElement(
        Guid id,
        PresentationSlide slide,
        string elementTypeCode,
        int zIndex,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string payloadJson,
        Guid? presentationAssetId = null)
    {
        ArgumentNullException.ThrowIfNull(slide);
        MaterialGuard.Identifier(id, nameof(id));

        Id = id;
        MaterialVersionId = slide.MaterialVersionId;
        PresentationSlideId = slide.Id;
        ElementSchemaVersion = CurrentSchemaVersion;
        SetTypeAndPayload(elementTypeCode, payloadJson, presentationAssetId);
        SetLayout(zIndex, x, y, width, height);
    }

    public Guid Id { get; private set; }

    public Guid MaterialVersionId { get; private set; }

    public Guid PresentationSlideId { get; private set; }

    public string ElementTypeCode { get; private set; } = string.Empty;

    public int ElementSchemaVersion { get; private set; }

    public int ZIndex { get; private set; }

    public decimal X { get; private set; }

    public decimal Y { get; private set; }

    public decimal Width { get; private set; }

    public decimal Height { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public Guid? PresentationAssetId { get; private set; }

    public void Update(
        int zIndex,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string payloadJson)
    {
        SetTypeAndPayload(ElementTypeCode, payloadJson, PresentationAssetId);
        SetLayout(zIndex, x, y, width, height);
    }

    public static bool IsSupportedType(string? value) =>
        value is not null && SupportedTypes.Contains(value);

    private void SetTypeAndPayload(
        string elementTypeCode,
        string payloadJson,
        Guid? presentationAssetId)
    {
        var normalizedType = MaterialGuard.Code(
            elementTypeCode,
            32,
            nameof(elementTypeCode));
        if (!SupportedTypes.Contains(normalizedType))
        {
            throw new ArgumentException("Unsupported presentation element type.", nameof(elementTypeCode));
        }

        if ((normalizedType == "IMAGE") != presentationAssetId.HasValue)
        {
            throw new ArgumentException(
                "Only IMAGE elements require a presentation asset.",
                nameof(presentationAssetId));
        }

        if (presentationAssetId.HasValue)
        {
            MaterialGuard.Identifier(presentationAssetId.Value, nameof(presentationAssetId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        if (Encoding.UTF8.GetByteCount(payloadJson) > MaximumPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadJson),
                "Presentation element payload exceeds the bounded size.");
        }

        ElementTypeCode = normalizedType;
        PayloadJson = payloadJson;
        PresentationAssetId = presentationAssetId;
    }

    private void SetLayout(
        int zIndex,
        decimal x,
        decimal y,
        decimal width,
        decimal height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(zIndex);

        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || x + width > PresentationDocument.CanvasWidthValue
            || y + height > PresentationDocument.CanvasHeightValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Element bounds must remain inside the logical presentation canvas.");
        }

        ZIndex = zIndex;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
