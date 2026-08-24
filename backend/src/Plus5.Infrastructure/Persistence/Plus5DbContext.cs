using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Identity;

namespace Plus5.Infrastructure.Persistence;

public sealed class Plus5DbContext(DbContextOptions<Plus5DbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<AuthenticatedSession> AuthenticatedSessions => Set<AuthenticatedSession>();

    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Plus5DbContext).Assembly);
    }
}
