namespace CrevaFactOS.Persistence.Erp.Netsis;

public static class NetsisTransportErrorCodes
{
    public const string TransportUnreachable = "netsis.transport.unreachable";
    public const string AuthenticationFailed = "netsis.authentication.failed";
    public const string ContextInvalid = "netsis.context.invalid";
    public const string CompanyNotFound = "netsis.company.not-found";
    public const string PermissionDenied = "netsis.permission.denied";
    public const string Timeout = "netsis.timeout";
    public const string SourceReadFailed = "netsis.source.read-failed";
    public const string SchemaUnsupported = "netsis.schema.unsupported";
    public const string VersionUnsupported = "netsis.version.unsupported";
    public const string TransportUnconfigured = "netsis.transport.unconfigured";
}

public sealed class NetsisTransportException : Exception
{
    public NetsisTransportException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
