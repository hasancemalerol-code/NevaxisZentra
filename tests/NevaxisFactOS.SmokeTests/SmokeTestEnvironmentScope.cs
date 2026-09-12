namespace NevaxisFactOS.SmokeTests;

internal static class SmokeTestEnvironmentScope
{
    private static readonly string[] EnvironmentKeys =
    [
        "NEVAXIS_FACTOS_POSTGRES_HOST",
        "NEVAXIS_FACTOS_POSTGRES_PORT",
        "NEVAXIS_FACTOS_POSTGRES_DATABASE",
        "NEVAXIS_FACTOS_POSTGRES_USERNAME",
        "NEVAXIS_FACTOS_POSTGRES_PASSWORD",
        "NEVAXIS_FACTOS_RABBITMQ_HOST",
        "NEVAXIS_FACTOS_RABBITMQ_AMQP_PORT",
        "NEVAXIS_FACTOS_RABBITMQ_MANAGEMENT_PORT",
        "NEVAXIS_FACTOS_RABBITMQ_USERNAME",
        "NEVAXIS_FACTOS_RABBITMQ_PASSWORD",
        "NEVAXIS_FACTOS__AUTHENTICATION__MODE",
        "NEVAXIS_FACTOS__PERSISTENCE__ENABLED",
        "NEVAXIS_FACTOS__PERSISTENCE__HOST",
        "NEVAXIS_FACTOS__PERSISTENCE__PORT",
        "NEVAXIS_FACTOS__PERSISTENCE__DATABASE",
        "NEVAXIS_FACTOS__PERSISTENCE__USERNAME",
        "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD",
        "NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE",
        "NEVAXIS_FACTOS__PERSISTENCE__CONNECTION_TIMEOUT_SECONDS",
        "NEVAXIS_FACTOS__PERSISTENCE__COMMAND_TIMEOUT_SECONDS",
        "NEVAXIS_FACTOS__PERSISTENCE__POOLING",
        "NEVAXIS_FACTOS__MIGRATIONS__HOST",
        "NEVAXIS_FACTOS__MIGRATIONS__PORT",
        "NEVAXIS_FACTOS__MIGRATIONS__DATABASE",
        "NEVAXIS_FACTOS__MIGRATIONS__USERNAME",
        "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD",
        "ASPNETCORE_ENVIRONMENT",
        "ASPNETCORE_URLS",
        "CREVAFACTOS_POSTGRES_HOST",
        "CREVAFACTOS_POSTGRES_PORT",
        "CREVAFACTOS__AUTHENTICATION__MODE",
    ];

    internal static IDisposable SuppressOverrides()
    {
        var previousValues = EnvironmentKeys
            .ToDictionary(key => key, Environment.GetEnvironmentVariable);

        foreach (var key in EnvironmentKeys)
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
