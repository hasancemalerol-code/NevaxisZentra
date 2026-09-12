namespace NevaxisFactOS.Persistence.Erp.Mikro;

public static class MikroTransportErrorCodes
{
    public const string TransportUnreachable = "mikro.transport.unreachable";
    public const string AuthenticationFailed = "mikro.authentication.failed";
    public const string ContextInvalid = "mikro.context.invalid";
    public const string CompanyNotFound = "mikro.company.not-found";
    public const string PermissionDenied = "mikro.permission.denied";
    public const string Timeout = "mikro.timeout";
    public const string SourceReadFailed = "mikro.source.read-failed";
    public const string SchemaUnsupported = "mikro.schema.unsupported";
    public const string VersionUnsupported = "mikro.version.unsupported";
    public const string TransportUnconfigured = "mikro.transport.unconfigured";
}

public sealed class MikroTransportException : Exception
{
    public MikroTransportException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
