using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NevaxisFactOS.Persistence.Outbox;

public static partial class OutboxPayloadValidator
{
    public const int MaximumPayloadBytes = 4_096;
    public const int MaximumHeadersBytes = 2_048;
    public const int MaximumKeyCount = 20;
    public const int MaximumValueLength = 300;

    private static readonly string[] SensitiveFragments =
        ["authorization", "cookie", "token", "secret", "password", "api-key", "apikey", "connection", "prompt", "completion", "reasoning"];

    public static bool TrySerialize(
        OutboxWriteRequest request,
        Guid correlationId,
        out string payloadJson,
        out string headersJson,
        out string? failureCode)
    {
        payloadJson = "{}";
        headersJson = "{}";
        failureCode = ValidateCore(request, correlationId);
        if (failureCode is not null)
        {
            return false;
        }

        payloadJson = JsonSerializer.Serialize(request.Payload);
        headersJson = JsonSerializer.Serialize(request.Headers);
        if (Encoding.UTF8.GetByteCount(payloadJson) > MaximumPayloadBytes)
        {
            failureCode = "outbox.payload.too_large";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(headersJson) > MaximumHeadersBytes)
        {
            failureCode = "outbox.headers.too_large";
            return false;
        }

        failureCode = null;
        return true;
    }

    private static string? ValidateCore(OutboxWriteRequest request, Guid correlationId)
    {
        if (!OutboxMessageTypes.All.Contains(request.MessageType)
            || request.SchemaVersion != OutboxMessageTypes.SchemaVersion)
        {
            return "outbox.contract.unsupported";
        }

        if (!IsBoundedKey(request.IdempotencyKey, 200)
            || correlationId == Guid.Empty)
        {
            return "outbox.identity.invalid";
        }

        if (request.OccurredAtUtc.Offset != TimeSpan.Zero
            || request.AvailableAtUtc.Offset != TimeSpan.Zero
            || request.AvailableAtUtc < request.OccurredAtUtc)
        {
            return "outbox.timestamp.invalid";
        }

        if ((request.AggregateType is null) != (request.AggregateId is null)
            || request.AggregateType is { } aggregateType && !IsStableKey(aggregateType, 100)
            || request.AggregateId is { } aggregateId && !IsBoundedKey(aggregateId, 200))
        {
            return "outbox.aggregate.invalid";
        }

        if (!ValidateMap(request.Payload) || !ValidateMap(request.Headers))
        {
            return "outbox.metadata.invalid";
        }

        return null;
    }

    private static bool ValidateMap(IReadOnlyDictionary<string, string?> values) =>
        values.Count <= MaximumKeyCount
        && values.All(pair =>
            IsStableKey(pair.Key, 100)
            && pair.Value?.Length <= MaximumValueLength
            && !SensitiveFragments.Any(fragment => pair.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)));

    private static bool IsBoundedKey(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && IdentifierPattern().IsMatch(value);

    private static bool IsStableKey(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && StableKeyPattern().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    [GeneratedRegex("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableKeyPattern();
}
