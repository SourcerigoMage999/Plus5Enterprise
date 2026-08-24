namespace Plus5.Domain.Identity;

public sealed class AuthenticatedSession
{
    private AuthenticatedSession()
    {
    }

    public AuthenticatedSession(
        Guid id,
        Guid userAccountId,
        Guid securityStamp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || userAccountId == Guid.Empty || securityStamp == Guid.Empty)
        {
            throw new ArgumentException("Session identifiers are required.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);

        Id = id;
        UserAccountId = userAccountId;
        SecurityStamp = securityStamp;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserAccountId { get; private set; }

    public Guid SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    public bool IsActive(DateTimeOffset now, Guid currentSecurityStamp) =>
        RevokedAtUtc is null
        && ExpiresAtUtc > now
        && SecurityStamp == currentSecurityStamp
        && UserAccount.CanAuthenticate;

    public void Revoke(DateTimeOffset now)
    {
        RevokedAtUtc ??= now;
    }
}
