using Microsoft.Extensions.Options;
using Plus5.Api.Configuration;
using Plus5.Domain.LessonPlanning;
using Plus5.Infrastructure.LessonPlanning;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonActivityDetailEditingAiTests
{
    [Fact]
    public void ManualEditAndDuplicatePreserveVersionBoundFields()
    {
        var versionId = Guid.NewGuid();
        var blockVersionId = Guid.NewGuid();
        var activity = new LessonActivity(Guid.NewGuid(), versionId, blockVersionId, 3,
            "Speaking", LessonActivityPurpose.Practice, 20);
        activity.UpdateCore("Speaking drill", LessonActivityPurpose.Practice, 25,
            LessonActivityWorkMode.Pair, "Opis", "Svrha", "Učitelj", "Učenik",
            "Privatna bilješka", true, false, true);

        var copy = activity.Duplicate(Guid.NewGuid(), 4);

        Assert.Equal(versionId, copy.LessonPlanVersionId);
        Assert.Equal(blockVersionId, copy.KnowledgeBlockVersionId);
        Assert.Equal(25, copy.DurationMinutes);
        Assert.Equal(LessonActivityPurpose.Practice, copy.Purpose);
        Assert.Equal("Privatna bilješka", copy.Notes);
        Assert.Equal(LessonActivityWorkMode.Pair, copy.WorkMode);
    }

    [Fact]
    public void ApplyingAiContentChangesOnlyLockedSixFields()
    {
        var blockVersionId = Guid.NewGuid();
        var activity = new LessonActivity(Guid.NewGuid(), Guid.NewGuid(), blockVersionId, 2,
            "Original", LessonActivityPurpose.CheckForUnderstanding, 12);
        activity.UpdateCore("Original", LessonActivityPurpose.CheckForUnderstanding, 12,
            LessonActivityWorkMode.Individual, null, null, null, null, "Ne šalji AI-u",
            true, false, true);

        activity.ApplyAiContent("Prijedlog", "Nova svrha", "Novi opis", "T upute", "U upute",
            LessonActivityWorkMode.Group);

        Assert.Equal(12, activity.DurationMinutes);
        Assert.Equal(2, activity.SortOrder);
        Assert.Equal(blockVersionId, activity.KnowledgeBlockVersionId);
        Assert.Equal(LessonActivityPurpose.CheckForUnderstanding, activity.Purpose);
        Assert.Equal("Ne šalji AI-u", activity.Notes);
        Assert.True(activity.ShowTeacherInstructionsOnBoard);
        Assert.False(activity.ShowStudentInstructionsOnBoard);
        Assert.True(activity.ShowProgressOnBoard);
    }

    [Fact]
    public void AiOutputIsStrictlyBoundedAndRejectsExtraFields()
    {
        const string valid = """
            {"name":"Mini dijalog","purpose":"Primjena","description":"Rad u paru","teacherInstructions":"Podijeli uloge","studentInstructions":"Izvedite dijalog","workMode":"Pair"}
            """;
        Assert.True(EfLessonActivityAiSuggestionService.TryNormalize(valid, out var normalized));
        Assert.Contains("Mini dijalog", normalized, StringComparison.Ordinal);
        Assert.False(EfLessonActivityAiSuggestionService.TryNormalize(
            valid[..^1] + ",\"durationMinutes\":5}", out _));
    }

    [Fact]
    public void ResolvedSuggestionClearsProviderContent()
    {
        var now = DateTimeOffset.UtcNow;
        var suggestion = new LessonActivityAiSuggestion(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new byte[8], "OPENAI", LessonBuilderAiOptions.CanonicalModel,
            "v1", "{\"name\":\"A\"}", now, now.AddHours(24));

        suggestion.Resolve(LessonActivityAiSuggestionStatus.Accepted, now.AddMinutes(1));

        Assert.Equal(LessonActivityAiSuggestionStatus.Accepted, suggestion.Status);
        Assert.Null(suggestion.ContentJson);
        Assert.NotNull(suggestion.ResolvedAtUtc);
    }

    [Fact]
    public async Task DisabledAiNeverCallsProvider()
    {
        var handler = new CountingHandler();
        var assistant = new OpenAiLessonBuilderAiAssistant(new HttpClient(handler)
        {
            BaseAddress = new Uri(LessonBuilderAiOptions.RequiredBaseUrl),
        }, Options.Create(new LessonBuilderAiOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiLessonBuilderAiAssistant>.Instance);

        var result = await assistant.GenerateActivitySuggestionAsync(
            new("{}", EfLessonActivityAiSuggestionService.OutputSchema), CancellationToken.None);

        Assert.Equal(Plus5.Application.LessonPlanning.LessonActivityAiProviderOutcome.Disabled, result.Outcome);
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public void EnabledAiRequiresLockedConfigurationAndApiKey()
    {
        var validator = new LessonBuilderAiOptionsValidator();
        Assert.True(validator.Validate(null, new LessonBuilderAiOptions()).Succeeded);
        Assert.False(validator.Validate(null, new LessonBuilderAiOptions { Enabled = true }).Succeeded);
        Assert.True(validator.Validate(null, new LessonBuilderAiOptions { Enabled = true, ApiKey = "secret" }).Succeeded);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
