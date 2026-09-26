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
using Plus5.Api.Readiness;
using Plus5.Api.Students;
using Plus5.Api.Groups;
using Plus5.Api.Scheduling;
using Plus5.Application.Groups;
using Plus5.Application.Scheduling;
using Plus5.Infrastructure.Groups;
using Plus5.Application.Identity;
using Plus5.Application.Readiness;
using Plus5.Application.Students;
using Plus5.Domain.Identity;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Identity;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Readiness;
using Plus5.Infrastructure.Scheduling;
using Plus5.Infrastructure.Students;

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
        using var anonymousStudents = await client.GetAsync("/api/v1/students", CancellationToken.None);
        using var anonymousDossier = await client.GetAsync($"/api/v1/students/{Guid.NewGuid()}", CancellationToken.None);
        using var anonymousReadiness = await client.GetAsync($"/api/v1/students/{Guid.NewGuid()}/readiness", CancellationToken.None);
        using var anonymousEdit = await client.GetAsync($"/api/v1/students/{Guid.NewGuid()}/edit", CancellationToken.None);
        using var anonymousGroups = await client.GetAsync("/api/v1/groups", CancellationToken.None);
        using var anonymousGroupEdit = await client.GetAsync($"/api/v1/groups/{Guid.NewGuid()}/edit", CancellationToken.None);
        using var anonymousSchedule = await client.GetAsync("/api/v1/schedule?from=2026-09-14&to=2026-09-21", CancellationToken.None);
        using var anonymousSessionDetail = await client.GetAsync($"/api/v1/schedule/{Guid.NewGuid()}", CancellationToken.None);
        using var anonymousSessionEdit = await client.GetAsync($"/api/v1/schedule/{Guid.NewGuid()}/edit", CancellationToken.None);
        using var anonymousSessionCreate = await client.PostAsJsonAsync("/api/v1/schedule", new { }, CancellationToken.None);
        using var missingCsrf = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = Email, password = Password },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousStudents.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousDossier.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousReadiness.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousGroups.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousGroupEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousSchedule.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousSessionDetail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousSessionEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousSessionCreate.StatusCode);
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
        var grade = new SchoolGrade(Guid.NewGuid(), "7R", "Sedmi razred", 7);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Plus5DbContext>();
            dbContext.SchoolGrades.Add(grade);
            await dbContext.SaveChangesAsync();
        }
        csrf = await GetCsrfAsync(client, authCookie);
        using var session = await GetWithCookiesAsync(client, "/api/v1/auth/session", authCookie, csrf.Cookie);
        using var students = await GetWithCookiesAsync(client, "/api/v1/students?pageSize=100", authCookie, csrf.Cookie);
        using var invalidStudents = await GetWithCookiesAsync(client, "/api/v1/students?pageSize=101", authCookie, csrf.Cookie);
        using var createOptions = await GetWithCookiesAsync(
            client, "/api/v1/students/create-options", authCookie, csrf.Cookie);
        using var calendar = await GetWithCookiesAsync(
            client, "/api/v1/schedule?from=2026-09-14&to=2026-09-21", authCookie, csrf.Cookie);
        using var invalidCalendar = await GetWithCookiesAsync(
            client, "/api/v1/schedule?from=2026-09-01&to=2026-10-03", authCookie, csrf.Cookie);
        using var missingSessionDetail = await GetWithCookiesAsync(
            client, $"/api/v1/schedule/{Guid.NewGuid()}", authCookie, csrf.Cookie);
        using var createSessionWithoutCsrf = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/schedule")
        {
            Content = JsonContent.Create(new
            {
                deliveryMode = 1,
                contextId = Guid.NewGuid(),
                date = new DateOnly(2026, 9, 14),
                startsAt = new TimeOnly(16, 0),
                endsAt = new TimeOnly(17, 0),
            }),
            Headers = { { "Cookie", authCookie } },
        });
        using var createWithoutCsrf = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/students")
        {
            Content = JsonContent.Create(new
            {
                firstName = "Ana",
                lastName = "Anić",
                schoolGradeId = grade.Id,
                status = "active",
            }),
            Headers = { { "Cookie", authCookie } },
        });
        using var createStudent = await PostAsync(client, "/api/v1/students", new
        {
            firstName = "Ana",
            lastName = "Anić",
            schoolGradeId = grade.Id,
            status = "active",
        }, csrf, authCookie);
        var createdStudent = await createStudent.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        using var createSession = await PostAsync(client, "/api/v1/schedule", new
        {
            deliveryMode = 1,
            contextId = createdStudent.GetProperty("id").GetGuid(),
            title = "API journey",
            date = new DateOnly(2099, 9, 14),
            startsAt = new TimeOnly(16, 0),
            endsAt = new TimeOnly(17, 0),
            repeatWeekly = false,
        }, csrf, authCookie);
        var createdSession = await createSession.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var createdSessionId = createdSession.GetProperty("id").GetGuid();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Plus5DbContext>();
            var storedSession = await db.Sessions.SingleAsync(item => item.Id == createdSessionId);
            db.Entry(storedSession).Property("RowVersion").CurrentValue = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            await db.SaveChangesAsync();
        }
        using var createdSessionDetail = await GetWithCookiesAsync(
            client,
            $"/api/v1/schedule/{createdSessionId}",
            authCookie,
            csrf.Cookie);
        using var sessionEdit = await GetWithCookiesAsync(
            client, $"/api/v1/schedule/{createdSessionId}/edit", authCookie, csrf.Cookie);
        var sessionEditModel = await sessionEdit.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var sessionEditBody = new
        {
            title = "Updated API journey",
            notes = "Changed safely.",
            date = new DateOnly(2099, 9, 14),
            startsAt = new TimeOnly(17, 0),
            endsAt = new TimeOnly(18, 0),
            locationId = (Guid?)null,
            onlineMeetingUrl = (string?)null,
            scope = 1,
            rowVersion = sessionEditModel.GetProperty("rowVersion").GetString(),
        };
        using var previewSessionEdit = await PostAsync(
            client, $"/api/v1/schedule/{createdSessionId}/conflicts", sessionEditBody, csrf, authCookie);
        using var updateSession = await PutAsync(
            client, $"/api/v1/schedule/{createdSessionId}", sessionEditBody, csrf, authCookie);
        using var refreshedSessionEdit = await GetWithCookiesAsync(
            client, $"/api/v1/schedule/{createdSessionId}/edit", authCookie, csrf.Cookie);
        var refreshedSessionEditModel = await refreshedSessionEdit.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        using var cancelSession = await PostAsync(client, $"/api/v1/schedule/{createdSessionId}/cancel",
            new { rowVersion = refreshedSessionEditModel.GetProperty("rowVersion").GetString() }, csrf, authCookie);
        using var invalidGroupRecurrence = await PostAsync(client, "/api/v1/schedule", new
        {
            deliveryMode = 2,
            contextId = Guid.NewGuid(),
            date = new DateOnly(2099, 9, 14),
            startsAt = new TimeOnly(16, 0),
            endsAt = new TimeOnly(17, 0),
            repeatWeekly = true,
        }, csrf, authCookie);
        using var dossier = await GetWithCookiesAsync(
            client,
            $"/api/v1/students/{createdStudent.GetProperty("id").GetGuid()}",
            authCookie,
            csrf.Cookie);
        using var readiness = await GetWithCookiesAsync(
            client,
            $"/api/v1/students/{createdStudent.GetProperty("id").GetGuid()}/readiness",
            authCookie,
            csrf.Cookie);
        using var edit = await GetWithCookiesAsync(
            client,
            $"/api/v1/students/{createdStudent.GetProperty("id").GetGuid()}/edit",
            authCookie,
            csrf.Cookie);
        var editModel = await edit.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        using var update = await PutAsync(client, $"/api/v1/students/{createdStudent.GetProperty("id").GetGuid()}", new
        {
            rowVersion = editModel.GetProperty("rowVersion").GetString(),
            firstName = "Anamarija",
            lastName = "Anić",
            schoolGradeId = grade.Id,
            status = "active",
            guardians = Array.Empty<object>(),
        }, csrf, authCookie);
        using var logout = await PostAsync(client, "/api/v1/auth/logout", new { }, csrf, authCookie);
        using var revoked = await GetWithCookiesAsync(client, "/api/v1/auth/session", authCookie, csrf.Cookie);

        var authCookieHeader = login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("plus5-auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        Assert.True(
            students.StatusCode == HttpStatusCode.OK,
            await students.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.BadRequest, invalidStudents.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createOptions.StatusCode);
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCalendar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingSessionDetail.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, createSessionWithoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, createWithoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createStudent.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSession.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createdSessionDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sessionEdit.StatusCode);
        Assert.True(previewSessionEdit.StatusCode == HttpStatusCode.OK,
            await previewSessionEdit.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.True(updateSession.StatusCode == HttpStatusCode.OK,
            await updateSession.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.OK, refreshedSessionEdit.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, cancelSession.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidGroupRecurrence.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dossier.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
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
    public async Task GroupsRequireValidatedPagingOwnershipAndCsrf()
    {
        await using var app = await StartApplicationAsync();
        using var client = app.GetTestClient();
        var sender = app.Services.GetRequiredService<CapturingEmailSender>();
        var csrf = await GetCsrfAsync(client);
        using var register = await PostAsync(client, "/api/v1/auth/register", new { email = Email, password = Password }, csrf);
        using var verify = await PostAsync(client, "/api/v1/auth/verify-email", new { email = Email, token = sender.VerificationToken }, csrf);
        using var login = await PostAsync(client, "/api/v1/auth/login", new { email = Email, password = Password }, csrf);
        var cookie = ReadCookie(login, "plus5-auth");
        csrf = await GetCsrfAsync(client, cookie);
        foreach (var query in new[] { "page=0", "pageSize=101", "status=4", "programId=00000000-0000-0000-0000-000000000000", "search=" + new string('x', 101) })
        {
            using var invalid = await GetWithCookiesAsync(client, "/api/v1/groups?" + query, cookie, csrf.Cookie);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var groups = await GetWithCookiesAsync(client, "/api/v1/groups", cookie, csrf.Cookie);
        Assert.Equal(HttpStatusCode.OK, groups.StatusCode);
        var candidatePath = $"/api/v1/groups/create-candidates?programId={Guid.NewGuid()}&schoolGradeId={Guid.NewGuid()}";
        using var anonymousCandidates = await client.GetAsync(candidatePath, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousCandidates.StatusCode);
        using var missingCandidates = await GetWithCookiesAsync(client, candidatePath, cookie, csrf.Cookie);
        Assert.Equal(HttpStatusCode.NotFound, missingCandidates.StatusCode);
        foreach (var invalidQuery in new[] { "page=0", "pageSize=101", "search=" + new string('x', 101) })
        {
            using var invalidCandidates = await GetWithCookiesAsync(client, candidatePath + "&" + invalidQuery, cookie, csrf.Cookie);
            Assert.Equal(HttpStatusCode.BadRequest, invalidCandidates.StatusCode);
        }
        using var missing = await GetWithCookiesAsync(client, $"/api/v1/groups/{Guid.NewGuid()}", cookie, csrf.Cookie);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var path = $"/api/v1/groups/{Guid.NewGuid()}/members/{Guid.NewGuid()}";
        using var missingCsrf = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { join = true, groupRowVersion = "AQ==", studentRowVersion = "Ag==" }),
            Headers = { { "Cookie", cookie } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var invalidVersion = await PostAsync(client, path, new { join = true, groupRowVersion = "invalid", studentRowVersion = "" }, csrf, cookie);
        Assert.Equal(HttpStatusCode.BadRequest, invalidVersion.StatusCode);
        using var foreignWrite = await PostAsync(client, path, new { join = true, groupRowVersion = "AQ==", studentRowVersion = "Ag==" }, csrf, cookie);
        Assert.Equal(HttpStatusCode.NotFound, foreignWrite.StatusCode);
        using var createWithoutCsrf = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/groups")
        {
            Content = JsonContent.Create(new { name = "Test create", programId = Guid.NewGuid(), schoolGradeId = Guid.NewGuid(), capacity = 6 }),
            Headers = { { "Cookie", cookie } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, createWithoutCsrf.StatusCode);
        Guid createProgram;
        Guid createGrade;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Plus5DbContext>();
            var owner = await db.UserAccounts.SingleAsync();
            var grade = new Plus5.Domain.Teaching.SchoolGrade(Guid.NewGuid(), "T7", "Test grade", 7);
            var program = new Plus5.Domain.Teaching.Program(Guid.NewGuid(), owner.Id, "Test program", DateTimeOffset.UtcNow);
            db.AddRange(grade, program);
            await db.SaveChangesAsync();
            createProgram = program.Id;
            createGrade = grade.Id;
        }
        using var created = await PostAsync(client, "/api/v1/groups", new { name = "Empty active", programId = createProgram, schoolGradeId = createGrade, capacity = 6 }, csrf, cookie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdGroup = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var createdGroupId = createdGroup.GetProperty("id").GetGuid();
        using var editGroup = await GetWithCookiesAsync(client, $"/api/v1/groups/{createdGroupId}/edit", cookie, csrf.Cookie);
        Assert.Equal(HttpStatusCode.OK, editGroup.StatusCode);
        using var editMissingCsrf = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/v1/groups/{createdGroupId}")
        {
            Content = JsonContent.Create(new { name = "Changed", programId = createProgram, schoolGradeId = createGrade, status = 1, capacity = 6, rowVersion = "AAAAAAAAAAA=", slots = Array.Empty<object>() }),
            Headers = { { "Cookie", cookie } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, editMissingCsrf.StatusCode);
        using var invalidEditVersion = await PutAsync(client, $"/api/v1/groups/{createdGroupId}", new { name = "Changed", programId = createProgram, schoolGradeId = createGrade, status = 1, capacity = 6, rowVersion = "invalid", slots = Array.Empty<object>() }, csrf, cookie);
        Assert.Equal(HttpStatusCode.BadRequest, invalidEditVersion.StatusCode);
        using var duplicate = await PostAsync(client, "/api/v1/groups", new { name = "Empty active", programId = createProgram, schoolGradeId = createGrade, capacity = 6 }, csrf, cookie);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var badMembers = await PostAsync(client, "/api/v1/groups", new { name = "Bad", programId = createProgram, schoolGradeId = createGrade, capacity = 6, members = new[] { new { studentId = Guid.NewGuid(), rowVersion = "not-base64" } } }, csrf, cookie);
        Assert.Equal(HttpStatusCode.BadRequest, badMembers.StatusCode);
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
        builder.Services.AddScoped<IStudentListQuery, EfStudentListQuery>();
        builder.Services.AddScoped<IStudentCreationService, EfStudentCreationService>();
        builder.Services.AddScoped<IStudentDossierQuery, EfStudentDossierQuery>();
        builder.Services.AddScoped<IStudentReadinessQuery, EfStudentReadinessQuery>();
        builder.Services.AddScoped<IStudentEditingService, EfStudentEditingService>();
        builder.Services.AddScoped<IGroupQuery, EfGroupQuery>();
        builder.Services.AddScoped<IGroupCreationQuery, EfGroupCreationQuery>();
        builder.Services.AddScoped<IGroupCreationService, EfGroupCreationService>();
        builder.Services.AddScoped<IGroupEditingQuery, EfGroupEditingQuery>();
        builder.Services.AddScoped<IGroupEditingService, EfGroupEditingService>();
        builder.Services.AddScoped<IGroupMembershipService, EfGroupMembershipService>();
        builder.Services.AddScoped<IScheduleCalendarQuery, EfScheduleCalendarQuery>();
        builder.Services.AddScoped<IScheduleSessionDetailQuery, EfScheduleSessionDetailQuery>();
        builder.Services.AddScoped<IScheduleCreationService, EfScheduleCreationService>();
        builder.Services.AddScoped<IScheduleEditingQuery, EfScheduleEditingQuery>();
        builder.Services.AddScoped<IScheduleEditingService, EfScheduleEditingService>();
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
        app.MapStudentList();
        app.MapStudentCreation();
        app.MapStudentDossier();
        app.MapStudentReadiness();
        app.MapStudentEditing();
        app.MapGroups();
        app.MapScheduleCalendar();
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

    private static async Task<HttpResponseMessage> PutAsync(
        HttpClient client,
        string path,
        object body,
        CsrfState csrf,
        params string[] cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) };
        request.Headers.Add(IdentityServiceExtensions.CsrfHeaderName, csrf.Token);
        AddCookies(request, [csrf.Cookie, .. cookies]);
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
