using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Plus5.Infrastructure.Persistence;

public sealed class Plus5DbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<Plus5DbContext>
{
    public const string MigrationConnectionStringEnvironmentVariable =
        "PLUS5_MIGRATION_CONNECTION_STRING";
    public const string AllowUntrustedCertificateEnvironmentVariable =
        "PLUS5_MIGRATION_ALLOW_UNTRUSTED_CERTIFICATE";

    public Plus5DbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            MigrationConnectionStringEnvironmentVariable);

        var allowUntrustedServerCertificate = string.Equals(
            Environment.GetEnvironmentVariable(AllowUntrustedCertificateEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

        PersistenceServiceCollectionExtensions.ValidateConnectionString(
            connectionString,
            allowUntrustedServerCertificate);

        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(30);
                    sqlServerOptions.MigrationsAssembly(typeof(Plus5DbContext).Assembly.FullName);
                })
            .Options;

        return new Plus5DbContext(options);
    }
}
