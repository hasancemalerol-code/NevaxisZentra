using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CrevaFactOS.Persistence.Integration.Common;
using CrevaFactOS.Persistence.Integration.FileImport;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.Persistence.Integration.Rest;

public sealed record IntegrationRestFetchResult(
    bool Success,
    IntegrationFileTable? Table,
    string? CheckpointJson,
    string? FailureCategory,
    string? SafeMessage,
    int PagesFetched = 0);

public interface IIntegrationRestRecordFetcher
{
    Task<IntegrationRestFetchResult> FetchAsync(
        IntegrationConnector connector,
        string? checkpointJson,
        string? bearerToken,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationRestRecordFetcher(
    IHttpClientFactory httpClientFactory,
    IHostEnvironment hostEnvironment) : IIntegrationRestRecordFetcher
{
    public async Task<IntegrationRestFetchResult> FetchAsync(
        IntegrationConnector connector,
        string? checkpointJson,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationRestConnectorConfiguration.TryParse(connector.ConfigurationJson);
        if (config is null || string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return new IntegrationRestFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "REST connector configuration is invalid.");
        }

        if (!Uri.TryCreate(config.BaseUrl.TrimEnd('/') + config.Path, UriKind.Absolute, out var requestUri))
        {
            return new IntegrationRestFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                "REST base URL is invalid.");
        }

        var validation = IntegrationRestSsrfGuard.ValidateRequestUri(
            requestUri,
            allowLocalhostInDevelopment: true,
            isDevelopmentEnvironment: hostEnvironment.IsDevelopment());
        if (!validation.Allowed)
        {
            return new IntegrationRestFetchResult(
                false,
                null,
                checkpointJson,
                IntegrationFailureCategories.Configuration,
                validation.SafeMessage ?? "REST destination blocked by SSRF guard.");
        }

        var cursor = ReadCursor(checkpointJson);
        var headers = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, string>>();
        var pagesFetched = 0;
        string? nextCursor = cursor;

        using var client = httpClientFactory.CreateClient("integration-rest");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 5, 120));

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        foreach (var (key, value) in config.Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
        }

        do
        {
            var pageUri = BuildPageUri(validation.NormalizedUri!, config, nextCursor);
            var pageValidation = IntegrationRestSsrfGuard.ValidateRequestUri(
                pageUri,
                allowLocalhostInDevelopment: true,
                isDevelopmentEnvironment: hostEnvironment.IsDevelopment());
            if (!pageValidation.Allowed)
            {
                return new IntegrationRestFetchResult(
                    false,
                    null,
                    checkpointJson,
                    IntegrationFailureCategories.Configuration,
                    pageValidation.SafeMessage);
            }

            using var response = await client.GetAsync(pageValidation.NormalizedUri, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new IntegrationRestFetchResult(
                    false,
                    null,
                    checkpointJson,
                    IntegrationFailureCategories.RateLimit,
                    "REST source returned rate limit.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new IntegrationRestFetchResult(
                    false,
                    null,
                    checkpointJson,
                    IntegrationFailureCategories.SourceUnavailable,
                    $"REST source returned {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            var pageRecords = ExtractRecords(document.RootElement, config.RecordsJsonPath);
            if (pageRecords.Count == 0)
            {
                break;
            }

            foreach (var record in pageRecords)
            {
                var row = FlattenRecord(record);
                if (headers.Count == 0)
                {
                    headers.AddRange(row.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
                }

                rows.Add(row);
            }

            pagesFetched++;
            nextCursor = ExtractNextCursor(document.RootElement, config);
            checkpointJson = WriteCursor(nextCursor);
        }
        while (!string.IsNullOrWhiteSpace(nextCursor) && pagesFetched < Math.Clamp(config.MaxPagesPerSync, 1, 100));

        if (headers.Count == 0)
        {
            return new IntegrationRestFetchResult(
                true,
                new IntegrationFileTable(IntegrationFileKinds.Json, [], []),
                checkpointJson,
                null,
                null,
                pagesFetched);
        }

        var table = new IntegrationFileTable(
            IntegrationFileKinds.Json,
            headers,
            rows);

        return new IntegrationRestFetchResult(true, table, checkpointJson, null, null, pagesFetched);
    }

    private static Uri BuildPageUri(Uri baseUri, IntegrationRestConnectorConfiguration config, string? cursor)
    {
        var query = QueryHelpers.ParseQuery(baseUri.Query)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (string?)kvp.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(config.PageSizeQueryParameter))
        {
            query[config.PageSizeQueryParameter] = Math.Clamp(config.PageSize, 1, 1000)
                .ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(config.CursorQueryParameter) && !string.IsNullOrWhiteSpace(cursor))
        {
            query[config.CursorQueryParameter] = cursor;
        }

        var path = baseUri.GetLeftPart(UriPartial.Path);
        return query.Count == 0
            ? baseUri
            : new Uri(QueryHelpers.AddQueryString(path, query));
    }

    private static List<JsonElement> ExtractRecords(JsonElement root, string recordsPath)
    {
        var current = root;
        foreach (var segment in recordsPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return [];
            }
        }

        if (current.ValueKind == JsonValueKind.Array)
        {
            return current.EnumerateArray().ToList();
        }

        return current.ValueKind == JsonValueKind.Object ? [current] : [];
    }

    private static string? ExtractNextCursor(JsonElement root, IntegrationRestConnectorConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.NextCursorJsonPath))
        {
            return null;
        }

        var current = root;
        foreach (var segment in config.NextCursorJsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            _ => null,
        };
    }

    private static Dictionary<string, string> FlattenRecord(JsonElement record)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (record.ValueKind != JsonValueKind.Object)
        {
            return row;
        }

        foreach (var property in record.EnumerateObject())
        {
            row[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText(),
            };
        }

        return row;
    }

    private static string? ReadCursor(string? checkpointJson)
    {
        if (string.IsNullOrWhiteSpace(checkpointJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(checkpointJson);
            return document.RootElement.TryGetProperty("cursor", out var cursor)
                ? cursor.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string WriteCursor(string? cursor) =>
        JsonSerializer.Serialize(new { cursor });
}
