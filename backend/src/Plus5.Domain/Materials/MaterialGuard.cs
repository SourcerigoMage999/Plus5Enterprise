using System.Text;

namespace Plus5.Domain.Materials;

internal static class MaterialGuard
{
    public static void Identifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    public static void Utc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }

    public static string RequiredText(string value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim().Normalize(NormalizationForm.FormC);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value is required and may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? OptionalText(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim().Normalize(NormalizationForm.FormC);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string Code(string value, int maxLength, string parameterName)
    {
        var normalized = RequiredText(value, maxLength, parameterName).ToUpperInvariant();
        if (normalized.Any(character =>
            !(character is >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '_'
                or '-'
                or '.')))
        {
            throw new ArgumentException(
                "Value may contain only A-Z, 0-9, underscore, hyphen, or dot.",
                parameterName);
        }

        return normalized;
    }
}
