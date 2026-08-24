using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plus5.Api.Contracts;
using Plus5.Api.Conventions;

namespace Plus5.Api.Tests.Conventions;

public sealed class ApiConventionsTests
{
    [Fact]
    public async Task VersionOneRouteGroupUsesCanonicalPrefix()
    {
        await using var app = await StartTestApplication(application =>
            application.MapVersionOneApi().MapGet("/probe", () => TypedResults.Ok()));
        using var client = app.GetTestClient();

        using var versionedResponse = await client.GetAsync("/api/v1/probe", CancellationToken.None);
        using var unversionedResponse = await client.GetAsync("/probe", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, versionedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unversionedResponse.StatusCode);
        Assert.Equal(ApiProblemCodes.NotFound, await ReadProblemCode(unversionedResponse));
    }

    [Fact]
    public async Task ValidationFailureUsesStandardProblemDetailsContract()
    {
        await using var app = await StartTestApplication(application =>
            application.MapPost(
                "/api/v1/validation-probe",
                (ValidationProbeRequest request) => TypedResults.Ok(request)));
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/validation-probe",
            new ValidationProbeRequest(string.Empty),
            CancellationToken.None);
        using var problem = await ReadProblem(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ApiProblemCodes.ValidationFailed, problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("urn:plus5:problem:validation_failed", problem.RootElement.GetProperty("type").GetString());
        Assert.Equal("/api/v1/validation-probe", problem.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        Assert.Equal(JsonValueKind.Object, problem.RootElement.GetProperty("errors").ValueKind);
    }

    [Fact]
    public async Task UnexpectedExceptionDoesNotExposeInternalDetails()
    {
        const string sensitiveExceptionMessage = "sensitive-internal-exception-detail";
        await using var app = await StartTestApplication(application =>
            application.MapGet("/api/v1/failure", () =>
            {
                throw new InvalidOperationException(sensitiveExceptionMessage);
            }));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/v1/failure?secret=query-value", CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        using var problem = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ApiProblemCodes.InternalError, problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("/api/v1/failure", problem.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(sensitiveExceptionMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("query-value", body, StringComparison.Ordinal);
        Assert.False(problem.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task MethodNotAllowedUsesStandardProblemDetailsContract()
    {
        await using var app = await StartTestApplication(application =>
            application.MapVersionOneApi().MapGet("/probe", () => TypedResults.Ok()));
        using var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/api/v1/probe",
            content: null,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(ApiProblemCodes.MethodNotAllowed, await ReadProblemCode(response));
    }

    [Fact]
    public async Task MalformedJsonUsesInvalidRequestProblemCode()
    {
        await using var app = await StartTestApplication(application =>
            application.MapPost(
                "/api/v1/validation-probe",
                (ValidationProbeRequest request) => TypedResults.Ok(request)));
        using var client = app.GetTestClient();
        using var content = new StringContent("{ invalid-json", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            "/api/v1/validation-probe",
            content,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApiProblemCodes.InvalidRequest, await ReadProblemCode(response));
    }

    [Fact]
    public void PaginationQueryIsBoundedAndHasStableDefaults()
    {
        var defaultQuery = new PaginationQuery();
        var invalidQuery = new PaginationQuery
        {
            Page = 0,
            PageSize = PaginationQuery.MaximumPageSize + 1,
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            invalidQuery,
            new ValidationContext(invalidQuery),
            validationResults,
            validateAllProperties: true);

        Assert.Equal(PaginationQuery.DefaultPage, defaultQuery.Page);
        Assert.Equal(PaginationQuery.DefaultPageSize, defaultQuery.PageSize);
        Assert.False(isValid);
        Assert.Equal(2, validationResults.Count);
    }

    [Fact]
    public void PagedResponseCalculatesTotalPagesWithoutOverflowProneAddition()
    {
        var response = new PagedResponse<int>([1, 2], page: 2, pageSize: 25, totalCount: 51);
        var emptyResponse = new PagedResponse<int>([], page: 1, pageSize: 25, totalCount: 0);

        Assert.Equal(3, response.TotalPages);
        Assert.Equal(0, emptyResponse.TotalPages);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResponse<int>([], page: 1, pageSize: 101, totalCount: 0));
    }

    private static async Task<WebApplication> StartTestApplication(Action<WebApplication> mapEndpoints)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(ApiConventionsTests).Assembly.FullName,
            EnvironmentName = "Testing",
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddApiConventions();
        builder.Services.AddValidation();

        var app = builder.Build();
        app.UseApiConventions();
        mapEndpoints(app);
        await app.StartAsync(CancellationToken.None);

        return app;
    }

    private static async Task<string?> ReadProblemCode(HttpResponseMessage response)
    {
        using var problem = await ReadProblem(response);
        return problem.RootElement.GetProperty("code").GetString();
    }

    private static async Task<JsonDocument> ReadProblem(HttpResponseMessage response)
    {
        await using var responseStream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        return await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: CancellationToken.None);
    }
}

public sealed record ValidationProbeRequest(
    [property: Required]
    [property: StringLength(20, MinimumLength = 2)]
    string? Name);
