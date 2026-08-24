using Plus5.Domain.Identity;

namespace Plus5.Application.Identity;

public enum AuthenticationOutcome
{
    Success,
    DuplicateEmail,
    InvalidCredentials,
    InvalidToken,
    InvalidCurrentPassword,
    PasswordRejected,
}

public sealed record AuthenticationResult(
    AuthenticationOutcome Outcome,
    Guid? UserAccountId = null,
    Guid? SessionId = null,
    string? Email = null,
    DateTimeOffset? SessionExpiresAtUtc = null)
{
    public bool IsSuccess => Outcome == AuthenticationOutcome.Success;
}

public sealed record CurrentSession(
    Guid UserAccountId,
    Guid SessionId,
    string Email,
    DateTimeOffset ExpiresAtUtc);

public interface ITeacherAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken);

    Task<AuthenticationResult> VerifyEmailAsync(string email, string token, CancellationToken cancellationToken);

    Task ResendVerificationAsync(string email, CancellationToken cancellationToken);

    Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userAccountId, Guid sessionId, CancellationToken cancellationToken);

    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<AuthenticationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    Task<AuthenticationResult> ChangePasswordAsync(
        Guid userAccountId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);

    Task<CurrentSession?> GetCurrentSessionAsync(
        Guid userAccountId,
        Guid sessionId,
        CancellationToken cancellationToken);
}

public interface IAccountEmailSender
{
    Task SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken);
}
