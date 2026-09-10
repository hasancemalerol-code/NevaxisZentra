using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrevaFactOS.Persistence.Integration.FileImport;

namespace CrevaFactOS.Persistence.Integration.Mapping;

public static class IntegrationContentFingerprint
{
    public static string Compute(IReadOnlyDictionary<string, string?> values)
    {
        var ordered = values
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var json = JsonSerializer.Serialize(ordered);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeFileFingerprint(Stream content)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeTableFingerprint(IntegrationFileTable table)
    {
        var builder = new StringBuilder();
        builder.AppendJoin('|', table.Headers);
        foreach (var row in table.Rows)
        {
            builder.Append('|');
            builder.Append(Compute(row.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value)));
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
