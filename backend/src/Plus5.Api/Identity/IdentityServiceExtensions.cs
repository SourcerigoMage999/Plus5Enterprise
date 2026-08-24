using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Plus5.Application.Identity;

namespace Plus5.Api.Identity;

public static class IdentityServiceExtensions
{
    public const string CookieScheme = "Plus5TeacherCookie";
    public const string TeacherPolicy = "Teacher";
    public const string AuthRateLimitPolicy = "AuthSensitive";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    public static IServiceCollection AddTeacherIdentity(
        this IServiceCollection services,
        bool isDevelopment,
        string frontendOrigin)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCors(options => options.AddPolicy(
            "Frontend",
            policy => policy
                .WithOrigins(frontendOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        services.AddAuthentication(CookieScheme)
            .AddCookie(CookieScheme, options =>
            {
                options.Cookie.Name = isDevelopment ? "plus5-auth" : "__Host-plus5-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Path = "/";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = ValidateSessionAsync;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(CookieScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(TeacherPolicy, policy => policy
                .AddAuthenticationSchemes(CookieScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(IdentityClaims.AccountType, IdentityClaims.TeacherAccountType));

        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = isDevelopment ? "plus5-csrf" : "__Host-plus5-csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.HeaderName = CsrfHeaderName;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    }));
        });

        return services;
    }

    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null
            || !IdentityClaims.TryRead(context.Principal, out var accountId, out var sessionId))
        {
            context.RejectPrincipal();
            return;
        }

        var service = context.HttpContext.RequestServices
            .GetRequiredService<ITeacherAuthenticationService>();
        var session = await service.GetCurrentSessionAsync(
            accountId,
            sessionId,
            context.HttpContext.RequestAborted);

        if (session is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieScheme);
        }
    }

    internal static ClaimsPrincipal CreatePrincipal(AuthenticationResult result)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserAccountId!.Value.ToString("D", CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Email, result.Email!),
            new Claim(IdentityClaims.SessionId, result.SessionId!.Value.ToString("D", CultureInfo.InvariantCulture)),
            new Claim(IdentityClaims.AccountType, IdentityClaims.TeacherAccountType),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieScheme));
    }
}
