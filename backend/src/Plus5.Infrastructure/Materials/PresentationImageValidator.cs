using System.Security.Cryptography;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;

namespace Plus5.Infrastructure.Materials;

public sealed class PresentationAssetValidator : IPresentationAssetValidator
{
    public async Task<PresentationAssetValidationResult> ValidateAsync(
        string elementTypeCode,
        string originalFileName,
        string declaredMediaType,
        long declaredSizeBytes,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || !content.CanSeek
            || !PresentationAsset.IsAllowedMediaType(declaredMediaType)
            || !string.Equals(
                PresentationAsset.KindForMediaType(declaredMediaType),
                elementTypeCode,
                StringComparison.Ordinal)
            || declaredSizeBytes <= 0
            || declaredSizeBytes > PresentationAsset.MaximumSizeBytes(elementTypeCode)
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
        var valid = declaredMediaType == "audio/mpeg"
            ? await IsStructurallyValidMp3Async(content, cancellationToken)
            : await HasExpectedSignatureAsync(content, declaredMediaType, cancellationToken);
        content.Position = 0;
        return valid
            ? new(true, "VALID", actualSize, Convert.ToHexString(checksum))
            : Invalid("SIGNATURE_INVALID");
    }

    private static async Task<bool> HasExpectedSignatureAsync(
        Stream content,
        string declaredMediaType,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        var header = new byte[12];
        var read = await content.ReadAsync(header, cancellationToken);
        return declaredMediaType switch
        {
            "image/png" => read >= 8
                && header.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/jpeg" => read >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/webp" => read >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "video/mp4" => read >= 12
                && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false,
        };
    }

    private static async Task<bool> IsStructurallyValidMp3Async(
        Stream content,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        var header = new byte[10];
        if (await ReadAtLeastAsync(content, header, 10, cancellationToken) < 4)
        {
            return false;
        }

        long offset = 0;
        if (header.AsSpan(0, 3).SequenceEqual("ID3"u8))
        {
            if (header[3] is 0xFF || header[4] is 0xFF
                || (header[6] & 0x80) != 0 || (header[7] & 0x80) != 0
                || (header[8] & 0x80) != 0 || (header[9] & 0x80) != 0)
            {
                return false;
            }

            var tagSize = (header[6] << 21) | (header[7] << 14) | (header[8] << 7) | header[9];
            offset = 10L + tagSize + ((header[5] & 0x10) != 0 ? 10 : 0);
            if (offset + 4 > content.Length)
            {
                return false;
            }
        }

        var frameCount = 0;
        while (offset + 4 <= content.Length)
        {
            if (content.Length - offset == 128)
            {
                content.Position = offset;
                var tag = new byte[3];
                await ReadAtLeastAsync(content, tag, 3, cancellationToken);
                return frameCount >= 2 && tag.AsSpan().SequenceEqual("TAG"u8);
            }

            content.Position = offset;
            var frameHeader = new byte[4];
            if (await ReadAtLeastAsync(content, frameHeader, 4, cancellationToken) != 4
                || !TryFrameLength(frameHeader, out var frameLength)
                || offset + frameLength > content.Length)
            {
                return false;
            }

            offset += frameLength;
            frameCount++;
        }

        return frameCount >= 2 && offset == content.Length;
    }

    private static bool TryFrameLength(ReadOnlySpan<byte> header, out int frameLength)
    {
        frameLength = 0;
        if (header.Length < 4 || header[0] != 0xFF || (header[1] & 0xE0) != 0xE0)
        {
            return false;
        }

        var version = (header[1] >> 3) & 0x03;
        var layer = (header[1] >> 1) & 0x03;
        var bitrateIndex = (header[2] >> 4) & 0x0F;
        var sampleRateIndex = (header[2] >> 2) & 0x03;
        var padding = (header[2] >> 1) & 0x01;
        if (version == 1 || layer == 0 || bitrateIndex is 0 or 15 || sampleRateIndex == 3)
        {
            return false;
        }

        int[] version1Layer1 = [32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448];
        int[] version1Layer2 = [32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384];
        int[] version1Layer3 = [32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
        int[] version2Layer1 = [32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256];
        int[] version2Layer23 = [8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160];
        int[] sampleRates = version switch
        {
            3 => [44100, 48000, 32000],
            2 => [22050, 24000, 16000],
            0 => [11025, 12000, 8000],
            _ => throw new InvalidOperationException(),
        };
        var bitrates = version == 3
            ? layer switch
            {
                3 => version1Layer1,
                2 => version1Layer2,
                _ => version1Layer3,
            }
            : layer == 3 ? version2Layer1 : version2Layer23;
        var bitrate = bitrates[bitrateIndex - 1] * 1000;
        var sampleRate = sampleRates[sampleRateIndex];
        frameLength = layer == 3
            ? ((12 * bitrate / sampleRate) + padding) * 4
            : layer == 1 && version != 3
                ? (72 * bitrate / sampleRate) + padding
                : (144 * bitrate / sampleRate) + padding;
        return frameLength > 4;
    }

    private static async Task<int> ReadAtLeastAsync(
        Stream content,
        byte[] buffer,
        int count,
        CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < count)
        {
            var read = await content.ReadAsync(buffer.AsMemory(total, count - total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
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

    private static PresentationAssetValidationResult Invalid(string category) =>
        new(false, category, 0, null);
}
