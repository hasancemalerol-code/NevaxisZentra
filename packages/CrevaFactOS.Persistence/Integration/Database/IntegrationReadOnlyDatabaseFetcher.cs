using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Integration.Common;
using CrevaFactOS.Persistence.Integration.FileImport;
using Npgsql;

namespace CrevaFactOS.Persistence.Integration.Database;

public sealed record IntegrationDatabaseFetchResult(
    bool Success,
    IntegrationFileTable? Table,
    string? CheckpointJson,
    string? FailureCategory,
    string? SafeMessage);

public interface IIntegrationReadOnlyDatabaseFetcher
{
    Task<IntegrationDatabaseFetchResult> FetchAsync(
        IntegrationConnector connector,
        string? checkpointJson,
        CancellationToken cancellationToken = default);
}

public sealed partial class IntegrationReadOnlyDatabaseFetcher : IIntegrationReadOnlyDatabaseFetcher
{
    private static readonly Regex ForbiddenSqlPattern = ForbiddenSqlRegex();

    public async Task<IntegrationDatabaseFetchResult> FetchAsync(
        IntegrationConnector connector,
        string? checkpointJson,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationReadOnlyDatabaseConnectorConfiguration.TryParse(connector.ConfigurationJson);
        if (config is null || string.IsNullOrWhiteSpace(config.AllowlistedSourceName))
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "Database connector configuration is invalid.");
        }

        if (!string.Equals(config.Engine, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "Only PostgreSQL read-only database connector is enabled in Wave C.");
        }

        if (!IsSafeIdentifier(config.AllowlistedSourceName))
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "Allowlisted source name is invalid.");
        }

        var connectionString = connector.EndpointOrLocation;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "Database connection reference is required.");
        }

        var sql = BuildSelectSql(config, checkpointJson);
        if (ForbiddenSqlPattern.IsMatch(sql))
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "Generated SQL failed read-only safety validation.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection)
            {
                CommandTimeout = Math.Clamp(config.CommandTimeoutSeconds, 5, 120),
            };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var headers = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();
            var rows = new List<IReadOnlyDictionary<string, string>>();
            string? maxCheckpoint = null;

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    row[name] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(config.CheckpointColumn)
                        && string.Equals(name, config.CheckpointColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        maxCheckpoint = row[name];
                    }
                }

                rows.Add(row);
            }

            var table = new IntegrationFileTable(IntegrationFileKinds.Json, headers, rows);
            var nextCheckpoint = maxCheckpoint is null
                ? checkpointJson
                : $"{{\"cursor\":\"{maxCheckpoint}\"}}";

            return new IntegrationDatabaseFetchResult(true, table, nextCheckpoint, null, null);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            return new IntegrationDatabaseFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.SourceUnavailable,
                "Database read failed.");
        }
    }

    internal static string BuildSelectSql(
        IntegrationReadOnlyDatabaseConnectorConfiguration config,
        string? checkpointJson)
    {
        var batchSize = Math.Clamp(config.BatchSize, 1, 5000);
        var source = config.AllowlistedSourceName.Trim();
        if (!string.IsNullOrWhiteSpace(config.CheckpointColumn)
            && IsSafeIdentifier(config.CheckpointColumn))
        {
            var cursor = ReadCursor(checkpointJson);
            return string.IsNullOrWhiteSpace(cursor)
                ? $"SELECT * FROM {source} ORDER BY {config.CheckpointColumn} ASC LIMIT {batchSize}"
                : $"SELECT * FROM {source} WHERE {config.CheckpointColumn} > '{cursor.Replace("'", "''")}' ORDER BY {config.CheckpointColumn} ASC LIMIT {batchSize}";
        }

        return $"SELECT * FROM {source} LIMIT {batchSize}";
    }

    internal static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '.')
        && !value.Contains("..", StringComparison.Ordinal);

    private static string? ReadCursor(string? checkpointJson)
    {
        if (string.IsNullOrWhiteSpace(checkpointJson))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(checkpointJson);
            return document.RootElement.TryGetProperty("cursor", out var cursor)
                ? cursor.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"\b(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|GRANT|REVOKE)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenSqlRegex();
}
