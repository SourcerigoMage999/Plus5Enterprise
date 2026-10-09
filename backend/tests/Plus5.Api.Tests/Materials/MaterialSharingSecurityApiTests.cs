using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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

public sealed class MaterialSharingSecurityApiTests
{
    [Fact]
    public async Task RecipientCategoriesHaveAnIndistinguishableApiResponse()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var csrf = await GetCsrfAsync(client);
        var responses = new List<(HttpStatusCode Status, string Code)>();

        foreach (var email in new[]
                 {
                     "unknown@plus5.local",
                     "deactivated@plus5.local",
                     "owner@plus5.local",
                 })
        {
            using var response = await PutAsync(client, csrf, email);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            responses.Add((response.StatusCode, body.GetProperty("code").GetString()!));
        }

        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.Status);
            Assert.Equal("material_share_invalid_recipient", response.Code);
        });
        Assert.Single(responses.Distinct());
    }

    [Fact]
    public async Task SharingWritesHaveADedicatedFixedWindowLimit()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var csrf = await GetCsrfAsync(client);
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            using var response = await PutAsync(
                client,
                csrf,
                $"unknown{attempt}@plus5.local");
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    private static async Task<WebApplication> StartApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(MaterialSharingSecurityApiTests).Assembly.FullName,
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddProblemDetails();
        builder.Services.AddTeacherIdentity(isDevelopment: true, "http://frontend.test");
        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            IdentityServiceExtensions.CookieScheme,
            options => options.Events.OnValidatePrincipal = _ => Task.CompletedTask);
        builder.Services.AddSingleton<IMaterialSharingQuery, EmptySharingQuery>();
        builder.Services.AddSingleton<IMaterialSharingService, InvalidRecipientSharingService>();

        var app = builder.Build();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapGet("/test/sign-in", async (HttpContext context) =>
        {
            await context.SignInAsync(
                IdentityServiceExtensions.CookieScheme,
                CreateTeacherPrincipal());
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }).AllowAnonymous();
        app.MapGet("/test/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();
        app.MapMaterialSharing();
        await app.StartAsync();
        return app;
    }

    private static ClaimsPrincipal CreateTeacherPrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
            new Claim(ClaimTypes.Email, "owner@plus5.local"),
            new Claim(IdentityClaims.SessionId, Guid.NewGuid().ToString("D")),
            new Claim(IdentityClaims.AccountType, IdentityClaims.TeacherAccountType),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<CsrfState> GetCsrfAsync(HttpClient client)
    {
        using var signInResponse = await client.GetAsync("/test/sign-in");
        Assert.Equal(HttpStatusCode.NoContent, signInResponse.StatusCode);
        var authenticationCookie = Assert.Single(signInResponse.Headers.GetValues("Set-Cookie"))
            .Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/csrf");
        request.Headers.Add("Cookie", authenticationCookie);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var cookie = authenticationCookie + "; " + setCookie.Split(';', 2)[0];
        return new CsrfState(body.GetProperty("token").GetString()!, cookie);
    }

    private static async Task<HttpResponseMessage> PutAsync(
        HttpClient client,
        CsrfState csrf,
        string recipientEmail)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/materials/{Guid.NewGuid():D}/sharing")
        {
            Content = JsonContent.Create(new
            {
                expectedRowVersion = "AAAAAAAAAAA=",
                visibility = "shared",
                grants = new[] { new { recipientEmail, access = "view" } },
            }),
        };
        request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        request.Headers.Add("Cookie", csrf.Cookie);
        return await client.SendAsync(request);
    }

    private sealed record CsrfState(string Token, string Cookie);

    private sealed class EmptySharingQuery : IMaterialSharingQuery
    {
        public Task<MaterialSharingWorkspace?> GetAsync(
            Guid teacherAccountId,
            Guid materialId,
            CancellationToken cancellationToken) => Task.FromResult<MaterialSharingWorkspace?>(null);
    }

    private sealed class InvalidRecipientSharingService : IMaterialSharingService
    {
        public Task<MaterialSharingResult> SaveAsync(
            Guid teacherAccountId,
            Guid materialId,
            MaterialSharingCommand command,
            CancellationToken cancellationToken) => Task.FromResult(
                new MaterialSharingResult(MaterialSharingOutcome.InvalidRecipient));
    }
}
