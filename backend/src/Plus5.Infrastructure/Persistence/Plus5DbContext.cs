using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

public sealed class Plus5DbContext(DbContextOptions<Plus5DbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<AuthenticatedSession> AuthenticatedSessions => Set<AuthenticatedSession>();

    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

    public DbSet<Program> Programs => Set<Program>();

    public DbSet<SchoolGrade> SchoolGrades => Set<SchoolGrade>();

    public DbSet<ProficiencyLevel> ProficiencyLevels => Set<ProficiencyLevel>();

    public DbSet<Curriculum> Curricula => Set<Curriculum>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Guardian> Guardians => Set<Guardian>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<RecurringSessionSeries> RecurringSessionSeries => Set<RecurringSessionSeries>();

    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Plus5DbContext).Assembly);
    }
}
