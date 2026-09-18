using System.Security.Cryptography;
using System.Text;

namespace NevaxisZentra.Persistence.Integration.Mapping;

public static class IntegrationSchemaFingerprint
{
    public static string Compute(IReadOnlyList<string> headers)
    {
        var normalized = headers
            .Select(h => h.Trim().ToLowerInvariant())
            .OrderBy(h => h, StringComparer.Ordinal)
            .ToArray();
        var payload = string.Join("|", normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static IntegrationSchemaComparison Compare(
        string activeFingerprint,
        IReadOnlyList<string> currentHeaders)
    {
        var currentFingerprint = Compute(currentHeaders);
        if (string.Equals(activeFingerprint, currentFingerprint, StringComparison.Ordinal))
        {
            return new IntegrationSchemaComparison(true, currentFingerprint, [], [], []);
        }

        var activeSet = new HashSet<string>(
            activeFingerprint == string.Empty
                ? []
                : [], StringComparer.OrdinalIgnoreCase);

        // Reconstruct active headers isn't possible from fingerprint alone;
        // compare using stored header list in mapping metadata when available.
        var currentSet = currentHeaders.Select(h => h.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new IntegrationSchemaComparison(
            false,
            currentFingerprint,
            [],
            [],
            currentSet.OrderBy(h => h, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public static IntegrationSchemaComparison CompareHeaders(
        IReadOnlyList<string> expectedHeaders,
        IReadOnlyList<string> currentHeaders)
    {
        var expected = expectedHeaders
            .Select(h => h.Trim())
            .Where(h => h.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var current = currentHeaders
            .Select(h => h.Trim())
            .Where(h => h.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = current.Except(expected, StringComparer.OrdinalIgnoreCase).OrderBy(h => h).ToList();
        var removed = expected.Except(current, StringComparer.OrdinalIgnoreCase).OrderBy(h => h).ToList();
        var compatible = removed.Count == 0;

        return new IntegrationSchemaComparison(
            compatible,
            Compute(currentHeaders),
            added,
            removed,
            current.OrderBy(h => h, StringComparer.OrdinalIgnoreCase).ToList());
    }
}

public sealed record IntegrationSchemaComparison(
    bool IsCompatible,
    string CurrentFingerprint,
    IReadOnlyList<string> AddedFields,
    IReadOnlyList<string> RemovedFields,
    IReadOnlyList<string> CurrentFields);
