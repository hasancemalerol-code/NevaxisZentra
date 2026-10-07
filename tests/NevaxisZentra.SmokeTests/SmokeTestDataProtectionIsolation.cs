using System.Runtime.CompilerServices;

namespace NevaxisZentra.SmokeTests;

/// <summary>
/// Production-mode API hosts now persist a machine Data Protection key ring. Point every smoke-test
/// host at a throw-away directory so tests never create or read keys in the developer profile.
/// </summary>
internal static class SmokeTestDataProtectionIsolation
{
    private const string Variable = "NEVAXIS_ZENTRA_DATA_PROTECTION_PATH";

#pragma warning disable CA2255 // Test assembly: process-wide isolation must be in place before any host is built.
    [ModuleInitializer]
    internal static void Isolate()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
        {
            Environment.SetEnvironmentVariable(
                Variable,
                Path.Combine(Path.GetTempPath(), $"zentra-smoke-dp-{Guid.NewGuid():N}"));
        }
    }
#pragma warning restore CA2255
}
