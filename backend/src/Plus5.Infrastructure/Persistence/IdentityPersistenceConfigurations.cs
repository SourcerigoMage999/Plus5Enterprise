using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Identity;

namespace Plus5.Infrastructure.Persistence;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts", table =>
            table.HasCheckConstraint(
                "CK_UserAccounts_Status",
                "[Status] IN (1, 2, 3)"));
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Email).HasMaxLength(320).IsRequired();
        builder.Property(account => account.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(account => account.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(account => account.Status).HasConversion<int>().IsRequired();
        builder.Property(account => account.SecurityStamp).IsRequired();
        builder.Property(account => account.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(account => account.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasIndex(account => account.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_UserAccounts_NormalizedEmail");
    }
}

internal sealed class AuthenticatedSessionConfiguration
    : IEntityTypeConfiguration<AuthenticatedSession>
{
    public void Configure(EntityTypeBuilder<AuthenticatedSession> builder)
    {
        builder.ToTable("AuthenticatedSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.SecurityStamp).IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.ExpiresAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.RevokedAtUtc).HasPrecision(7);
        builder.HasOne(session => session.UserAccount)
            .WithMany()
            .HasForeignKey(session => session.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(session => new { session.UserAccountId, session.ExpiresAtUtc })
            .HasDatabaseName("IX_AuthenticatedSessions_Account_Expiry");
    }
}

internal sealed class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
{
    public void Configure(EntityTypeBuilder<AccountToken> builder)
    {
        builder.ToTable("AccountTokens", table =>
            table.HasCheckConstraint(
                "CK_AccountTokens_Purpose",
                "[Purpose] IN (1, 2)"));
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Purpose).HasConversion<int>().IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(token => token.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasPrecision(7).IsRequired();
        builder.Property(token => token.ConsumedAtUtc).HasPrecision(7);
        builder.HasOne(token => token.UserAccount)
            .WithMany()
            .HasForeignKey(token => token.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_AccountTokens_TokenHash");
        builder.HasIndex(token => new { token.UserAccountId, token.Purpose })
            .IsUnique()
            .HasFilter("[ConsumedAtUtc] IS NULL")
            .HasDatabaseName("UX_AccountTokens_ActivePurpose");
        builder.HasIndex(token => new { token.UserAccountId, token.Purpose, token.ExpiresAtUtc })
            .HasDatabaseName("IX_AccountTokens_Account_Purpose_Expiry");
    }
}
