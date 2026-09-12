namespace NevaxisFactOS.Persistence.Erp.Logo;

/// <summary>
/// Safe Logo context discovery result. Never includes secrets or raw credentials.
/// </summary>
public sealed record LogoContextInfo(
    string? ProductFamily,
    string? ProductVersion,
    string? CompanyId,
    string? CompanyCode,
    string? CompanyName,
    int? Period,
    string TransportKind,
    bool ReadOnlyMode,
    IReadOnlyList<string> SupportedCapabilities);

public static class LogoReadinessStates
{
    public const string Configured = "configured";
    public const string TransportUnavailable = "transport-unavailable";
    public const string CredentialsRequired = "credentials-required";
    public const string Reachable = "reachable";
    public const string Validated = "validated";
}
