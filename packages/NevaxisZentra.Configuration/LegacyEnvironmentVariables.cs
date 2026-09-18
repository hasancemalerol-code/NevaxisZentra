namespace NevaxisZentra.Configuration;

public static class LegacyEnvironmentVariables
{
    /// <summary>
    /// Reads a prefixed environment variable using NEVAXIS_ZENTRA as canonical,
    /// then NEVAXIS_FACTOS and CREVAFACTOS as backward-compatible fallbacks.
    /// </summary>
    public static string? GetPrefixed(string suffix) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_ZENTRA_{suffix}")
        ?? Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS_{suffix}")
        ?? Environment.GetEnvironmentVariable($"CREVAFACTOS_{suffix}");

    /// <summary>
    /// Reads a double-underscore sectioned environment variable using NEVAXIS_ZENTRA
    /// as canonical, then NEVAXIS_FACTOS and CREVAFACTOS as backward-compatible fallbacks.
    /// </summary>
    public static string? GetSectioned(string section, string suffix) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_ZENTRA__{section}__{suffix}")
        ?? Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__{section}__{suffix}")
        ?? Environment.GetEnvironmentVariable($"CREVAFACTOS__{section}__{suffix}");
}
