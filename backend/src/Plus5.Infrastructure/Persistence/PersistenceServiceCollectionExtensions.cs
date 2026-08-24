using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Plus5.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const int CommandTimeoutSeconds = 30;

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string? connectionString,
        bool allowUntrustedServerCertificate)
    {
        ArgumentNullException.ThrowIfNull(services);

        ValidateConnectionString(connectionString, allowUntrustedServerCertificate);

        services.AddDbContext<Plus5DbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(CommandTimeoutSeconds);
                    sqlServerOptions.MigrationsAssembly(typeof(Plus5DbContext).Assembly.FullName);
                }));

        return services;
    }

    public static void ValidateConnectionString(
        string? connectionString,
        bool allowUntrustedServerCertificate)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 is required and must be supplied through the environment/secrets layer.");
        }

        SqlConnectionStringBuilder builder;

        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 is not a valid SQL Server connection string.");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 must specify both Server and Database.");
        }

        var encryptionMode = Convert.ToString(builder["Encrypt"], CultureInfo.InvariantCulture);

        if (string.Equals(encryptionMode, "False", StringComparison.OrdinalIgnoreCase)
            || string.Equals(encryptionMode, "Optional", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 must enable transport encryption.");
        }

        if (builder.TrustServerCertificate && !allowUntrustedServerCertificate)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 may trust an unverified server certificate only in Development.");
        }

        if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Plus5 may not use AttachDBFilename.");
        }
    }
}
