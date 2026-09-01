using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Plus5.Application.Identity;
using Plus5.Application.Students;
using Plus5.Domain.Identity;
using Plus5.Infrastructure.Identity;
using Plus5.Infrastructure.Students;

namespace Plus5.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const int CommandTimeoutSeconds = 30;

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string? connectionString,
        bool allowUntrustedServerCertificate,
        string? dataProtectionCertificatePath,
        string? dataProtectionCertificatePassword,
        bool allowUnprotectedDataProtectionKeys)
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

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("Plus5Enterprise")
            .PersistKeysToDbContext<Plus5DbContext>();

        var hasCertificatePath = !string.IsNullOrWhiteSpace(dataProtectionCertificatePath);
        var hasCertificatePassword = !string.IsNullOrWhiteSpace(dataProtectionCertificatePassword);

        if (hasCertificatePath != hasCertificatePassword)
        {
            throw new InvalidOperationException(
                "Data Protection certificate path and password must be supplied together.");
        }

        if (hasCertificatePath && hasCertificatePassword)
        {
            if (!Path.IsPathFullyQualified(dataProtectionCertificatePath!))
            {
                throw new InvalidOperationException(
                    "The Data Protection certificate path must be absolute.");
            }

            X509Certificate2 certificate;

            try
            {
                certificate = X509CertificateLoader.LoadPkcs12FromFile(
                    dataProtectionCertificatePath!,
                    dataProtectionCertificatePassword,
                    X509KeyStorageFlags.EphemeralKeySet);
            }
            catch (Exception exception) when (exception is CryptographicException
                or IOException
                or UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    "The Data Protection certificate could not be loaded.");
            }

            dataProtection.ProtectKeysWithCertificate(certificate);
        }
        else if (!allowUnprotectedDataProtectionKeys)
        {
            throw new InvalidOperationException(
                "A Data Protection certificate is required outside Development.");
        }

        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddScoped<ITeacherAuthenticationService, TeacherAuthenticationService>();
        services.AddScoped<IStudentListQuery, EfStudentListQuery>();
        services.AddScoped<IStudentCreationService, EfStudentCreationService>();
        services.AddScoped<IStudentDossierQuery, EfStudentDossierQuery>();
        services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
        services.AddSingleton(TimeProvider.System);

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
