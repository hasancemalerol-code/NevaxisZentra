using NevaxisZentra.Configuration;
using NevaxisZentra.Persistence;

namespace NevaxisZentra.DatabaseTool;

internal static class DatabaseConfiguration
{
#if ZENTRA_PRODUCTION_BUILD
    // Customer builds carry no default database passwords; credentials must be supplied explicitly.
    private const string DefaultRuntimePassword = "";
    private const string DefaultMigrationPassword = "";
#else
    private const string DefaultRuntimePassword = "local_runtime_only_change_me";
    private const string DefaultMigrationPassword = "local_dev_only_change_me";
#endif

    public static PersistenceOptions LoadRuntime() => Load("PERSISTENCE", "crevafactos_runtime", DefaultRuntimePassword, true);

    public static PersistenceOptions LoadMigrations() => Load("MIGRATIONS", "crevafactos", DefaultMigrationPassword, false);

    private static PersistenceOptions Load(
        string section,
        string defaultUsername,
        string defaultPassword,
        bool defaultPooling) => new()
    {
        Enabled = true,
        Host = GetValue(section, "HOST", "127.0.0.1"),
        Port = GetInt32(section, "PORT", 5432),
        Database = GetValue(section, "DATABASE", "crevafactos_local"),
        Username = GetValue(section, "USERNAME", defaultUsername),
        Password = GetValue(section, "PASSWORD", defaultPassword),
        SslMode = GetValue(section, "SSL_MODE", "Disable"),
        ConnectionTimeoutSeconds = GetInt32(section, "CONNECTION_TIMEOUT_SECONDS", 15),
        CommandTimeoutSeconds = GetInt32(section, "COMMAND_TIMEOUT_SECONDS", 30),
        Pooling = GetBoolean(section, "POOLING", defaultPooling),
    };

    private static string GetValue(string section, string suffix, string fallback) =>
        LegacyEnvironmentVariables.GetSectioned(section, suffix) ?? fallback;

    private static int GetInt32(string section, string suffix, int fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned(section, suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

    private static bool GetBoolean(string section, string suffix, bool fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned(section, suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }
}
