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

namespace Plus5.Api.Tests.Materials;

public sealed class PresentationEditorApiTests
{
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

    private static async Task<WebApplication> StartApplicationAsync(CapturingQuery query)
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
}
