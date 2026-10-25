using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plus5.Api.Dashboard;
using Plus5.Api.Identity;
using Plus5.Application.Dashboard;

namespace Plus5.Api.Tests.Dashboard;

public sealed class TeacherDashboardApiTests
{
    [Fact]
    public async Task EndpointRequiresTeacherAndUsesAuthenticatedOwner()
    {
        var teacherId = Guid.NewGuid();
        var query = new CapturingDashboardQuery();
        await using var app = await StartApplicationAsync(query);
        using var client = app.GetTestClient();

        using var anonymous = await client.GetAsync("/api/v1/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var signIn = await client.GetAsync($"/test/sign-in/{teacherId:D}");
        Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
        var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie")).Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard");
        request.Headers.Add("Cookie", cookie);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TeacherDashboardEndpoints.DashboardResponse>();
        Assert.NotNull(body);
        Assert.Equal(teacherId, query.LastTeacherAccountId);
        Assert.Equal(new DateOnly(2026, 10, 25), body.Today);
        Assert.Empty(body.TodaySessions);
        Assert.Null(body.NextSession);
        Assert.Equal(2, body.Students.TotalCount);
    }

    private static async Task<WebApplication> StartApplicationAsync(
        CapturingDashboardQuery query)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(TeacherDashboardApiTests).Assembly.FullName,
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddProblemDetails();
        builder.Services.AddTeacherIdentity(isDevelopment: true, "http://frontend.test");
        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            IdentityServiceExtensions.CookieScheme,
            options => options.Events.OnValidatePrincipal = _ => Task.CompletedTask);
        builder.Services.AddSingleton<ITeacherDashboardQuery>(query);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/test/sign-in/{teacherId:guid}", async (
            Guid teacherId,
            HttpContext context) =>
        {
            await context.SignInAsync(
                IdentityServiceExtensions.CookieScheme,
                CreateTeacherPrincipal(teacherId));
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }).AllowAnonymous();
        app.MapTeacherDashboard();
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

    private sealed class CapturingDashboardQuery : ITeacherDashboardQuery
    {
        public Guid? LastTeacherAccountId { get; private set; }

        public Task<TeacherDashboard> GetAsync(
            Guid teacherAccountId,
            CancellationToken cancellationToken)
        {
            LastTeacherAccountId = teacherAccountId;
            return Task.FromResult(new TeacherDashboard(
                "Europe/Zagreb",
                new DateOnly(2026, 10, 25),
                new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero),
                [],
                null,
                new TeacherDashboardStudentOverview(2, 1, 1, 0)));
        }
    }
}
