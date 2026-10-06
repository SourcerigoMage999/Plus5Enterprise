using System.IO.Compression;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialFileValidatorTests
{
    private readonly MaterialFileValidator validator = new();

    [Fact]
    public async Task ValidPdfReturnsChecksumAndActualSize()
    {
        await using var content = new MemoryStream("%PDF-1.7\nvalid"u8.ToArray());

        var result = await validator.ValidateAsync(
            Request(MaterialFileFormat.Pdf, "lesson.pdf", "application/pdf", content.Length),
            content,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal(content.Length, result.ActualSizeBytes);
        Assert.Equal(64, result.Sha256Checksum?.Length);
    }

    [Fact]
    public async Task DeclaredPdfWithWrongSignatureIsRejected()
    {
        await using var content = new MemoryStream("not a pdf"u8.ToArray());

        var result = await validator.ValidateAsync(
            Request(MaterialFileFormat.Pdf, "lesson.pdf", "application/pdf", content.Length),
            content,
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("STRUCTURE_INVALID", result.ResultCategory);
    }

    [Fact]
    public async Task DeclaredFormatWithDifferentExtensionIsRejected()
    {
        await using var content = new MemoryStream("%PDF-1.7\nvalid"u8.ToArray());

        var result = await validator.ValidateAsync(
            Request(MaterialFileFormat.Pdf, "lesson.docx", "application/pdf", content.Length),
            content,
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("DECLARATION_INVALID", result.ResultCategory);
    }

    [Fact]
    public async Task DocxRequiresOoxmlContentTypesAndWordDocumentPart()
    {
        await using var valid = CreateZip(
            ("[Content_Types].xml", "<Types />"),
            ("word/document.xml", "<document />"));
        await using var invalid = CreateZip(("[Content_Types].xml", "<Types />"));

        var validResult = await validator.ValidateAsync(
            Request(MaterialFileFormat.Docx, "lesson.docx", OoxmlWord, valid.Length),
            valid,
            CancellationToken.None);
        var invalidResult = await validator.ValidateAsync(
            Request(MaterialFileFormat.Docx, "lesson.docx", OoxmlWord, invalid.Length),
            invalid,
            CancellationToken.None);

        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("run.exe")]
    [InlineData("nested.zip")]
    public async Task UnsafeZipEntryIsRejected(string entryName)
    {
        await using var content = CreateZip((entryName, "payload"));

        var result = await validator.ValidateAsync(
            Request(MaterialFileFormat.Zip, "bundle.zip", "application/zip", content.Length),
            content,
            CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task SafeGenericZipIsAccepted()
    {
        await using var content = CreateZip(("worksheets/exercise.txt", "safe"));

        var result = await validator.ValidateAsync(
            Request(MaterialFileFormat.Zip, "bundle.zip", "application/zip", content.Length),
            content,
            CancellationToken.None);

        Assert.True(result.IsValid);
    }

    private static MaterialFileValidationRequest Request(
        MaterialFileFormat format,
        string name,
        string mediaType,
        long size) => new(format, name, mediaType, size);

    private static MemoryStream CreateZip(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry.Name).Open());
                writer.Write(entry.Content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private const string OoxmlWord =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
}
