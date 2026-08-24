using System.Diagnostics;
using OpenTelemetry;

namespace Plus5.Api.Observability;

internal sealed class SensitiveTelemetrySanitizerProcessor : BaseProcessor<Activity>
{
    private static readonly string[] SensitiveTagNames =
    [
        "http.target",
        "http.url",
        "http.user_agent",
        "url.full",
        "url.path",
        "url.query",
        "user_agent.original",
    ];

    public override void OnEnd(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        foreach (var tagName in SensitiveTagNames)
        {
            activity.SetTag(tagName, value: null);
        }
    }
}
