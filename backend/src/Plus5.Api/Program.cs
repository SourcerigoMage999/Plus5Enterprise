using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Plus5.Api.Conventions;
using Plus5.Api.Configuration;
using Plus5.Api.Observability;
using Plus5.Api.Identity;
using Plus5.Api.Students;
using Plus5.Api.Groups;
using Plus5.Api.Scheduling;
using Plus5.Api.Readiness;
using Plus5.Api.Materials;
using Plus5.Infrastructure.Persistence;
using Plus5.Infrastructure.Materials;

var builder = WebApplication.CreateBuilder(args);

builder.AddValidatedConfiguration();
builder.AddObservability();
builder.Services.AddApiConventions();
builder.Services.AddTeacherIdentity(
    builder.Environment.IsDevelopment(),
    builder.Configuration["Frontend:PublicOrigin"]!);
builder.Services.AddOptions<MaterialStorageOptions>()
    .Bind(builder.Configuration.GetSection(MaterialStorageOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps
                || (builder.Environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)),
        "MaterialStorage:ServiceUrl must be an absolute HTTPS URL (HTTP is allowed only in Development).")
    .Validate(options => !string.IsNullOrWhiteSpace(options.AccessKeyId)
            && !string.IsNullOrWhiteSpace(options.SecretAccessKey)
            && !string.IsNullOrWhiteSpace(options.QuarantineBucket)
            && !string.IsNullOrWhiteSpace(options.CleanBucket)
            && !string.Equals(options.QuarantineBucket, options.CleanBucket, StringComparison.Ordinal),
        "MaterialStorage credentials and distinct quarantine/clean buckets are required.")
    .ValidateOnStart();
builder.Services.AddOptions<MalwareScannerOptions>()
    .Bind(builder.Configuration.GetSection(MalwareScannerOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Host)
            && options.Port is > 0 and <= 65535
            && options.TimeoutSeconds is >= 10 and <= 300,
        "MalwareScanner host, port and timeout are invalid.")
    .ValidateOnStart();

builder.Services.AddPersistence(
    builder.Configuration.GetConnectionString("Plus5"),
    allowUntrustedServerCertificate: builder.Environment.IsDevelopment(),
    dataProtectionCertificatePath: builder.Configuration["DataProtection:CertificatePath"],
    dataProtectionCertificatePassword: builder.Configuration["DataProtection:CertificatePassword"],
    allowUnprotectedDataProtectionKeys: builder.Environment.IsDevelopment());

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<Plus5DbContext>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        customTestQuery: async (dbContext, cancellationToken) =>
            !(await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any());
builder.Services.AddHostedService<ScheduleMaterializationWorker>();
builder.Services.AddHostedService<ReadinessRefreshWorker>();

var app = builder.Build();

app.UseObservability();
app.UseApiConventions();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapTeacherAuthentication();
app.MapStudentList();
app.MapStudentCreation();
app.MapStudentDossier();
app.MapStudentReadiness();
app.MapStudentEditing();
app.MapGroups();
app.MapScheduleCalendar();
app.MapMaterialLibrary();
app.MapMaterialImport();

app.Run();

public partial class Program;
