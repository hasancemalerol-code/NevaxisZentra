using System.Text.Json;

namespace NevaxisZentra.Persistence.Audit;

public static class AuditMetadataSanitizer
{
    public const int MaximumKeyCount = 20;
    public const int MaximumKeyLength = 64;
    public const int MaximumStringLength = 256;
    public const int MaximumArrayLength = 10;
    public const int MaximumSerializedBytes = 4096;
    public const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitiveKeyFragments =
    [
        "password", "token", "secret", "authorization", "cookie", "connectionstring", "apikey",
    ];

    public static bool TrySanitize(
        IReadOnlyDictionary<string, object?>? metadata,
        out string json,
        out string? failureCode)
    {
        var safe = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        if (metadata is not null)
        {
            if (metadata.Count > MaximumKeyCount)
            {
                return Fail("audit.metadata.too_many_keys", out json, out failureCode);
            }

            foreach (var item in metadata)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > MaximumKeyLength)
                {
                    return Fail("audit.metadata.invalid_key", out json, out failureCode);
                }

                if (IsSensitive(item.Key))
                {
                    safe[item.Key] = RedactedValue;
                    continue;
                }

                if (!TrySanitizeValue(item.Value, out var value))
                {
                    return Fail("audit.metadata.invalid_value", out json, out failureCode);
                }

                safe[item.Key] = value;
            }
        }

        json = JsonSerializer.Serialize(safe);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumSerializedBytes)
        {
            return Fail("audit.metadata.too_large", out json, out failureCode);
        }

        failureCode = null;
        return true;
    }

    private static bool TrySanitizeValue(object? value, out object? safe)
    {
        if (value is null or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or Guid)
        {
            safe = value;
            return true;
        }

        if (value is string text && text.Length <= MaximumStringLength)
        {
            safe = text;
            return true;
        }

        if (value is IEnumerable<object?> values)
        {
            var items = values.Take(MaximumArrayLength + 1).ToArray();
            if (items.Length > MaximumArrayLength)
            {
                safe = null;
                return false;
            }

            var sanitized = new object?[items.Length];
            for (var index = 0; index < items.Length; index++)
            {
                if (items[index] is IEnumerable<object?>
                    || !TrySanitizeValue(items[index], out sanitized[index]))
                {
                    safe = null;
                    return false;
                }
            }

            safe = sanitized;
            return true;
        }

        safe = null;
        return false;
    }

    private static bool IsSensitive(string key)
    {
        var normalized = new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return SensitiveKeyFragments.Any(normalized.Contains);
    }

    private static bool Fail(string code, out string json, out string? failureCode)
    {
        json = "{}";
        failureCode = code;
        return false;
    }
}
