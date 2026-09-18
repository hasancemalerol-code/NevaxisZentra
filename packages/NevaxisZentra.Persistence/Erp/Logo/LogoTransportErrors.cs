namespace NevaxisZentra.Persistence.Erp.Logo;

public static class LogoTransportErrorCodes
{
    public const string TransportUnreachable = "logo.transport.unreachable";
    public const string AuthenticationFailed = "logo.authentication.failed";
    public const string ContextInvalid = "logo.context.invalid";
    public const string CompanyNotFound = "logo.company.not-found";
    public const string PermissionDenied = "logo.permission.denied";
    public const string Timeout = "logo.timeout";
    public const string SourceReadFailed = "logo.source.read-failed";
    public const string SchemaUnsupported = "logo.schema.unsupported";
    public const string VersionUnsupported = "logo.version.unsupported";
    public const string TransportUnconfigured = "logo.transport.unconfigured";
}

public sealed class LogoTransportException : Exception
{
    public LogoTransportException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
