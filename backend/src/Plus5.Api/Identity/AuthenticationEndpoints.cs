using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Plus5.Api.Conventions;
using Plus5.Application.Identity;

namespace Plus5.Api.Identity;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapTeacherAuthentication(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionOneApi().MapGroup("/auth");

        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            TypedResults.Ok(new CsrfResponse(antiforgery.GetAndStoreTokens(context).RequestToken!)))
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);
        group.MapPost("/verify-email", VerifyEmailAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);
        group.MapPost("/resend-verification", ResendVerificationAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);
        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);
        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);
        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .RequireCsrf()
            .RequireRateLimiting(IdentityServiceExtensions.AuthRateLimitPolicy);

        group.MapGet("/session", CurrentSessionAsync)
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy)
            .RequireCsrf();
        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy)
            .RequireCsrf();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RegisterAsync(request.Email, request.Password, cancellationToken);
        return result.Outcome switch
        {
            AuthenticationOutcome.Success => TypedResults.Accepted((string?)null),
            AuthenticationOutcome.DuplicateEmail => Problem(
                StatusCodes.Status409Conflict,
                "email_already_registered",
                "An account with this e-mail address already exists."),
            _ => PasswordProblem(),
        };
    }

    private static async Task<IResult> VerifyEmailAsync(
        TokenRequest request,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.VerifyEmailAsync(request.Email, request.Token, cancellationToken);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : Problem(StatusCodes.Status400BadRequest, "invalid_or_expired_token", "The token is invalid or expired.");
    }

    private static async Task<IResult> ResendVerificationAsync(
        EmailRequest request,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        await service.ResendVerificationAsync(request.Email, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "invalid_credentials",
                "E-mail address or password is incorrect.");
        }

        await context.SignInAsync(
            IdentityServiceExtensions.CookieScheme,
            IdentityServiceExtensions.CreatePrincipal(result),
            new AuthenticationProperties
            {
                AllowRefresh = false,
                IsPersistent = false,
                ExpiresUtc = result.SessionExpiresAtUtc,
            });
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        EmailRequest request,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        await service.ForgotPasswordAsync(request.Email, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);
        return result.Outcome switch
        {
            AuthenticationOutcome.Success => TypedResults.NoContent(),
            AuthenticationOutcome.PasswordRejected => PasswordProblem(),
            _ => Problem(StatusCodes.Status400BadRequest, "invalid_or_expired_token", "The token is invalid or expired."),
        };
    }

    private static async Task<IResult> CurrentSessionAsync(
        HttpContext context,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var accountId, out var sessionId))
        {
            return TypedResults.Unauthorized();
        }

        var session = await service.GetCurrentSessionAsync(accountId, sessionId, cancellationToken);
        return session is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new SessionResponse(session.Email, "Teacher", session.ExpiresAtUtc));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.TryRead(context.User, out var accountId, out var sessionId))
        {
            await service.LogoutAsync(accountId, sessionId, cancellationToken);
        }

        await context.SignOutAsync(IdentityServiceExtensions.CookieScheme);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext context,
        ITeacherAuthenticationService service,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var accountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.ChangePasswordAsync(
            accountId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
        if (result.Outcome == AuthenticationOutcome.PasswordRejected)
        {
            return PasswordProblem();
        }

        if (!result.IsSuccess)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "invalid_current_password",
                "The current password is incorrect.");
        }

        await context.SignOutAsync(IdentityServiceExtensions.CookieScheme);
        return TypedResults.NoContent();
    }

    private static IResult PasswordProblem() => Problem(
        StatusCodes.Status400BadRequest,
        "password_policy_failed",
        "Password must contain 12–128 characters with uppercase, lowercase, number, and symbol.");

    private static IResult Problem(int status, string code, string title) =>
        Results.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static RouteHandlerBuilder RequireCsrf(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "invalid_csrf_token",
                    "The anti-forgery token is missing or invalid.");
            }

            return await next(context);
        });
}

public sealed record RegisterRequest(
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, StringLength(128)] string Password);

public sealed record EmailRequest(
    [property: Required, EmailAddress, StringLength(320)] string Email);

public sealed record TokenRequest(
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, StringLength(128, MinimumLength = 32)] string Token);

public sealed record ResetPasswordRequest(
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, StringLength(128, MinimumLength = 32)] string Token,
    [property: Required, StringLength(128, MinimumLength = 12)] string NewPassword);

public sealed record ChangePasswordRequest(
    [property: Required, StringLength(128)] string CurrentPassword,
    [property: Required, StringLength(128, MinimumLength = 12)] string NewPassword);

public sealed record CsrfResponse(string Token);

public sealed record SessionResponse(string Email, string AccountType, DateTimeOffset ExpiresAtUtc);
