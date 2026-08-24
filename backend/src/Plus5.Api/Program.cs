using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Plus5.Api.Conventions;
using Plus5.Api.Configuration;
using Plus5.Api.Observability;
using Plus5.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddValidatedConfiguration();
builder.AddObservability();
builder.Services.AddApiConventions();

builder.Services.AddPersistence(
    builder.Configuration.GetConnectionString("Plus5"),
    allowUntrustedServerCertificate: builder.Environment.IsDevelopment());

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<Plus5DbContext>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        customTestQuery: async (dbContext, cancellationToken) =>
            !(await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any());

var app = builder.Build();

app.UseObservability();
app.UseApiConventions();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

app.Run();

public partial class Program;
