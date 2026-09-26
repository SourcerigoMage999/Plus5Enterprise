using Plus5.Api.Conventions;
using Plus5.Api.Identity;
using Plus5.Application.Readiness;

namespace Plus5.Api.Readiness;

public static class StudentReadinessEndpoints
{
    public static IEndpointRouteBuilder MapStudentReadiness(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var students = endpoints.MapVersionOneApi()
            .MapGroup("/students")
            .RequireAuthorization(IdentityServiceExtensions.TeacherPolicy);

        students.MapGet("/{studentId:guid}/readiness", GetAsync);
        students.MapGet("/{studentId:guid}/knowledge", GetKnowledgeAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid studentId,
        HttpContext context,
        IStudentReadinessQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (studentId == Guid.Empty)
        {
            return TypedResults.NotFound();
        }

        var snapshot = await query.GetAsync(teacherAccountId, studentId, cancellationToken);
        return snapshot is null ? TypedResults.NotFound() : TypedResults.Ok(snapshot);
    }

    private static async Task<IResult> GetKnowledgeAsync(
        Guid studentId,
        HttpContext context,
        IStudentKnowledgeDetailQuery query,
        CancellationToken cancellationToken)
    {
        if (!IdentityClaims.TryRead(context.User, out var teacherAccountId, out _))
        {
            return TypedResults.Unauthorized();
        }

        if (studentId == Guid.Empty)
        {
            return TypedResults.NotFound();
        }

        var snapshot = await query.GetAsync(teacherAccountId, studentId, cancellationToken);
        return snapshot is null ? TypedResults.NotFound() : TypedResults.Ok(snapshot);
    }
}
