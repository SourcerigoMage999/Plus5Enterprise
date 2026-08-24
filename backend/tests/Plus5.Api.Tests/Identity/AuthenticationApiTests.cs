using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Identity;
using Plus5.Domain.Identity;
using Plus5.Infrastructure.Identity;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Identity;

public sealed class AuthenticationApiTests
{
    private const string Email = "teacher@example.test";
    private const string Password = "StrongPassword42!";

    [Fact]
    public async Task ProtectedApiIsAnonymousByDefaultAndInvalidCsrfIsRejected()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();

        using var anonymous = await client.GetAsync("/api/v1/auth/session", CancellationToken.None);
        using var missingCsrf = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = Email, password = Password },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }

    [Fact]
    public async Task TeacherJourneyIssuesRevocableCookieAndLogoutRemovesAccess()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var sender = app.Services.GetRequiredService<CapturingEmailSender>();
        var csrf = await GetCsrfAsync(client);

        using var register = await PostAsync(client, "/api/v1/auth/register", new { email = Email, password = Password }, csrf);
        using var verify = await PostAsync(client, "/api/v1/auth/verify-email", new { email = Email, token = sender.VerificationToken }, csrf);
        using var login = await PostAsync(client, "/api/v1/auth/login", new { email = Email, password = Password }, csrf);

        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);
        Assert.True(
            verify.StatusCode == HttpStatusCode.NoContent,
            await verify.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        var authCookie = ReadCookie(login, "plus5-auth");
        csrf = await GetCsrfAsync(client, authCookie);
        using var session = await GetWithCookiesAsync(client, "/api/v1/auth/session", authCookie, csrf.Cookie);
        using var logout = await PostAsync(client, "/api/v1/auth/logout", new { }, csrf, authCookie);
        using var revoked = await GetWithCookiesAsync(client, "/api/v1/auth/session", authCookie, csrf.Cookie);

        var authCookieHeader = login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("plus5-auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordDoesNotEnumerateAccounts()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var sender = app.Services.GetRequiredService<CapturingEmailSender>();
        var csrf = await GetCsrfAsync(client);
        await PostAsync(client, "/api/v1/auth/register", new { email = Email, password = Password }, csrf);
        await PostAsync(client, "/api/v1/auth/verify-email", new { email = Email, token = sender.VerificationToken }, csrf);

        using var known = await PostAsync(client, "/api/v1/auth/forgot-password", new { email = Email }, csrf);
        using var unknown = await PostAsync(client, "/api/v1/auth/forgot-password", new { email = "unknown@example.test" }, csrf);

        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PublicAuthSurfaceIsRateLimited()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var csrf = await GetCsrfAsync(client);
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            using var response = await PostAsync(
                client,
                "/api/v1/auth/forgot-password",
                new { email = $"unknown{attempt}@example.test" },
                csrf);
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.Accepted, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    [Fact]
    public void ProductionCookieContractIsHostScopedSecureHttpOnlyAndStrict()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTeacherIdentity(isDevelopment: false, "https://plus5.example.test");
        using var provider = services.BuildServiceProvider();
        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityServiceExtensions.CookieScheme)
            .Cookie;

        Assert.Equal("__Host-plus5-auth", cookie.Name);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
        Assert.Equal("/", cookie.Path);
    }

    private static async Task<WebApplication> StartApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AuthenticationApiTests).Assembly.FullName,
            EnvironmentName = "Development",
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddApiConventions();
        builder.Services.AddTeacherIdentity(isDevelopment: true, "http://frontend.test");
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var databaseName = Guid.NewGuid().ToString("N");
        builder.Services.AddDbContext<Plus5DbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        builder.Services.AddScoped<ITeacherAuthenticationService, TeacherAuthenticationService>();
        builder.Services.AddSingleton<CapturingEmailSender>();
        builder.Services.AddSingleton<IAccountEmailSender>(provider =>
            provider.GetRequiredService<CapturingEmailSender>());
        builder.Services.AddSingleton(TimeProvider.System);

        var app = builder.Build();
        app.UseApiConventions();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapTeacherAuthentication();
        await app.StartAsync(CancellationToken.None);
        return app;
    }

    private static async Task<CsrfState> GetCsrfAsync(HttpClient client, params string[] cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");
        AddCookies(request, cookies);
        using var response = await client.SendAsync(request, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return new CsrfState(
            body.GetProperty("token").GetString()!,
            ReadCookie(response, "plus5-csrf"));
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        object body,
        CsrfState csrf,
        params string[] cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        AddCookies(request, [csrf.Cookie, .. cookies]);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> GetWithCookiesAsync(
        HttpClient client,
        string path,
        params string[] cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddCookies(request, cookies);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static void AddCookies(HttpRequestMessage request, IEnumerable<string> cookies)
    {
        var values = cookies.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (values.Length > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", values));
        }
    }

    private static string ReadCookie(HttpResponseMessage response, string name)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        return header.Split(';', 2)[0];
    }

    private sealed record CsrfState(string Token, string Cookie);

    private sealed class CapturingEmailSender : IAccountEmailSender
    {
        public string? VerificationToken { get; private set; }

        public Task SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken)
        {
            VerificationToken = token;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
