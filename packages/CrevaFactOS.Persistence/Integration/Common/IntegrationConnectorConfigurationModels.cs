using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrevaFactOS.Persistence.Integration.Common;

public sealed class IntegrationRestConnectorConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public string RecordsJsonPath { get; set; } = "data";
    public string? NextCursorJsonPath { get; set; }
    public string? CursorQueryParameter { get; set; }
    public string? PageSizeQueryParameter { get; set; }
    public int PageSize { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxResponseBytes { get; set; } = 5 * 1024 * 1024;
    public int MaxPagesPerSync { get; set; } = 25;
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static IntegrationRestConnectorConfiguration? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IntegrationRestConnectorConfiguration>(
                json,
                IntegrationConnectorConfigurationJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed class IntegrationWebhookConnectorConfiguration
{
    public string IngressKey { get; set; } = string.Empty;
    public string SignatureHeader { get; set; } = "X-Integration-Signature";
    public string TimestampHeader { get; set; } = "X-Integration-Timestamp";
    public string EventIdHeader { get; set; } = "X-Integration-Event-Id";
    public int ReplayWindowSeconds { get; set; } = 300;
    public int MaxPayloadBytes { get; set; } = 1024 * 1024;
    public string PayloadRecordsJsonPath { get; set; } = "records";

    public static IntegrationWebhookConnectorConfiguration? TryParse(string? json) =>
        IntegrationConnectorConfigurationJson.TryParse<IntegrationWebhookConnectorConfiguration>(json);
}

public sealed class IntegrationReadOnlyDatabaseConnectorConfiguration
{
    public string Engine { get; set; } = "postgresql";
    public string AllowlistedSourceKind { get; set; } = "table";
    public string AllowlistedSourceName { get; set; } = string.Empty;
    public string? CheckpointColumn { get; set; }
    public int BatchSize { get; set; } = 500;
    public int CommandTimeoutSeconds { get; set; } = 30;

    public static IntegrationReadOnlyDatabaseConnectorConfiguration? TryParse(string? json) =>
        IntegrationConnectorConfigurationJson.TryParse<IntegrationReadOnlyDatabaseConnectorConfiguration>(json);
}

public sealed class IntegrationFolderWatchConnectorConfiguration
{
    public string RootPath { get; set; } = string.Empty;
    public string FilePattern { get; set; } = "*.csv";
    public int StableFileSeconds { get; set; } = 5;
    public string ArchiveSubfolder { get; set; } = "processed";
    public string FailureSubfolder { get; set; } = "failed";

    public static IntegrationFolderWatchConnectorConfiguration? TryParse(string? json) =>
        IntegrationConnectorConfigurationJson.TryParse<IntegrationFolderWatchConnectorConfiguration>(json);
}

public sealed class IntegrationErpAdapterBridgeConfiguration
{
    public string ProviderKey { get; set; } = string.Empty;
    public string Capability { get; set; } = "products.read";
    public Guid? ErpConnectionProfileId { get; set; }

    public static IntegrationErpAdapterBridgeConfiguration? TryParse(string? json) =>
        IntegrationConnectorConfigurationJson.TryParse<IntegrationErpAdapterBridgeConfiguration>(json);
}

public static class IntegrationConnectorConfigurationJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static T? TryParse<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
