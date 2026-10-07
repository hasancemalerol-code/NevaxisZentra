using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Hosting;

namespace NevaxisZentra.Persistence.DataProtection;

public sealed record DataProtectionKeyRingSettings(string Directory);

/// <summary>
/// Runs at host start: creates the first key on a new installation, and stops startup with an
/// actionable message (never key material) when the ring is unreadable.
/// </summary>
internal sealed class DataProtectionKeyRingInitializer(
    IKeyManager keyManager,
    DataProtectionKeyRingSettings settings) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        DataProtectionKeyRing.EnsureInitialized(keyManager, settings.Directory);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
