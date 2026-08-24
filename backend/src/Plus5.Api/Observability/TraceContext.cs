using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Plus5.Api.Observability;

internal static class TraceContext
{
    internal static string GetTraceId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    }
}
