using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Identity;
using Plus5.Domain.Identity;
using Plus5.Infrastructure.Identity;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Identity;

public sealed class TeacherAuthenticationServiceTests
{
    private const string Email = "teacher@example.test";
    private const string Password = "StrongPassword42!";
    private const string NewPassword = "NewStrongPassword43!";

    [Fact]
    public async Task RegistrationCreatesOnlyPendingTeacherAccountAndHashedVerificationToken()
    {
        await using var fixture = CreateFixture();

        var result = await fixture.Service.RegisterAsync(Email, Password, CancellationToken.None);
        var account = await fixture.DbContext.UserAccounts.SingleAsync();
        var token = await fixture.DbContext.AccountTokens.SingleAsync();

        Assert.Equal(AuthenticationOutcome.Success, result.Outcome);
        Assert.Equal(AccountStatus.PendingEmailVerification, account.Status);
        Assert.Equal(Email.ToUpperInvariant(), account.NormalizedEmail);
        Assert.NotEqual(Password, account.PasswordHash);
        Assert.Equal(AccountTokenPurpose.EmailVerification, token.Purpose);
        Assert.NotEqual(fixture.EmailSender.VerificationToken, token.TokenHash);
        Assert.Equal(64, token.TokenHash.Length);
        Assert.Empty(fixture.DbContext.AuthenticatedSessions);
    }

    [Fact]
    public async Task DuplicateEmailIsRejectedCaseInsensitively()
    {
        await using var fixture = CreateFixture();
        await fixture.Service.RegisterAsync(Email, Password, CancellationToken.None);

        var duplicate = await fixture.Service.RegisterAsync(
            Email.ToUpperInvariant(),
            Password,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.DuplicateEmail, duplicate.Outcome);
        Assert.Single(fixture.DbContext.UserAccounts);
    }

    [Fact]
    public async Task PendingAccountCannotLoginAndVerificationTokenIsSingleUse()
    {
        await using var fixture = CreateFixture();
        await fixture.Service.RegisterAsync(Email, Password, CancellationToken.None);

        var pendingLogin = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);
        var invalid = await fixture.Service.VerifyEmailAsync(Email, "invalid", CancellationToken.None);
        var verified = await fixture.Service.VerifyEmailAsync(
            Email,
            fixture.EmailSender.VerificationToken!,
            CancellationToken.None);
        var reused = await fixture.Service.VerifyEmailAsync(
            Email,
            fixture.EmailSender.VerificationToken!,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.InvalidCredentials, pendingLogin.Outcome);
        Assert.Equal(AuthenticationOutcome.InvalidToken, invalid.Outcome);
        Assert.Equal(AuthenticationOutcome.Success, verified.Outcome);
        Assert.Equal(AuthenticationOutcome.InvalidToken, reused.Outcome);
        Assert.Equal(AccountStatus.Active, (await fixture.DbContext.UserAccounts.SingleAsync()).Status);
    }

    [Fact]
    public async Task ExpiredVerificationTokenCannotActivateAccount()
    {
        await using var fixture = CreateFixture();
        await fixture.Service.RegisterAsync(Email, Password, CancellationToken.None);
        fixture.Time.Advance(TeacherAuthenticationService.VerificationLifetime.Add(TimeSpan.FromSeconds(1)));

        var result = await fixture.Service.VerifyEmailAsync(
            Email,
            fixture.EmailSender.VerificationToken!,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.InvalidToken, result.Outcome);
    }

    [Fact]
    public async Task ActiveTeacherCanLoginAndLogoutRevokesCurrentSession()
    {
        await using var fixture = CreateFixture();
        await RegisterAndVerifyAsync(fixture);
        var login = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);

        var active = await fixture.Service.GetCurrentSessionAsync(
            login.UserAccountId!.Value,
            login.SessionId!.Value,
            CancellationToken.None);
        await fixture.Service.LogoutAsync(
            login.UserAccountId.Value,
            login.SessionId.Value,
            CancellationToken.None);
        var revoked = await fixture.Service.GetCurrentSessionAsync(
            login.UserAccountId.Value,
            login.SessionId.Value,
            CancellationToken.None);

        Assert.NotNull(active);
        Assert.Null(revoked);
    }

    [Fact]
    public async Task PasswordResetIsSingleUseAndRevokesEverySession()
    {
        await using var fixture = CreateFixture();
        await RegisterAndVerifyAsync(fixture);
        var first = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);
        var second = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);
        await fixture.Service.ForgotPasswordAsync(Email, CancellationToken.None);

        var reset = await fixture.Service.ResetPasswordAsync(
            Email,
            fixture.EmailSender.PasswordResetToken!,
            NewPassword,
            CancellationToken.None);
        var reused = await fixture.Service.ResetPasswordAsync(
            Email,
            fixture.EmailSender.PasswordResetToken!,
            Password,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.Success, reset.Outcome);
        Assert.Equal(AuthenticationOutcome.InvalidToken, reused.Outcome);
        Assert.Null(await fixture.Service.GetCurrentSessionAsync(first.UserAccountId!.Value, first.SessionId!.Value, CancellationToken.None));
        Assert.Null(await fixture.Service.GetCurrentSessionAsync(second.UserAccountId!.Value, second.SessionId!.Value, CancellationToken.None));
        Assert.Equal(AuthenticationOutcome.Success, (await fixture.Service.LoginAsync(Email, NewPassword, CancellationToken.None)).Outcome);
        Assert.Equal(AuthenticationOutcome.InvalidCredentials, (await fixture.Service.LoginAsync(Email, Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task PasswordChangeRevokesEverySession()
    {
        await using var fixture = CreateFixture();
        await RegisterAndVerifyAsync(fixture);
        var first = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);
        var second = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);

        var changed = await fixture.Service.ChangePasswordAsync(
            first.UserAccountId!.Value,
            Password,
            NewPassword,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.Success, changed.Outcome);
        Assert.All(await fixture.DbContext.AuthenticatedSessions.ToListAsync(), session => Assert.NotNull(session.RevokedAtUtc));
        Assert.Null(await fixture.Service.GetCurrentSessionAsync(second.UserAccountId!.Value, second.SessionId!.Value, CancellationToken.None));
    }

    [Fact]
    public async Task DeactivatedAccountRejectsExistingSession()
    {
        await using var fixture = CreateFixture();
        await RegisterAndVerifyAsync(fixture);
        var login = await fixture.Service.LoginAsync(Email, Password, CancellationToken.None);
        var account = await fixture.DbContext.UserAccounts.SingleAsync();
        account.Deactivate(fixture.Time.GetUtcNow());
        await fixture.DbContext.SaveChangesAsync();

        var session = await fixture.Service.GetCurrentSessionAsync(
            login.UserAccountId!.Value,
            login.SessionId!.Value,
            CancellationToken.None);

        Assert.Null(session);
        Assert.Equal(AuthenticationOutcome.InvalidCredentials, (await fixture.Service.LoginAsync(Email, Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task ForgotPasswordHasSamePublicOutcomeForKnownAndUnknownAccount()
    {
        await using var fixture = CreateFixture();
        await RegisterAndVerifyAsync(fixture);

        await fixture.Service.ForgotPasswordAsync(Email, CancellationToken.None);
        await fixture.Service.ForgotPasswordAsync("unknown@example.test", CancellationToken.None);

        Assert.NotNull(fixture.EmailSender.PasswordResetToken);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercasepassword42!")]
    [InlineData("ALLUPPERCASEPASSWORD42!")]
    [InlineData("NoNumberPassword!")]
    [InlineData("NoSymbolPassword42")]
    public void PasswordPolicyRejectsWeakValues(string password)
    {
        Assert.False(TeacherAuthenticationService.IsPasswordAllowed(password));
    }

    private static async Task RegisterAndVerifyAsync(TestFixture fixture)
    {
        await fixture.Service.RegisterAsync(Email, Password, CancellationToken.None);
        await fixture.Service.VerifyEmailAsync(
            Email,
            fixture.EmailSender.VerificationToken!,
            CancellationToken.None);
    }

    private static TestFixture CreateFixture()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var dbContext = new Plus5DbContext(options);
        var emailSender = new CapturingEmailSender();
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero));
        var service = new TeacherAuthenticationService(
            dbContext,
            new PasswordHasher<UserAccount>(),
            emailSender,
            time);
        return new TestFixture(dbContext, emailSender, time, service);
    }

    private sealed record TestFixture(
        Plus5DbContext DbContext,
        CapturingEmailSender EmailSender,
        MutableTimeProvider Time,
        TeacherAuthenticationService Service) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
    }

    private sealed class CapturingEmailSender : IAccountEmailSender
    {
        public string? VerificationToken { get; private set; }
        public string? PasswordResetToken { get; private set; }

        public Task SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken)
        {
            VerificationToken = token;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
        {
            PasswordResetToken = token;
            return Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
