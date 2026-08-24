using Microsoft.EntityFrameworkCore;

namespace Plus5.Infrastructure.Persistence;

public sealed class Plus5DbContext(DbContextOptions<Plus5DbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Plus5DbContext).Assembly);
    }
}
