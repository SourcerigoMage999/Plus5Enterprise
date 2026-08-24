using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Identity;
using Plus5.Domain.Identity;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Identity;

public sealed class TeacherAuthenticationService(
    Plus5DbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    IAccountEmailSender emailSender,
    TimeProvider timeProvider) : ITeacherAuthenticationService
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);

    public async Task<AuthenticationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(email, out var canonicalEmail, out var normalizedEmail)
            || !IsPasswordAllowed(password))
        {
            return new AuthenticationResult(AuthenticationOutcome.PasswordRejected);
        }

        if (await dbContext.UserAccounts.AnyAsync(
                account => account.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return new AuthenticationResult(AuthenticationOutcome.DuplicateEmail);
        }

        var now = timeProvider.GetUtcNow();
        var account = new UserAccount(Guid.NewGuid(), canonicalEmail, normalizedEmail, now);
        account.SetPasswordHash(passwordHasher.HashPassword(account, password), now);
        dbContext.UserAccounts.Add(account);
        var rawToken = await ReplaceTokenAsync(
            account.Id,
            AccountTokenPurpose.EmailVerification,
            VerificationLifetime,
            cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmailViolation(exception))
        {
            return new AuthenticationResult(AuthenticationOutcome.DuplicateEmail);
        }

        await emailSender.SendEmailVerificationAsync(account.Email, rawToken, cancellationToken);
        return new AuthenticationResult(AuthenticationOutcome.Success, account.Id, Email: account.Email);
    }

    public async Task<AuthenticationResult> VerifyEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        var account = await FindByEmailAsync(email, cancellationToken);
        if (account is null || account.Status != AccountStatus.PendingEmailVerification)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidToken);
        }

        var storedToken = await FindConsumableTokenAsync(
            account.Id,
            AccountTokenPurpose.EmailVerification,
            token,
            cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (storedToken is null || !storedToken.CanConsume(now) || !account.ConfirmEmail(now))
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidToken);
        }

        storedToken.Consume(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthenticationResult(AuthenticationOutcome.Success, account.Id, Email: account.Email);
    }

    public async Task ResendVerificationAsync(string email, CancellationToken cancellationToken)
    {
        var account = await FindByEmailAsync(email, cancellationToken);
        if (account is null || account.Status != AccountStatus.PendingEmailVerification)
        {
            return;
        }

        var token = await ReplaceTokenAsync(
            account.Id,
            AccountTokenPurpose.EmailVerification,
            VerificationLifetime,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await emailSender.SendEmailVerificationAsync(account.Email, token, cancellationToken);
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var account = await FindByEmailAsync(email, cancellationToken);
        if (account is null || !account.CanAuthenticate)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidCredentials);
        }

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.SetPasswordHash(passwordHasher.HashPassword(account, password), now);
        }

        var session = new AuthenticatedSession(
            Guid.NewGuid(),
            account.Id,
            account.SecurityStamp,
            now,
            now.Add(SessionLifetime));
        dbContext.AuthenticatedSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            AuthenticationOutcome.Success,
            account.Id,
            session.Id,
            account.Email,
            session.ExpiresAtUtc);
    }

    public async Task LogoutAsync(
        Guid userAccountId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.AuthenticatedSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId && candidate.UserAccountId == userAccountId,
            cancellationToken);

        if (session is not null)
        {
            session.Revoke(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var account = await FindByEmailAsync(email, cancellationToken);
        if (account is null || !account.CanAuthenticate)
        {
            return;
        }

        var token = await ReplaceTokenAsync(
            account.Id,
            AccountTokenPurpose.PasswordReset,
            PasswordResetLifetime,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await emailSender.SendPasswordResetAsync(account.Email, token, cancellationToken);
    }

    public async Task<AuthenticationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (!IsPasswordAllowed(newPassword))
        {
            return new AuthenticationResult(AuthenticationOutcome.PasswordRejected);
        }

        var account = await FindByEmailAsync(email, cancellationToken);
        if (account is null || !account.CanAuthenticate)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidToken);
        }

        var storedToken = await FindConsumableTokenAsync(
            account.Id,
            AccountTokenPurpose.PasswordReset,
            token,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (storedToken is null || !storedToken.CanConsume(now))
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidToken);
        }

        storedToken.Consume(now);
        account.SetPasswordHash(passwordHasher.HashPassword(account, newPassword), now);
        await RevokeAllSessionsAsync(account.Id, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthenticationResult(AuthenticationOutcome.Success, account.Id, Email: account.Email);
    }

    public async Task<AuthenticationResult> ChangePasswordAsync(
        Guid userAccountId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (!IsPasswordAllowed(newPassword))
        {
            return new AuthenticationResult(AuthenticationOutcome.PasswordRejected);
        }

        var account = await dbContext.UserAccounts.SingleOrDefaultAsync(
            candidate => candidate.Id == userAccountId,
            cancellationToken);
        if (account is null || !account.CanAuthenticate)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidCurrentPassword);
        }

        if (passwordHasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword)
            == PasswordVerificationResult.Failed)
        {
            return new AuthenticationResult(AuthenticationOutcome.InvalidCurrentPassword);
        }

        var now = timeProvider.GetUtcNow();
        account.SetPasswordHash(passwordHasher.HashPassword(account, newPassword), now);
        await RevokeAllSessionsAsync(account.Id, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthenticationResult(AuthenticationOutcome.Success, account.Id, Email: account.Email);
    }

    public Task<CurrentSession?> GetCurrentSessionAsync(
        Guid userAccountId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.AuthenticatedSessions
            .AsNoTracking()
            .Where(session =>
                session.Id == sessionId
                && session.UserAccountId == userAccountId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > now
                && session.SecurityStamp == session.UserAccount.SecurityStamp
                && session.UserAccount.Status == AccountStatus.Active)
            .Select(session => new CurrentSession(
                session.UserAccountId,
                session.Id,
                session.UserAccount.Email,
                session.ExpiresAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static bool IsPasswordAllowed(string password) =>
        password.Length is >= MinimumPasswordLength and <= MaximumPasswordLength
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(character => !char.IsLetterOrDigit(character));

    private async Task<UserAccount?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(email, out _, out var normalizedEmail))
        {
            return null;
        }

        return await dbContext.UserAccounts.SingleOrDefaultAsync(
            account => account.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    private async Task<string> ReplaceTokenAsync(
        Guid accountId,
        AccountTokenPurpose purpose,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var existingTokens = await dbContext.AccountTokens
            .Where(token =>
                token.UserAccountId == accountId
                && token.Purpose == purpose
                && token.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.Invalidate(now);
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        dbContext.AccountTokens.Add(new AccountToken(
            Guid.NewGuid(),
            accountId,
            purpose,
            HashToken(rawToken),
            now,
            now.Add(lifetime)));
        return rawToken;
    }

    private Task<AccountToken?> FindConsumableTokenAsync(
        Guid accountId,
        AccountTokenPurpose purpose,
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length != 64)
        {
            return Task.FromResult<AccountToken?>(null);
        }

        var hash = HashToken(rawToken);
        return dbContext.AccountTokens.SingleOrDefaultAsync(
            token =>
                token.UserAccountId == accountId
                && token.Purpose == purpose
                && token.TokenHash == hash,
            cancellationToken);
    }

    private async Task RevokeAllSessionsAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.AuthenticatedSessions
            .Where(session => session.UserAccountId == accountId && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(now);
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static bool IsDuplicateEmailViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(
            "UX_UserAccounts_NormalizedEmail",
            StringComparison.OrdinalIgnoreCase);

    private static bool TryNormalizeEmail(
        string email,
        out string canonicalEmail,
        out string normalizedEmail)
    {
        canonicalEmail = string.Empty;
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(email) || email.Length > 320)
        {
            return false;
        }

        try
        {
            var parsed = new MailAddress(email.Trim());
            canonicalEmail = parsed.Address;
            normalizedEmail = canonicalEmail.ToUpper(CultureInfo.InvariantCulture);
            return string.Equals(canonicalEmail, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
