using NevaxisZentra.Configuration;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ZentraRenameCompatibilityTests
{
    [Fact]
    public void GetPrefixedUsesZentraBeforeFactosBeforeCreva()
    {
        var suffix = $"RENAME_PROBE_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable($"NEVAXIS_ZENTRA_{suffix}", "zentra");
        Environment.SetEnvironmentVariable($"NEVAXIS_FACTOS_{suffix}", "factos");
        Environment.SetEnvironmentVariable($"CREVAFACTOS_{suffix}", "creva");

        Assert.Equal("zentra", LegacyEnvironmentVariables.GetPrefixed(suffix));

        Environment.SetEnvironmentVariable($"NEVAXIS_ZENTRA_{suffix}", null);
        Assert.Equal("factos", LegacyEnvironmentVariables.GetPrefixed(suffix));

        Environment.SetEnvironmentVariable($"NEVAXIS_FACTOS_{suffix}", null);
        Assert.Equal("creva", LegacyEnvironmentVariables.GetPrefixed(suffix));

        Environment.SetEnvironmentVariable($"CREVAFACTOS_{suffix}", null);
    }
}
