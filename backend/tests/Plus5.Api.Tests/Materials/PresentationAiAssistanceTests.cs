using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationAiAssistanceTests
{
    private static readonly DateTimeOffset GeneratedAt =
        new(2026, 10, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SuggestionClearsGeneratedContentWhenTeacherResolvesIt()
    {
        var suggestion = CreateSuggestion();

        suggestion.Resolve(
            PresentationAiSuggestionStatus.Accepted,
            GeneratedAt.AddMinutes(3));

        Assert.Equal(PresentationAiSuggestionStatus.Accepted, suggestion.Status);
        Assert.Null(suggestion.GeneratedContentJson);
        Assert.Equal(GeneratedAt.AddMinutes(3), suggestion.ResolvedAtUtc);
        Assert.Throws<InvalidOperationException>(() => suggestion.Resolve(
            PresentationAiSuggestionStatus.Rejected,
            GeneratedAt.AddMinutes(4)));
    }

    [Fact]
    public void SuggestionExpiresAfterTheBoundedTwentyFourHourWindow()
    {
        var suggestion = CreateSuggestion();

        Assert.False(suggestion.IsExpired(GeneratedAt.AddHours(23)));
        Assert.True(suggestion.IsExpired(GeneratedAt.AddHours(24)));
    }

    [Fact]
    public void ConfigurationIsDisabledWithoutAKeyAndRejectsEnabledMissingKey()
    {
        var validator = new Plus5.Api.Configuration.PresentationAiOptionsValidator();
        var disabled = new PresentationAiOptions();
        var enabled = new PresentationAiOptions { Enabled = true };

        Assert.True(validator.Validate(null, disabled).Succeeded);
        Assert.True(validator.Validate(null, enabled).Failed);
    }

    [Fact]
    public async Task DisabledAdapterDoesNotSendAProviderRequest()
    {
        var handler = new CapturingHandler();
        var assistant = CreateAssistant(handler, enabled: false);

        var result = await assistant.GenerateAsync(Request(), CancellationToken.None);

        Assert.Equal(PresentationAiProviderOutcome.Disabled, result.Outcome);
        Assert.Null(handler.RequestBody);
    }

    [Fact]
    public async Task OpenAiRequestIsStatelessStructuredAndToolFree()
    {
        var handler = new CapturingHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"output":[{"content":[{"type":"output_text","text":"{\"elementTypeCode\":\"TEXT\",\"content\":\"Siguran prijedlog\"}"}]}],"usage":{"input_tokens":12,"output_tokens":8}}
                    """,
                    Encoding.UTF8,
                    "application/json"),
            },
        };
        var assistant = CreateAssistant(handler, enabled: true);

        var result = await assistant.GenerateAsync(Request(), CancellationToken.None);

        Assert.Equal(PresentationAiProviderOutcome.Success, result.Outcome);
        Assert.Contains("\"store\":false", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"gpt-6.1-sol\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"tools\":[]", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"json_schema\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("server-secret", handler.AuthorizationParameter);
    }

    [Theory]
    [InlineData(PresentationAiSuggestionKinds.ElementContent, "{\"elementTypeCode\":\"TEXT\",\"content\":\"Primjer\"}")]
    [InlineData(PresentationAiSuggestionKinds.QuickCheck, "{\"question\":\"Pitanje?\",\"answers\":[\"Da\",\"Ne\"],\"correctAnswerIndex\":0,\"difficulty\":2,\"evidenceType\":1}")]
    [InlineData(PresentationAiSuggestionKinds.KnowledgeMapping, "{\"knowledgeComponentIds\":[\"2f91f6ee-b414-4f24-9ee8-7bccb5e2032a\"]}")]
    public void StructuredSuggestionsAreValidatedBeforePersistence(
        string kind,
        string json)
    {
        Assert.True(EfPresentationAiSuggestionService.TryNormalize(kind, json, out _));
        Assert.False(EfPresentationAiSuggestionService.TryNormalize(
            kind,
            "{\"unexpected\":\"value\"}",
            out _));
    }

    private static PresentationAiSuggestion CreateSuggestion() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        PresentationAiSuggestionKinds.ElementContent,
        "OPENAI",
        PresentationAiOptions.CanonicalModel,
        "presentation-ai-v1",
        "{\"elementTypeCode\":\"TEXT\",\"content\":\"Primjer\"}",
        GeneratedAt,
        GeneratedAt.AddHours(24));

    private static PresentationAiProviderRequest Request() => new(
        PresentationAiSuggestionKinds.ElementContent,
        "Predloži primjer.",
        "{\"material\":{\"title\":\"Present Continuous\"}}",
        EfPresentationAiSuggestionService.SchemaFor(
            PresentationAiSuggestionKinds.ElementContent));

    private static OpenAiPresentationAiAssistant CreateAssistant(
        CapturingHandler handler,
        bool enabled)
    {
        var options = Options.Create(new PresentationAiOptions
        {
            Enabled = enabled,
            ApiKey = enabled ? "server-secret" : string.Empty,
        });
        return new OpenAiPresentationAiAssistant(
            new HttpClient(handler)
            {
                BaseAddress = new Uri(PresentationAiOptions.RequiredBaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            },
            options,
            NullLogger<OpenAiPresentationAiAssistant>.Instance);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; init; } =
            new(HttpStatusCode.InternalServerError);
        public string? RequestBody { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Response;
        }
    }
}
