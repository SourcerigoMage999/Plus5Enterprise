namespace Plus5.Domain.Teaching;

public sealed class ProficiencyLevel
{
    public const int CodeMaxLength = 32;
    public const int FrameworkCodeMaxLength = 32;
    public const int NameMaxLength = 100;

    private ProficiencyLevel()
    {
    }

    public ProficiencyLevel(
        Guid id,
        string frameworkCode,
        string code,
        string name,
        int sortOrder)
    {
        EnsureIdentifier(id);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Id = id;
        FrameworkCode = NormalizeCode(
            frameworkCode,
            FrameworkCodeMaxLength,
            nameof(frameworkCode));
        Code = NormalizeCode(code, CodeMaxLength, nameof(code));
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }

    public string FrameworkCode { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    private static void EnsureIdentifier(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(value));
        }
    }

    private static string NormalizeCode(string value, int maxLength, string parameterName) =>
        NormalizeRequiredText(value, maxLength, parameterName).ToUpperInvariant();

    private static string NormalizeRequiredText(string value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value is required and may contain at most {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
