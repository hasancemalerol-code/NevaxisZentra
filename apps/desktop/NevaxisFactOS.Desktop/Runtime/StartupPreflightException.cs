namespace NevaxisFactOS.Desktop.Runtime;

public sealed class StartupPreflightException : Exception
{
    public StartupPreflightException(string errorCode, string message, IReadOnlyList<string> checks)
        : base(message)
    {
        ErrorCode = errorCode;
        Checks = checks;
    }

    public string ErrorCode { get; }

    public IReadOnlyList<string> Checks { get; }
}
