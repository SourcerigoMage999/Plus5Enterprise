namespace Plus5.Api.Conventions;

public static class ApiProblemCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string InvalidRequest = "invalid_request";
    public const string AuthenticationRequired = "authentication_required";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string MethodNotAllowed = "method_not_allowed";
    public const string Conflict = "conflict";
    public const string PayloadTooLarge = "payload_too_large";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string TooManyRequests = "too_many_requests";
    public const string InternalError = "internal_error";
    public const string ServiceUnavailable = "service_unavailable";
    public const string ServerError = "server_error";
    public const string RequestFailed = "request_failed";
}
