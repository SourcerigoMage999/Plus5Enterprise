using System.Text.Json;
using System.Text.RegularExpressions;

namespace Plus5.Application.Materials;

public static partial class PresentationElementPayloadRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryNormalize(string typeCode, string payloadJson, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return typeCode switch
            {
                "TEXT" => TryText(document.RootElement, out normalized),
                "IMAGE" => TryImage(document.RootElement, out normalized),
                "SHAPE" => TryShape(document.RootElement, out normalized),
                "TABLE" => TryTable(document.RootElement, out normalized),
                _ => false,
            };
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string Image(string altText, string fit) => JsonSerializer.Serialize(
        new ImagePayload(altText.Trim(), fit),
        JsonOptions);

    private static bool TryText(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "text", "fontSize", "fontWeight", "color", "align")
            || !TryString(root, "text", 4000, true, out var text)
            || !TryInt(root, "fontSize", 12, 160, out var fontSize)
            || !TryChoice(root, "fontWeight", ["regular", "bold"], out var weight)
            || !TryColor(root, "color", out var color)
            || !TryChoice(root, "align", ["left", "center", "right"], out var align))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new TextPayload(text, fontSize, weight, color, align),
            JsonOptions);
        return true;
    }

    private static bool TryImage(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "altText", "fit")
            || !TryString(root, "altText", 500, false, out var altText)
            || !TryChoice(root, "fit", ["contain", "cover"], out var fit))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(new ImagePayload(altText, fit), JsonOptions);
        return true;
    }

    private static bool TryShape(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "shape", "fill", "stroke", "strokeWidth")
            || !TryChoice(root, "shape", ["rectangle", "ellipse", "line"], out var shape)
            || !TryColor(root, "fill", out var fill)
            || !TryColor(root, "stroke", out var stroke)
            || !TryInt(root, "strokeWidth", 0, 20, out var strokeWidth))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new ShapePayload(shape, fill, stroke, strokeWidth),
            JsonOptions);
        return true;
    }

    private static bool TryTable(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "cells", "headerRow")
            || !root.TryGetProperty("headerRow", out var header)
            || header.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
            || !root.TryGetProperty("cells", out var cells)
            || cells.ValueKind != JsonValueKind.Array
            || cells.GetArrayLength() is < 1 or > 20)
        {
            return false;
        }

        var rows = new List<IReadOnlyList<string>>();
        int? columns = null;
        foreach (var row in cells.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array
                || row.GetArrayLength() is < 1 or > 10
                || columns.HasValue && columns.Value != row.GetArrayLength())
            {
                return false;
            }

            columns ??= row.GetArrayLength();
            var values = new List<string>();
            foreach (var cell in row.EnumerateArray())
            {
                if (cell.ValueKind != JsonValueKind.String
                    || cell.GetString() is not { } value
                    || value.Length > 500)
                {
                    return false;
                }

                values.Add(value.Trim());
            }

            rows.Add(values);
        }

        normalized = JsonSerializer.Serialize(
            new TablePayload(rows, header.GetBoolean()),
            JsonOptions);
        return true;
    }

    private static bool HasOnly(JsonElement root, params string[] allowed)
    {
        var names = root.EnumerateObject().Select(property => property.Name).ToList();
        return names.Count == allowed.Length
            && names.Distinct(StringComparer.Ordinal).Count() == names.Count
            && names.All(name => allowed.Contains(name, StringComparer.Ordinal));
    }

    private static bool TryString(
        JsonElement root,
        string name,
        int maxLength,
        bool required,
        out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String
            || property.GetString() is not { } source)
        {
            return false;
        }

        value = source.Trim();
        return value.Length <= maxLength && (!required || value.Length > 0);
    }

    private static bool TryChoice(
        JsonElement root,
        string name,
        IReadOnlyCollection<string> allowed,
        out string value) =>
        TryString(root, name, 32, true, out value) && allowed.Contains(value);

    private static bool TryColor(JsonElement root, string name, out string value) =>
        TryString(root, name, 7, true, out value) && ColorRegex().IsMatch(value);

    private static bool TryInt(
        JsonElement root,
        string name,
        int minimum,
        int maximum,
        out int value)
    {
        value = 0;
        return root.TryGetProperty(name, out var property)
            && property.TryGetInt32(out value)
            && value >= minimum
            && value <= maximum;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();

    private sealed record TextPayload(
        string Text,
        int FontSize,
        string FontWeight,
        string Color,
        string Align);

    private sealed record ImagePayload(string AltText, string Fit);

    private sealed record ShapePayload(
        string Shape,
        string Fill,
        string Stroke,
        int StrokeWidth);

    private sealed record TablePayload(
        IReadOnlyList<IReadOnlyList<string>> Cells,
        bool HeaderRow);
}
