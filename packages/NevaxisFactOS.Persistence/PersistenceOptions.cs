namespace NevaxisFactOS.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public bool Enabled { get; set; }

    public bool HealthChecksEnabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Database { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string SslMode { get; set; } = "Disable";

    public int ConnectionTimeoutSeconds { get; set; } = 15;

    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool Pooling { get; set; } = true;
}
