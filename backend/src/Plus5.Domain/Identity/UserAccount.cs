namespace Plus5.Domain.Identity;

public sealed class UserAccount
{
    private UserAccount()
    {
    }

    public UserAccount(
        Guid id,
        string email,
        string normalizedEmail,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Account identifier is required.", nameof(id));
        }

        Id = id;
        Email = RequireText(email, nameof(email));
        NormalizedEmail = RequireText(normalizedEmail, nameof(normalizedEmail));
        Status = AccountStatus.PendingEmailVerification;
        SecurityStamp = Guid.NewGuid();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public AccountStatus Status { get; private set; }

    public Guid SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool CanAuthenticate => Status == AccountStatus.Active;

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = RequireText(passwordHash, nameof(passwordHash));
        SecurityStamp = Guid.NewGuid();
        UpdatedAtUtc = now;
    }

    public bool ConfirmEmail(DateTimeOffset now)
    {
        if (Status != AccountStatus.PendingEmailVerification)
        {
            return false;
        }

        Status = AccountStatus.Active;
        SecurityStamp = Guid.NewGuid();
        UpdatedAtUtc = now;
        return true;
    }

    public void Deactivate(DateTimeOffset now)
    {
        Status = AccountStatus.Deactivated;
        SecurityStamp = Guid.NewGuid();
        UpdatedAtUtc = now;
    }

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value;
}
