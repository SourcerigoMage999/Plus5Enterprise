namespace Plus5.Api.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string PublicOrigin { get; init; } = string.Empty;
}
