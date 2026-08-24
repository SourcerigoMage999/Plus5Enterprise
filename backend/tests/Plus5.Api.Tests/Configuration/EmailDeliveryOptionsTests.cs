using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Plus5.Api.Configuration;
using Plus5.Infrastructure.Identity;

namespace Plus5.Api.Tests.Configuration;

public sealed class EmailDeliveryOptionsTests
{
    [Fact]
    public void ValidDevelopmentSmtpConfigurationIsAccepted()
    {
        var validator = new EmailDeliveryOptionsValidator(new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            Host = "localhost",
            Port = 1025,
            UseSsl = false,
            FromAddress = "no-reply@plus5.local",
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ProductionRequiresTls()
    {
        var validator = new EmailDeliveryOptionsValidator(new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            Host = "smtp.example.test",
            Port = 587,
            UseSsl = false,
            FromAddress = "no-reply@example.test",
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("UseSsl", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("", 587, "no-reply@example.test")]
    [InlineData("smtp.example.test", 0, "no-reply@example.test")]
    [InlineData("smtp.example.test", 587, "not-an-email")]
    public void InvalidRequiredSmtpValuesAreRejected(string host, int port, string fromAddress)
    {
        var validator = new EmailDeliveryOptionsValidator(new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            Host = host,
            Port = port,
            UseSsl = false,
            FromAddress = fromAddress,
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void SmtpCredentialsMustBeProvidedAsAPair()
    {
        var validator = new EmailDeliveryOptionsValidator(new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            Host = "smtp.example.test",
            Port = 587,
            UseSsl = true,
            FromAddress = "no-reply@example.test",
            UserName = "smtp-user",
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Password", StringComparison.Ordinal));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Plus5.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
