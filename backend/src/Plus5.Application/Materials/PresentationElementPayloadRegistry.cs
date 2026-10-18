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
                "RULE" => TryRule(document.RootElement, out normalized),
                "EXAMPLE" => TryExample(document.RootElement, out normalized),
                "VOCABULARY" => TryVocabulary(document.RootElement, out normalized),
                "QUESTION" => TryQuestion(document.RootElement, out normalized),
                "VIDEO" => TryVideo(document.RootElement, out normalized),
                "AUDIO" => TryAudio(document.RootElement, out normalized),
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

    public static string Video(string title) => JsonSerializer.Serialize(
        new VideoPayload(title.Trim(), true),
        JsonOptions);

    public static string Audio() => JsonSerializer.Serialize(
        new AudioPayload(1, false, true),
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

    private static bool TryRule(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "heading", "body", "accentColor")
            || !TryString(root, "heading", 160, true, out var heading)
            || !TryString(root, "body", 4000, true, out var body)
            || !TryColor(root, "accentColor", out var accentColor))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new RulePayload(heading, body, accentColor),
            JsonOptions);
        return true;
    }

    private static bool TryExample(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "example", "explanation", "accentColor")
            || !TryString(root, "example", 2000, true, out var example)
            || !TryString(root, "explanation", 2000, false, out var explanation)
            || !TryColor(root, "accentColor", out var accentColor))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new ExamplePayload(example, explanation, accentColor),
            JsonOptions);
        return true;
    }

    private static bool TryVocabulary(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "term", "definition", "example")
            || !TryString(root, "term", 200, true, out var term)
            || !TryString(root, "definition", 1000, true, out var definition)
            || !TryString(root, "example", 2000, false, out var example))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new VocabularyPayload(term, definition, example),
            JsonOptions);
        return true;
    }

    private static bool TryQuestion(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "prompt", "discussionNote")
            || !TryString(root, "prompt", 4000, true, out var prompt)
            || !TryString(root, "discussionNote", 4000, false, out var discussionNote))
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new QuestionPayload(prompt, discussionNote),
            JsonOptions);
        return true;
    }

    private static bool TryVideo(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "title", "showControls")
            || !TryString(root, "title", 200, false, out var title)
            || !TryBoolean(root, "showControls", out var showControls)
            || !showControls)
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(new VideoPayload(title, true), JsonOptions);
        return true;
    }

    private static bool TryAudio(JsonElement root, out string normalized)
    {
        normalized = string.Empty;
        if (!HasOnly(root, "schemaVersion", "autoplay", "showControls")
            || !TryInt(root, "schemaVersion", 1, 1, out var schemaVersion)
            || !TryBoolean(root, "autoplay", out var autoplay)
            || !TryBoolean(root, "showControls", out var showControls)
            || autoplay
            || !showControls)
        {
            return false;
        }

        normalized = JsonSerializer.Serialize(
            new AudioPayload(schemaVersion, false, true),
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

    private static bool TryBoolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var property)
            || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
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

    private sealed record RulePayload(string Heading, string Body, string AccentColor);

    private sealed record ExamplePayload(string Example, string Explanation, string AccentColor);

    private sealed record VocabularyPayload(string Term, string Definition, string Example);

    private sealed record QuestionPayload(string Prompt, string DiscussionNote);

    private sealed record VideoPayload(string Title, bool ShowControls);

    private sealed record AudioPayload(int SchemaVersion, bool Autoplay, bool ShowControls);
}
