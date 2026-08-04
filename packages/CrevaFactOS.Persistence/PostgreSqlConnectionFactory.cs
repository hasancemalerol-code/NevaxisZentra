using Npgsql;

namespace CrevaFactOS.Persistence;

public static class PostgreSqlConnectionFactory
{
    public static string BuildConnectionString(PersistenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var validation = new PersistenceOptionsValidator().Validate(null, options);
        if (validation.Failed)
        {
            throw new InvalidOperationException(string.Join(" ", validation.Failures));
        }

        var sslMode = Enum.Parse<SslMode>(options.SslMode, ignoreCase: true);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Host,
            Port = options.Port,
            Database = options.Database,
            Username = options.Username,
            Password = options.Password,
            SslMode = sslMode,
            Timeout = options.ConnectionTimeoutSeconds,
            CommandTimeout = options.CommandTimeoutSeconds,
            Pooling = options.Pooling,
        };

        return builder.ConnectionString;
    }

    public static string GetSafeEndpointDescription(PersistenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $"enabled={options.Enabled}; host={options.Host}; port={options.Port}; "
            + $"database={options.Database}; sslMode={options.SslMode}; "
            + $"connectionTimeoutSeconds={options.ConnectionTimeoutSeconds}; "
            + $"commandTimeoutSeconds={options.CommandTimeoutSeconds}; pooling={options.Pooling}";
    }

    public static string RedactConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = "[REDACTED]",
        };

        return builder.ConnectionString;
    }
}
