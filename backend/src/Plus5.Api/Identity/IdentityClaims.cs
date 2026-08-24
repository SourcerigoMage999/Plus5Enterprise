using System.Security.Claims;

namespace Plus5.Api.Identity;

internal static class IdentityClaims
{
    internal const string AccountType = "plus5_account_type";
    internal const string SessionId = "plus5_session_id";
    internal const string TeacherAccountType = "Teacher";

    internal static bool TryRead(ClaimsPrincipal principal, out Guid accountId, out Guid sessionId)
    {
        accountId = Guid.Empty;
        sessionId = Guid.Empty;
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out accountId)
            && Guid.TryParse(principal.FindFirstValue(SessionId), out sessionId);
    }
}
