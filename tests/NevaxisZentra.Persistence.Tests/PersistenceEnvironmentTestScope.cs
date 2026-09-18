namespace NevaxisZentra.Persistence.Tests;

internal static class PersistenceEnvironmentTestScope
{
    private static readonly string[] PersistenceEnvironmentKeys =
    [
        "NEVAXIS_FACTOS__PERSISTENCE__ENABLED",
        "NEVAXIS_FACTOS__PERSISTENCE__HEALTH_CHECKS_ENABLED",
        "NEVAXIS_FACTOS__PERSISTENCE__HOST",
        "NEVAXIS_FACTOS__PERSISTENCE__PORT",
        "NEVAXIS_FACTOS__PERSISTENCE__DATABASE",
        "NEVAXIS_FACTOS__PERSISTENCE__USERNAME",
        "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD",
        "NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE",
        "NEVAXIS_FACTOS__PERSISTENCE__CONNECTION_TIMEOUT_SECONDS",
        "NEVAXIS_FACTOS__PERSISTENCE__COMMAND_TIMEOUT_SECONDS",
        "NEVAXIS_FACTOS__PERSISTENCE__POOLING",
        "CREVAFACTOS__PERSISTENCE__ENABLED",
        "CREVAFACTOS__PERSISTENCE__HEALTH_CHECKS_ENABLED",
        "CREVAFACTOS__PERSISTENCE__HOST",
        "CREVAFACTOS__PERSISTENCE__PORT",
        "CREVAFACTOS__PERSISTENCE__DATABASE",
        "CREVAFACTOS__PERSISTENCE__USERNAME",
        "CREVAFACTOS__PERSISTENCE__PASSWORD",
        "CREVAFACTOS__PERSISTENCE__SSL_MODE",
        "CREVAFACTOS__PERSISTENCE__CONNECTION_TIMEOUT_SECONDS",
        "CREVAFACTOS__PERSISTENCE__COMMAND_TIMEOUT_SECONDS",
        "CREVAFACTOS__PERSISTENCE__POOLING",
    ];

    internal static IDisposable SuppressOverrides()
    {
        var previousValues = PersistenceEnvironmentKeys
            .ToDictionary(key => key, Environment.GetEnvironmentVariable);

        foreach (var key in PersistenceEnvironmentKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        return new RestoreScope(previousValues);
    }

    private sealed class RestoreScope(Dictionary<string, string?> previousValues) : IDisposable
    {
        public void Dispose()
        {
            foreach (var (key, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
