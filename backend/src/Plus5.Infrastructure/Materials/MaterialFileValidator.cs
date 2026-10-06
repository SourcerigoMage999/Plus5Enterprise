using System.IO.Compression;
using System.Security.Cryptography;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;

namespace Plus5.Infrastructure.Materials;

public sealed class MaterialFileValidator : IMaterialFileValidator
{
    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat", ".cmd", ".com", ".dll", ".exe", ".hta", ".jar", ".js", ".lnk",
        ".msi", ".ps1", ".scr", ".vbs", ".wsf",
    };

    public async Task<MaterialFileValidationResult> ValidateAsync(
        MaterialFileValidationRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || !content.CanSeek)
        {
            return Invalid("STREAM_NOT_SEEKABLE");
        }

        if (!string.Equals(
                Path.GetExtension(request.OriginalFileName),
                MaterialFilePolicy.RequiredExtension(request.Format),
                StringComparison.OrdinalIgnoreCase)
            || !MaterialFilePolicy.IsAllowedMediaType(request.Format, request.DeclaredMediaType)
            || request.DeclaredSizeBytes <= 0
            || request.DeclaredSizeBytes > MaterialFilePolicy.MaximumSizeBytes(request.Format))
        {
            return Invalid("DECLARATION_INVALID");
        }

        content.Position = 0;
        var checksum = await SHA256.HashDataAsync(content, cancellationToken);
        var actualSize = content.Position;
        if (actualSize != request.DeclaredSizeBytes)
        {
            return new MaterialFileValidationResult(false, "SIZE_MISMATCH", actualSize, null);
        }

        content.Position = 0;
        var validStructure = request.Format switch
        {
            MaterialFileFormat.Pdf => await HasPrefixAsync(content, "%PDF-"u8.ToArray(), cancellationToken),
            MaterialFileFormat.Mp4 => await IsMp4Async(content, cancellationToken),
            MaterialFileFormat.Docx => await ValidateZipAsync(content, "word/document.xml", false, cancellationToken),
            MaterialFileFormat.Pptx => await ValidateZipAsync(content, "ppt/presentation.xml", false, cancellationToken),
            MaterialFileFormat.Zip => await ValidateZipAsync(content, null, true, cancellationToken),
            _ => false,
        };

        content.Position = 0;
        return validStructure
            ? new MaterialFileValidationResult(true, "VALID", actualSize, Convert.ToHexString(checksum))
            : new MaterialFileValidationResult(false, "STRUCTURE_INVALID", actualSize, null);
    }

    private static MaterialFileValidationResult Invalid(string category) =>
        new(false, category, 0, null);

    private static async Task<bool> HasPrefixAsync(
        Stream content,
        byte[] expected,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[expected.Length];
        return await content.ReadAsync(buffer, cancellationToken) == buffer.Length
            && buffer.AsSpan().SequenceEqual(expected);
    }

    private static async Task<bool> IsMp4Async(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new byte[12];
        return await content.ReadAsync(buffer, cancellationToken) == buffer.Length
            && buffer.AsSpan(4, 4).SequenceEqual("ftyp"u8);
    }

    private static async Task<bool> ValidateZipAsync(
        Stream content,
        string? requiredPart,
        bool enforceGenericZipPolicy,
        CancellationToken cancellationToken)
    {
        try
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count is 0 or > MaterialFilePolicy.MaximumZipEntries)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var normalized = entry.FullName.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(normalized)
                    || normalized.StartsWith('/')
                    || Path.IsPathFullyQualified(normalized)
                    || normalized.Split('/').Any(segment => segment is ".." or ".")
                    || !names.Add(normalized))
                {
                    return false;
                }

                if (enforceGenericZipPolicy
                    && (ExecutableExtensions.Contains(Path.GetExtension(normalized))
                        || string.Equals(Path.GetExtension(normalized), ".zip", StringComparison.OrdinalIgnoreCase)
                        || IsUnixSymlink(entry)))
                {
                    return false;
                }

                expandedBytes = checked(expandedBytes + entry.Length);
                if (expandedBytes > MaterialFilePolicy.MaximumZipExpandedBytes
                    || (entry.CompressedLength == 0 && entry.Length > 0)
                    || (entry.CompressedLength > 0
                        && (decimal)entry.Length / entry.CompressedLength
                            > MaterialFilePolicy.MaximumZipEntryCompressionRatio))
                {
                    return false;
                }

                if (entry.Name.Length > 0)
                {
                    await using var entryStream = entry.Open();
                    await entryStream.CopyToAsync(Stream.Null, cancellationToken);
                }
            }

            return requiredPart is null
                || (names.Contains("[Content_Types].xml") && names.Contains(requiredPart));
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or OverflowException)
        {
            return false;
        }
    }

    private static bool IsUnixSymlink(ZipArchiveEntry entry) =>
        entry.ExternalAttributes != 0
        && ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;
}
