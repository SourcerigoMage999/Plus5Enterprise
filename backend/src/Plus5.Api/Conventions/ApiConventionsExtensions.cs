using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Plus5.Api.Conventions;

public static class ApiConventionsExtensions
{
    public static IServiceCollection AddApiConventions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ApiProblemDetailsDefaults.Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddValidation();

        return services;
    }

    public static IApplicationBuilder UseApiConventions(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }

    public static RouteGroupBuilder MapVersionOneApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints
            .MapGroup(ApiRoutes.VersionOnePrefix)
            .WithGroupName(ApiRoutes.VersionOneGroupName);
    }
}
