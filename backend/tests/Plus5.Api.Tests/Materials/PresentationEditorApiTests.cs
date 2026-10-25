using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plus5.Api.Identity;
using Plus5.Api.Materials;
using Plus5.Application.Materials;
using Plus5.Infrastructure.Materials;

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationEditorApiTests
{
    private static readonly string[] QuickCheckOptions = ["Prvi", "Drugi"];

    [Fact]
    public async Task EditorRequiresTeacherAuthenticationAndCsrfForWrites()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var query = new CapturingQuery(materialId);
        await using var app = await StartApplicationAsync(query);
        using var client = app.GetTestClient();

        using var anonymous = await client.GetAsync(
            $"/api/v1/materials/{materialId:D}/presentation");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var getRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/materials/{materialId:D}/presentation");
        getRequest.Headers.Add("Cookie", csrf.Cookie);
        using var authorized = await client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        Assert.Equal(teacherId, query.LastTeacherId);

        using var missingCsrf = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation")
        {
            Content = JsonContent.Create(new
            {
                materialVersionId = Guid.NewGuid(),
                expectedMaterialRowVersion = "AAAAAAAAAAA=",
            }),
        };
        missingCsrf.Headers.Add("Cookie", csrf.Cookie);
        using var rejected = await client.SendAsync(missingCsrf);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        using var valid = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation")
        {
            Content = JsonContent.Create(new
            {
                materialVersionId = Guid.NewGuid(),
                expectedMaterialRowVersion = "AAAAAAAAAAA=",
            }),
        };
        valid.Headers.Add("Cookie", csrf.Cookie);
        valid.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        using var accepted = await client.SendAsync(valid);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task EditorWritesHaveADedicatedTeacherScopedLimit()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        await using var app = await StartApplicationAsync(new CapturingQuery(materialId));
        using var client = app.GetTestClient();
        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 121; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/materials/{materialId:D}/presentation")
            {
                Content = JsonContent.Create(new
                {
                    materialVersionId = Guid.NewGuid(),
                    expectedMaterialRowVersion = "AAAAAAAAAAA=",
                }),
            };
            request.Headers.Add("Cookie", csrf.Cookie);
            request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
            using var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(120), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[120]);

        var secondTeacherCsrf = await SignInAndGetCsrfAsync(client, Guid.NewGuid());
        using var secondTeacherRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation")
        {
            Content = JsonContent.Create(new
            {
                materialVersionId = Guid.NewGuid(),
                expectedMaterialRowVersion = "AAAAAAAAAAA=",
            }),
        };
        secondTeacherRequest.Headers.Add("Cookie", secondTeacherCsrf.Cookie);
        secondTeacherRequest.Headers.Add(
            IdentityServiceExtensions.CsrfHeaderName,
            secondTeacherCsrf.Token);
        using var secondTeacherResponse = await client.SendAsync(secondTeacherRequest);
        Assert.Equal(HttpStatusCode.OK, secondTeacherResponse.StatusCode);
    }

    [Fact]
    public async Task ElementWritesRequireTeacherAndCsrfWithoutExposingAssets()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var slideId = Guid.NewGuid();
        await using var app = await StartApplicationAsync(new CapturingQuery(materialId));
        using var client = app.GetTestClient();

        using var anonymousAsset = await client.GetAsync(
            $"/api/v1/materials/{materialId:D}/presentation/assets/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousAsset.StatusCode);

        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var missingCsrf = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation/slides/{slideId:D}/elements")
        {
            Content = JsonContent.Create(new
            {
                expectedDocumentRowVersion = "AAAAAAAAAAA=",
                elementTypeCode = "TEXT",
                x = 0,
                y = 0,
                width = 400,
                height = 100,
                payloadJson = "{}",
            }),
        };
        missingCsrf.Headers.Add("Cookie", csrf.Cookie);
        using var rejected = await client.SendAsync(missingCsrf);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        using var valid = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation/slides/{slideId:D}/elements")
        {
            Content = JsonContent.Create(new
            {
                expectedDocumentRowVersion = "AAAAAAAAAAA=",
                elementTypeCode = "TEXT",
                x = 0,
                y = 0,
                width = 400,
                height = 100,
                payloadJson = "{}",
            }),
        };
        valid.Headers.Add("Cookie", csrf.Cookie);
        valid.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        using var accepted = await client.SendAsync(valid);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task QuickCheckWriteUsesTheExistingTeacherAndCsrfBoundary()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var slideId = Guid.NewGuid();
        await using var app = await StartApplicationAsync(new CapturingQuery(materialId));
        using var client = app.GetTestClient();
        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation/slides/{slideId:D}/quick-checks")
        {
            Content = JsonContent.Create(new
            {
                expectedDocumentRowVersion = "AAAAAAAAAAA=",
                zIndex = 0,
                x = 100,
                y = 100,
                width = 700,
                height = 350,
                prompt = "Odaberite točan odgovor.",
                options = QuickCheckOptions,
                correctOptionIndex = 1,
                difficulty = 2,
                evidenceType = 1,
                knowledgeComponentIds = Array.Empty<Guid>(),
            }),
        };
        request.Headers.Add("Cookie", csrf.Cookie);
        request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task KnowledgeComponentSearchRequiresTeacherAndPreservesOwnerScope()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        var query = new CapturingQuery(materialId)
        {
            KnowledgePage = new PresentationKnowledgeComponentPage(
                [new PresentationKnowledgeComponentView(
                    componentId,
                    "Present Continuous",
                    "Grammar",
                    "ENG-7",
                    "2026",
                    "Published",
                    "Active")],
                1,
                20,
                1),
        };
        await using var app = await StartApplicationAsync(query);
        using var client = app.GetTestClient();
        var path = $"/api/v1/materials/{materialId:D}/presentation/knowledge-components?search=Present&page=1&pageSize=20";

        using var anonymous = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", csrf.Cookie);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PresentationKnowledgeComponentPage>();
        Assert.Equal(componentId, Assert.Single(body!.Items).Id);
        Assert.Equal(teacherId, query.LastTeacherId);
        Assert.Equal("Present", query.LastSearch);

        using var invalid = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/materials/{materialId:D}/presentation/knowledge-components?pageSize=51");
        invalid.Headers.Add("Cookie", csrf.Cookie);
        using var invalidResponse = await client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task ReviewAndPublishUseTheTeacherCsrfBoundary()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var document = new PresentationDocumentView(
            1,
            1600,
            900,
            "CAcGBQQDAgE=",
            new DateTimeOffset(2026, 10, 23, 8, 0, 0, TimeSpan.Zero),
            [new PresentationSlideView(Guid.NewGuid(), 0, [])]);
        var query = new CapturingQuery(materialId)
        {
            Review = new PresentationReviewWorkspace(
                materialId,
                versionId,
                2,
                "AQIDBAUGBwg=",
                "Present Continuous",
                null,
                "Engleski jezik",
                "hr",
                "Private",
                null,
                "7. razred",
                "A2",
                "Razlikovati vremena.",
                ["gramatika"],
                [],
                [],
                0,
                document,
                []),
        };
        var publisher = new CapturingPublishService();
        await using var app = await StartApplicationAsync(query, publisher);
        using var client = app.GetTestClient();

        using var anonymous = await client.GetAsync(
            $"/api/v1/materials/{materialId:D}/presentation/review");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var reviewRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/materials/{materialId:D}/presentation/review");
        reviewRequest.Headers.Add("Cookie", csrf.Cookie);
        using var reviewResponse = await client.SendAsync(reviewRequest);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var missingCsrf = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation/publish")
        {
            Content = JsonContent.Create(new PublishPresentationCommand(
                versionId,
                "AQIDBAUGBwg=",
                document.RowVersion)),
        };
        missingCsrf.Headers.Add("Cookie", csrf.Cookie);
        using var rejected = await client.SendAsync(missingCsrf);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/materials/{materialId:D}/presentation/publish")
        {
            Content = JsonContent.Create(new PublishPresentationCommand(
                versionId,
                "AQIDBAUGBwg=",
                document.RowVersion)),
        };
        publishRequest.Headers.Add("Cookie", csrf.Cookie);
        publishRequest.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        using var published = await client.SendAsync(publishRequest);

        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal(teacherId, publisher.TeacherId);
        Assert.Equal(materialId, publisher.MaterialId);
    }

    [Fact]
    public async Task AiGenerationRequiresCsrfAndUsesTheDedicatedTeacherLimit()
    {
        var teacherId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        await using var app = await StartApplicationAsync(new CapturingQuery(materialId));
        using var client = app.GetTestClient();
        var path = $"/api/v1/materials/{materialId:D}/presentation/ai/suggestions";

        using var anonymous = await client.PostAsJsonAsync(path, new
        {
            expectedDocumentRowVersion = "AAAAAAAAAAA=",
            suggestionKind = PresentationAiSuggestionKinds.ElementContent,
            instruction = "Predloži primjer.",
            targetSlideId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var csrf = await SignInAndGetCsrfAsync(client, teacherId);
        using var missingCsrf = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new
            {
                expectedDocumentRowVersion = "AAAAAAAAAAA=",
                suggestionKind = PresentationAiSuggestionKinds.ElementContent,
                instruction = "Predloži primjer.",
                targetSlideId = Guid.NewGuid(),
            }),
        };
        missingCsrf.Headers.Add("Cookie", csrf.Cookie);
        using var rejected = await client.SendAsync(missingCsrf);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 21; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new
                {
                    expectedDocumentRowVersion = "AAAAAAAAAAA=",
                    suggestionKind = PresentationAiSuggestionKinds.ElementContent,
                    instruction = "Predloži primjer.",
                    targetSlideId = Guid.NewGuid(),
                }),
            };
            request.Headers.Add("Cookie", csrf.Cookie);
            request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
            using var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        // The authenticated request rejected by CSRF is still an attempted AI write and
        // intentionally consumes one permit before endpoint filters run.
        Assert.All(statuses.Take(19), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.All(statuses.Skip(19), status => Assert.Equal(HttpStatusCode.TooManyRequests, status));
    }

    private static async Task<WebApplication> StartApplicationAsync(
        CapturingQuery query,
        IPresentationPublishService? publisher = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(PresentationEditorApiTests).Assembly.FullName,
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddProblemDetails();
        builder.Services.AddTeacherIdentity(isDevelopment: true, "http://frontend.test");
        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            IdentityServiceExtensions.CookieScheme,
            options => options.Events.OnValidatePrincipal = _ => Task.CompletedTask);
        builder.Services.AddSingleton<IPresentationEditorQuery>(query);
        builder.Services.AddSingleton<IPresentationSlideService, SuccessfulSlideService>();
        builder.Services.AddSingleton<IPresentationElementService, SuccessfulElementService>();
        builder.Services.AddSingleton<IPresentationPublishService>(
            publisher ?? new CapturingPublishService());
        builder.Services.AddSingleton<IPresentationAiSuggestionService, SuccessfulAiService>();
        builder.Services.AddOptions<PresentationAiOptions>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapGet("/test/sign-in/{teacherId:guid}", async (
            Guid teacherId,
            HttpContext context) =>
        {
            await context.SignInAsync(
                IdentityServiceExtensions.CookieScheme,
                CreateTeacherPrincipal(teacherId));
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }).AllowAnonymous();
        app.MapGet("/test/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();
        app.MapPresentationEditor();
        await app.StartAsync();
        return app;
    }

    private static ClaimsPrincipal CreateTeacherPrincipal(Guid teacherId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, teacherId.ToString("D")),
            new Claim(ClaimTypes.Email, "teacher@plus5.local"),
            new Claim(IdentityClaims.SessionId, Guid.NewGuid().ToString("D")),
            new Claim(IdentityClaims.AccountType, IdentityClaims.TeacherAccountType),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<CsrfState> SignInAndGetCsrfAsync(
        HttpClient client,
        Guid teacherId)
    {
        using var signInResponse = await client.GetAsync($"/test/sign-in/{teacherId:D}");
        Assert.Equal(HttpStatusCode.NoContent, signInResponse.StatusCode);
        var authenticationCookie = Assert.Single(
                signInResponse.Headers.GetValues("Set-Cookie"))
            .Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/csrf");
        request.Headers.Add("Cookie", authenticationCookie);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CsrfBody>();
        var csrfCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"))
            .Split(';', 2)[0];
        return new CsrfState(body!.Token, authenticationCookie + "; " + csrfCookie);
    }

    private sealed record CsrfBody(string Token);
    private sealed record CsrfState(string Token, string Cookie);

    private sealed class CapturingQuery(Guid materialId) : IPresentationEditorQuery
    {
        public Guid? LastTeacherId { get; private set; }
        public string? LastSearch { get; private set; }
        public PresentationKnowledgeComponentPage? KnowledgePage { get; init; }
        public PresentationReviewWorkspace? Review { get; init; }

        public Task<PresentationEditorWorkspace?> GetAsync(
            Guid teacherAccountId,
            Guid requestedMaterialId,
            CancellationToken cancellationToken)
        {
            LastTeacherId = teacherAccountId;
            return Task.FromResult<PresentationEditorWorkspace?>(requestedMaterialId == materialId
                ? new PresentationEditorWorkspace(
                    materialId,
                    Guid.NewGuid(),
                    1,
                    "Presentation",
                    "AAAAAAAAAAA=",
                    null)
                : null);
        }

        public Task<PresentationKnowledgeComponentPage?> SearchKnowledgeComponentsAsync(
            Guid teacherAccountId,
            Guid requestedMaterialId,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            LastTeacherId = teacherAccountId;
            LastSearch = search;
            return Task.FromResult(requestedMaterialId == materialId
                ? KnowledgePage ?? new PresentationKnowledgeComponentPage([], page, pageSize, 0)
                : null);
        }

        public Task<PresentationReviewWorkspace?> GetReviewAsync(
            Guid teacherAccountId,
            Guid requestedMaterialId,
            CancellationToken cancellationToken)
        {
            LastTeacherId = teacherAccountId;
            return Task.FromResult(requestedMaterialId == materialId ? Review : null);
        }
    }

    private sealed class CapturingPublishService : IPresentationPublishService
    {
        public Guid? TeacherId { get; private set; }
        public Guid? MaterialId { get; private set; }

        public Task<PresentationPublishResult> PublishAsync(
            Guid teacherAccountId,
            Guid materialId,
            PublishPresentationCommand command,
            CancellationToken cancellationToken)
        {
            TeacherId = teacherAccountId;
            MaterialId = materialId;
            return Task.FromResult(new PresentationPublishResult(
                PresentationPublishOutcome.Success));
        }
    }

    private sealed class SuccessfulSlideService : IPresentationSlideService
    {
        public Task<PresentationMutationResult> InitializeAsync(Guid teacherAccountId, Guid materialId, InitializePresentationCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> AddAsync(Guid teacherAccountId, Guid materialId, AddPresentationSlideCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> DeleteAsync(Guid teacherAccountId, Guid materialId, Guid slideId, PresentationConcurrencyCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> DuplicateAsync(Guid teacherAccountId, Guid materialId, Guid slideId, PresentationConcurrencyCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> ReorderAsync(Guid teacherAccountId, Guid materialId, ReorderPresentationSlidesCommand command, CancellationToken cancellationToken) => Success();

        private static Task<PresentationMutationResult> Success() => Task.FromResult(
            new PresentationMutationResult(PresentationMutationOutcome.Success));
    }

    private sealed class SuccessfulElementService : IPresentationElementService
    {
        public Task<PresentationAssetContent?> OpenAssetAsync(Guid teacherAccountId, Guid materialId, Guid assetId, CancellationToken cancellationToken) => Task.FromResult<PresentationAssetContent?>(null);
        public Task<PresentationMutationResult> CreateAsync(Guid teacherAccountId, Guid materialId, Guid slideId, CreatePresentationElementCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> UploadImageAsync(Guid teacherAccountId, Guid materialId, Guid slideId, UploadPresentationImageCommand command, CancellationToken cancellationToken) => Success();

        public Task<PresentationMutationResult> UploadMediaAsync(Guid teacherAccountId, Guid materialId, Guid slideId, UploadPresentationMediaCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> CreateQuickCheckAsync(Guid teacherAccountId, Guid materialId, Guid slideId, UpsertPresentationQuickCheckCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> UpdateQuickCheckAsync(Guid teacherAccountId, Guid materialId, Guid slideId, Guid elementId, UpsertPresentationQuickCheckCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> UpdateAsync(Guid teacherAccountId, Guid materialId, Guid slideId, Guid elementId, UpdatePresentationElementCommand command, CancellationToken cancellationToken) => Success();
        public Task<PresentationMutationResult> DeleteAsync(Guid teacherAccountId, Guid materialId, Guid slideId, Guid elementId, PresentationConcurrencyCommand command, CancellationToken cancellationToken) => Success();

        private static Task<PresentationMutationResult> Success() => Task.FromResult(
            new PresentationMutationResult(PresentationMutationOutcome.Success));
    }

    private sealed class SuccessfulAiService : IPresentationAiSuggestionService
    {
        public Task<PresentationAiSuggestionResult> CreateAsync(
            Guid teacherAccountId,
            Guid materialId,
            CreatePresentationAiSuggestionCommand command,
            CancellationToken cancellationToken) => Task.FromResult(
                new PresentationAiSuggestionResult(
                    PresentationAiSuggestionOutcome.Success,
                    View(command, teacherAccountId, materialId)));

        public Task<PresentationAiSuggestionView?> GetAsync(
            Guid teacherAccountId,
            Guid materialId,
            Guid suggestionId,
            CancellationToken cancellationToken) => Task.FromResult<PresentationAiSuggestionView?>(null);

        public Task<PresentationAiSuggestionResult> ResolveAsync(
            Guid teacherAccountId,
            Guid materialId,
            Guid suggestionId,
            ResolvePresentationAiSuggestionCommand command,
            CancellationToken cancellationToken) => Task.FromResult(
                new PresentationAiSuggestionResult(PresentationAiSuggestionOutcome.Success));

        private static PresentationAiSuggestionView View(
            CreatePresentationAiSuggestionCommand command,
            Guid teacherAccountId,
            Guid materialId)
        {
            var generatedAt = new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero);
            return new PresentationAiSuggestionView(
                Guid.NewGuid(),
                command.SuggestionKind,
                "Pending",
                "{\"elementTypeCode\":\"TEXT\",\"content\":\"Primjer\"}",
                "OPENAI",
                PresentationAiOptions.CanonicalModel,
                "presentation-ai-v1",
                generatedAt,
                generatedAt.AddHours(24),
                null,
                command.TargetSlideId,
                command.TargetElementId);
        }
    }
}
