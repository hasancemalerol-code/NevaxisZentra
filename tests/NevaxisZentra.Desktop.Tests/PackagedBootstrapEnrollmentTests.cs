using System.IO;
using NevaxisZentra.Configuration.Bootstrap;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

internal sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now = now.Add(by);
}

public sealed class PackagedBootstrapEnrollmentTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"zentra-desktop-bootstrap-{Guid.NewGuid():N}");
    private readonly ManualClock clock = new(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Func<Task<int?>> Count(int? value) => () => Task.FromResult(value);

    private string TokenOnDisk() =>
        File.ReadAllLines(Path.Combine(directory, BootstrapEnrollmentStore.TokenFileName))
            .Last(line => !string.IsNullOrWhiteSpace(line)).Trim();

    [Fact]
    public async Task NonProductionChannelsNeverIssueTokensOrTouchTheDisk()
    {
        var diagnostics = new List<string>();

        var access = await PackagedBootstrapEnrollment.PrepareAsync(false, directory, Count(0), diagnostics, clock);

        Assert.Equal(BootstrapApiAccess.None, access);
        Assert.False(Directory.Exists(directory));
        Assert.Empty(diagnostics);
        Assert.Empty(access.ApiKeysToRemove);
        Assert.Empty(access.WebKeysToRemove);
    }

    [Fact]
    public async Task FreshProductionInstallIssuesATokenDeliveredOnlyToTheTokenFile()
    {
        var diagnostics = new List<string>();

        var access = await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(0), diagnostics, clock);

        Assert.True(access.IsProduction);
        Assert.True(access.Pending);
        var token = TokenOnDisk();
        Assert.Contains(diagnostics, line => line.StartsWith("bootstrap=token-issued file=", StringComparison.Ordinal));
        Assert.All(diagnostics, line => Assert.DoesNotContain(token, line, StringComparison.Ordinal));
        Assert.DoesNotContain(token, File.ReadAllText(Path.Combine(directory, BootstrapEnrollmentStore.StateFileName)), StringComparison.Ordinal);
        // While pending the API legitimately holds the one-time bootstrap credential.
        Assert.Empty(access.ApiKeysToRemove);
    }

    [Fact]
    public async Task ExistingProductionInstallationIsLeftUnchangedAndClosedForBootstrap()
    {
        var diagnostics = new List<string>();

        var access = await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(3), diagnostics, clock);

        Assert.False(access.Pending);
        Assert.False(File.Exists(Path.Combine(directory, BootstrapEnrollmentStore.TokenFileName)));
        Assert.DoesNotContain(diagnostics, line => line.Contains("token-issued", StringComparison.Ordinal));
        Assert.Equal(BootstrapEnrollmentStage.Completed, new BootstrapEnrollmentStore(directory, clock).Read()!.StageValue);
        // Once closed, the API no longer receives the superuser credential or the bootstrap directory.
        Assert.Contains("NEVAXIS_FACTOS__MIGRATIONS__PASSWORD", access.ApiKeysToRemove);
        Assert.Contains(BootstrapEnrollmentStore.DirectoryEnvironmentVariable, access.ApiKeysToRemove);
    }

    [Fact]
    public async Task UnknownTenantCountKeepsBootstrapClosedAndWritesNothing()
    {
        var diagnostics = new List<string>();

        var access = await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(null), diagnostics, clock);

        Assert.False(access.Pending);
        Assert.False(Directory.Exists(directory));
        Assert.Contains("bootstrap=skipped-tenant-count-unavailable", diagnostics);
    }

    [Fact]
    public async Task ExpiredUnusedTokenIsReplacedOnTheNextStartup()
    {
        await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(0), [], clock);
        var first = TokenOnDisk();
        clock.Advance(BootstrapEnrollmentStore.DefaultTokenLifetime + TimeSpan.FromMinutes(5));
        var diagnostics = new List<string>();

        var access = await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(0), diagnostics, clock);

        Assert.True(access.Pending);
        Assert.Contains(diagnostics, line => line.StartsWith("bootstrap=token-replaced file=", StringComparison.Ordinal));
        var second = TokenOnDisk();
        Assert.NotEqual(first, second);
        var store = new BootstrapEnrollmentStore(directory, clock);
        Assert.Equal(BootstrapEntryResult.InvalidToken, store.TryBegin(first));
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(second));
    }

    [Fact]
    public async Task RestartsAfterCompletionNeverRegenerateTheToken()
    {
        await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(0), [], clock);
        var store = new BootstrapEnrollmentStore(directory, clock);
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(TokenOnDisk()));
        store.Complete("enrolled");

        for (var restart = 0; restart < 3; restart++)
        {
            clock.Advance(TimeSpan.FromDays(60));
            var diagnostics = new List<string>();
            var access = await PackagedBootstrapEnrollment.PrepareAsync(true, directory, Count(restart == 0 ? 1 : 0), diagnostics, clock);

            Assert.False(access.Pending);
            Assert.False(File.Exists(Path.Combine(directory, BootstrapEnrollmentStore.TokenFileName)));
            Assert.DoesNotContain(diagnostics, line => line.Contains("token-", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void WebServerNeverInheritsTheMigrationCredentialInProduction()
    {
        var production = new BootstrapApiAccess(true, true, directory);
        var demo = new BootstrapApiAccess(false, false, null);

        Assert.Equal(BootstrapApiAccess.MigrationCredentialKeys, production.WebKeysToRemove);
        Assert.Empty(demo.WebKeysToRemove);
    }
}

public sealed class RuntimeProcessEnvironmentTests
{
    [Fact]
    public async Task RemovedEnvironmentVariablesAreNotInheritedByTheChildProcess()
    {
        const string key = "ZENTRA_TEST_INHERIT_SECRET";
        var log = Path.Combine(Path.GetTempPath(), $"zentra-env-{Guid.NewGuid():N}.log");
        Environment.SetEnvironmentVariable(key, "should-not-leak");
        try
        {
            using (var kept = RuntimeProcess.StartHidden(
                       "cmd.exe",
                       $"/c if defined {key} (echo PRESENT) else (echo ABSENT)",
                       Path.GetTempPath(),
                       null,
                       log))
            {
                await WaitForExit(kept);
            }

            var removedLog = log + ".removed";
            using (var removed = RuntimeProcess.StartHidden(
                       "cmd.exe",
                       $"/c if defined {key} (echo PRESENT) else (echo ABSENT)",
                       Path.GetTempPath(),
                       null,
                       removedLog,
                       removeEnvironmentKeys: [key]))
            {
                await WaitForExit(removed);
            }

            await Task.Delay(300);
            // The log also records the command line, so compare whole output lines only.
            static string[] OutputLines(string path) =>
                File.ReadAllLines(path).Select(line => line.Trim()).ToArray();
            Assert.Contains("PRESENT", OutputLines(log));
            var removedLines = OutputLines(removedLog);
            Assert.Contains("ABSENT", removedLines);
            Assert.DoesNotContain("PRESENT", removedLines);
            File.Delete(removedLog);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            File.Delete(log);
        }
    }

    private static async Task WaitForExit(RuntimeProcess process)
    {
        for (var i = 0; i < 100 && !process.HasExited; i++)
        {
            await Task.Delay(100);
        }

        Assert.True(process.HasExited);
    }
}
