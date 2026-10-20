using System.Text.Json;

namespace Plus5.Application.Materials;

public static class QuickCheckAuthoringPayloadRegistry
{
    public const int MinimumOptions = 2;
    public const int MaximumOptions = 8;
    public const int OptionTextMaxLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryCreate(
        IReadOnlyList<string>? options,
        int correctOptionIndex,
        out string payloadJson,
        out string correctOptionKey)
    {
        payloadJson = string.Empty;
        correctOptionKey = string.Empty;
        if (options is null
            || options.Count is < MinimumOptions or > MaximumOptions
            || correctOptionIndex < 0
            || correctOptionIndex >= options.Count)
        {
            return false;
        }

        var normalized = options.Select((text, index) => new QuickCheckOptionView(
            ((char)('A' + index)).ToString(),
            text?.Trim() ?? string.Empty)).ToList();
        if (normalized.Any(option => option.Text.Length is 0 or > OptionTextMaxLength))
        {
            return false;
        }

        correctOptionKey = normalized[correctOptionIndex].Key;
        payloadJson = JsonSerializer.Serialize(
            new QuickCheckAuthoringPayload(1, normalized),
            JsonOptions);
        return true;
    }

    public static IReadOnlyList<QuickCheckOptionView> ReadOptions(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<QuickCheckAuthoringPayload>(
                payloadJson,
                JsonOptions);
            return payload is { SchemaVersion: 1 }
                ? payload.Options
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record QuickCheckAuthoringPayload(
        int SchemaVersion,
        IReadOnlyList<QuickCheckOptionView> Options);
}
