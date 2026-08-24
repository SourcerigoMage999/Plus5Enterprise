using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Plus5.Api.Configuration;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Tests.Configuration;

public sealed class DataProtectionPersistenceOptionsTests
{
    [Fact]
    public void DevelopmentAllowsDatabaseKeyRingWithoutCertificateEncryption()
    {
        var validator = new DataProtectionPersistenceOptionsValidator(
            new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new DataProtectionPersistenceOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ProductionRequiresCertificateEncryption()
    {
        var validator = new DataProtectionPersistenceOptionsValidator(
            new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, new DataProtectionPersistenceOptions());

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CertificatePathAndPasswordMustBeProvidedTogether()
    {
        var validator = new DataProtectionPersistenceOptionsValidator(
            new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new DataProtectionPersistenceOptions
        {
            CertificatePath = Path.GetFullPath("data-protection.pfx"),
        });

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("both", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CertificatePathMustBeAbsolute()
    {
        var validator = new DataProtectionPersistenceOptionsValidator(
            new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new DataProtectionPersistenceOptions
        {
            CertificatePath = "data-protection.pfx",
            CertificatePassword = "test-only-password",
        });

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Plus5.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
