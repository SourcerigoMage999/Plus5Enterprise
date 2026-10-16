using System.Security.Cryptography;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;

namespace Plus5.Infrastructure.Materials;

public sealed class PresentationImageValidator : IPresentationImageValidator
{
    public async Task<PresentationImageValidationResult> ValidateAsync(
        string originalFileName,
        string declaredMediaType,
        long declaredSizeBytes,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || !content.CanSeek
            || !PresentationAsset.IsAllowedMediaType(declaredMediaType)
            || declaredSizeBytes is <= 0 or > PresentationAsset.MaximumImageSizeBytes
            || !HasExpectedExtension(originalFileName, declaredMediaType))
        {
            return Invalid("DECLARATION_INVALID");
        }

        content.Position = 0;
        var checksum = await SHA256.HashDataAsync(content, cancellationToken);
        var actualSize = content.Position;
        if (actualSize != declaredSizeBytes)
        {
            return new(false, "SIZE_MISMATCH", actualSize, null);
        }

        content.Position = 0;
        var header = new byte[12];
        var read = await content.ReadAsync(header, cancellationToken);
        var valid = declaredMediaType switch
        {
            "image/png" => read >= 8
                && header.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/jpeg" => read >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/webp" => read >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };
        content.Position = 0;
        return valid
            ? new(true, "VALID", actualSize, Convert.ToHexString(checksum))
            : Invalid("SIGNATURE_INVALID");
    }

    private static bool HasExpectedExtension(string fileName, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.IndexOfAny(['/', '\\']) >= 0
            || fileName.Any(char.IsControl))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return mediaType == "image/jpeg"
            ? extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            : extension.Equals(
                PresentationAsset.RequiredExtension(mediaType),
                StringComparison.OrdinalIgnoreCase);
    }

    private static PresentationImageValidationResult Invalid(string category) =>
        new(false, category, 0, null);
}
