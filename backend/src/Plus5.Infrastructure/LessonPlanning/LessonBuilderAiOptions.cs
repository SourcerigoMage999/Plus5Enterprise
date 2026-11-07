namespace Plus5.Infrastructure.LessonPlanning;

public sealed class LessonBuilderAiOptions
{
    public const string SectionName = "LessonBuilderAi";
    public const string RequiredBaseUrl = "https://eu.api.openai.com/v1/";
    public const string CanonicalModel = "gpt-6.1-sol";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = RequiredBaseUrl;
    public string Model { get; set; } = CanonicalModel;
    public string ApiKey { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = "lesson-activity-regeneration-v1";
    public int TimeoutSeconds { get; set; } = 30;
}
