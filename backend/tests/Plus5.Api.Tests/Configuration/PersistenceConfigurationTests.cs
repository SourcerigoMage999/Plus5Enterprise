using Microsoft.Extensions.DependencyInjection;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Configuration;

public sealed class PersistenceConfigurationTests
{
    private const string SecureConnectionString =
        "Server=sql.example.test;Database=Plus5;Integrated Security=True;" +
        "Encrypt=True;TrustServerCertificate=False";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-connection-string")]
    [InlineData("Server=sql.example.test;Encrypt=True")]
    [InlineData("Database=Plus5;Encrypt=True")]
    [InlineData("Server=sql.example.test;Database=Plus5;Encrypt=False")]
    [InlineData("Server=sql.example.test;Database=Plus5;Encrypt=True;AttachDBFilename=test.mdf")]
    public void InvalidConnectionStringFailsFast(string? connectionString)
    {
        Assert.Throws<InvalidOperationException>(() =>
            PersistenceServiceCollectionExtensions.ValidateConnectionString(
                connectionString,
                allowUntrustedServerCertificate: false));
    }

    [Fact]
    public void UntrustedCertificateFailsOutsideDevelopment()
    {
        const string connectionString =
            "Server=sql.example.test;Database=Plus5;Integrated Security=True;" +
            "Encrypt=True;TrustServerCertificate=True";

        Assert.Throws<InvalidOperationException>(() =>
            PersistenceServiceCollectionExtensions.ValidateConnectionString(
                connectionString,
                allowUntrustedServerCertificate: false));
    }

    [Fact]
    public void UntrustedCertificateIsAllowedForDevelopmentSqlContainer()
    {
        const string connectionString =
            "Server=localhost;Database=Plus5;Integrated Security=True;" +
            "Encrypt=True;TrustServerCertificate=True";

        PersistenceServiceCollectionExtensions.ValidateConnectionString(
            connectionString,
            allowUntrustedServerCertificate: true);
    }

    [Fact]
    public void PersistenceRegistersDbContextAsScoped()
    {
        var services = new ServiceCollection();

        services.AddPersistence(
            SecureConnectionString,
            allowUntrustedServerCertificate: false);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(Plus5DbContext)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
