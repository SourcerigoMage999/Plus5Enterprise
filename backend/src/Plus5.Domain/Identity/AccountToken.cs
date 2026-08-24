namespace Plus5.Domain.Identity;

public sealed class AccountToken
{
    private AccountToken()
    {
    }

    public AccountToken(
        Guid id,
        Guid userAccountId,
        AccountTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || userAccountId == Guid.Empty)
        {
            throw new ArgumentException("Token identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);

        Id = id;
        UserAccountId = userAccountId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserAccountId { get; private set; }

    public AccountTokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    public bool CanConsume(DateTimeOffset now) => ConsumedAtUtc is null && ExpiresAtUtc > now;

    public void Consume(DateTimeOffset now)
    {
        if (!CanConsume(now))
        {
            throw new InvalidOperationException("Token cannot be consumed.");
        }

        ConsumedAtUtc = now;
    }

    public void Invalidate(DateTimeOffset now)
    {
        ConsumedAtUtc ??= now;
    }
}
