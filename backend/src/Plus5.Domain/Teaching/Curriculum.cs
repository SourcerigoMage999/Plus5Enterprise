namespace Plus5.Domain.Teaching;

public sealed class Curriculum
{
    public const int CodeMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int VersionMaxLength = 64;

    private Curriculum()
    {
    }

    public Curriculum(Guid id, string code, string name, string version)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(id));
        }

        Id = id;
        Code = NormalizeIdentifier(code, CodeMaxLength, nameof(code));
        Name = NormalizeRequiredText(name, NameMaxLength, nameof(name));
        Version = NormalizeIdentifier(version, VersionMaxLength, nameof(version));
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    private static string NormalizeIdentifier(string value, int maxLength, string parameterName) =>
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
