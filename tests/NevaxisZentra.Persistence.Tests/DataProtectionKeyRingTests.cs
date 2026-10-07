using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NevaxisZentra.Persistence.DataProtection;

namespace NevaxisZentra.Persistence.Tests;

internal sealed class TestHostEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;

    public string ApplicationName { get; set; } = "NevaxisZentra.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new Capture(Messages);

    public void Dispose()
    {
    }

    private sealed class Capture(List<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (sink)
            {
                sink.Add(formatter(state, exception) + (exception is null ? string.Empty : " | " + exception));
            }
        }
    }
}

[Collection("DataProtectionEnvironment")]
public sealed class DataProtectionKeyRingTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"zentra-dp-{Guid.NewGuid():N}", "data-protection");

    public void Dispose()
    {
        var root = Path.GetDirectoryName(directory)!;
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private ServiceProvider BuildProvider(CapturingLoggerProvider? logs = null)
    {
        DataProtectionKeyRing.Prepare(directory);
        var services = new ServiceCollection();
        if (logs is not null)
        {
            services.AddLogging(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
        }

        DataProtectionKeyRing.Configure(services.AddDataProtection(), directory);
        return services.BuildServiceProvider();
    }

    private static void Initialize(ServiceProvider provider, string directory) =>
        DataProtectionKeyRing.EnsureInitialized(provider.GetRequiredService<IKeyManager>(), directory);

    private string[] KeyFiles() => Directory.GetFiles(directory, "key-*.xml");

    [Fact]
    public void KeyRingSurvivesProcessRecreationAndProtectedValuesStillDecrypt()
    {
        string protectedValue;
        using (var first = BuildProvider())
        {
            Initialize(first, directory);
            protectedValue = first.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("NevaxisZentra.SaasIdentity.MfaSecret.v1")
                .Protect("JBSWY3DPEHPK3PXP");
        }

        using var second = BuildProvider(); // a new process would build an identical provider
        Initialize(second, directory);
        var restored = second.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("NevaxisZentra.SaasIdentity.MfaSecret.v1")
            .Unprotect(protectedValue);

        Assert.Equal("JBSWY3DPEHPK3PXP", restored);
    }

    [Fact]
    public void NewInstallationCreatesTheKeyRingExactlyOnce()
    {
        using (var first = BuildProvider())
        {
            Initialize(first, directory);
            Initialize(first, directory);
        }

        using (var second = BuildProvider())
        {
            Initialize(second, directory);
        }

        Assert.Single(KeyFiles());
        Assert.True(File.Exists(Path.Combine(directory, DataProtectionKeyRing.StateFileName)));
    }

    [Fact]
    public async Task ConcurrentHostsStartingTogetherCreateTheKeyRingExactlyOnceWithoutErrors()
    {
        DataProtectionKeyRing.Prepare(directory);
        var providers = Enumerable.Range(0, 8).Select(_ => BuildProvider()).ToArray();
        try
        {
            await Task.WhenAll(providers.Select(provider => Task.Run(() => Initialize(provider, directory))));
        }
        finally
        {
            foreach (var provider in providers)
            {
                provider.Dispose();
            }
        }

        Assert.Single(KeyFiles());
        Assert.True(File.Exists(Path.Combine(directory, DataProtectionKeyRing.StateFileName)));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public void ExistingKeyRingIsReusedAndNeverReplaced()
    {
        using var first = BuildProvider();
        Initialize(first, directory);
        var originalFile = Assert.Single(KeyFiles());
        var originalBytes = File.ReadAllBytes(originalFile);

        using var second = BuildProvider();
        Initialize(second, directory);

        Assert.Equal(originalFile, Assert.Single(KeyFiles()));
        Assert.Equal(originalBytes, File.ReadAllBytes(originalFile));
    }

    [Fact]
    public void KeysAreEncryptedAtRestNotStoredAsPlaintext()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var provider = BuildProvider();
        Initialize(provider, directory);

        var text = File.ReadAllText(Assert.Single(KeyFiles()));
        Assert.Contains("encryptedSecret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<masterKey", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingKeysAfterInitializationFailSafelyWithAnActionableMessage()
    {
        using (var provider = BuildProvider())
        {
            Initialize(provider, directory);
        }

        foreach (var file in KeyFiles())
        {
            File.Delete(file);
        }

        var exception = Assert.Throws<DataProtectionKeyRingException>(() => DataProtectionKeyRing.Prepare(directory));

        Assert.Equal("key-ring-missing-keys", exception.Code);
        Assert.Contains(directory, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Restore", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CorruptKeyFileFailsSafelyWithoutLeakingItsContent()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "key-deadbeef.xml"), "SECRET-KEY-MATERIAL-123 <not xml");

        var exception = Assert.Throws<DataProtectionKeyRingException>(() => DataProtectionKeyRing.Prepare(directory));

        Assert.Equal("corrupt-key-file", exception.Code);
        Assert.Contains("key-deadbeef.xml", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET-KEY-MATERIAL-123", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectoryIsProtectedWithOwnerOnlyAcl()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        DataProtectionKeyRing.Prepare(directory);

        Assert.True(DataProtectionKeyRing.IsOwnerOnly(directory));
    }

    [Fact]
    public void KeyRingUnderProgramFilesIsRejected()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (string.IsNullOrEmpty(programFiles))
        {
            return;
        }

        var exception = Assert.Throws<DataProtectionKeyRingException>(() =>
            DataProtectionKeyRing.Prepare(Path.Combine(programFiles, "Nevaxis Zentra", "data-protection")));

        Assert.Equal("key-ring-in-program-files", exception.Code);
    }

    [Fact]
    public void DefaultDirectoryLivesUnderThePerMachineConfigDirectoryNextToTheSecrets()
    {
        var previous = Environment.GetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, null);
            var resolved = DataProtectionKeyRing.ResolveDirectory();

            var expectedTail = Path.Combine("NevaxisZentra", "config", "data-protection");
            Assert.EndsWith(expectedTail, resolved, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                resolved,
                StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Path.GetFullPath(directory), DataProtectionKeyRing.ResolveDirectory(directory));
        }
        finally
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void NoRawKeyMaterialIsEverLogged()
    {
        var logs = new CapturingLoggerProvider();
        using (var provider = BuildProvider(logs))
        {
            Initialize(provider, directory);
            provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("p").Protect("value");
        }

        var keyText = File.ReadAllText(Assert.Single(KeyFiles()));
        var secretFragments = Regex.Matches(keyText, "[A-Za-z0-9+/=]{100,}").Select(match => match.Value).ToArray();
        Assert.NotEmpty(secretFragments);
        var logged = string.Join("\n", logs.Messages);
        Assert.All(secretFragments, fragment => Assert.DoesNotContain(fragment, logged, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProductionHostWiringPersistsAcrossHostRestartsAndStartsTheInitializer()
    {
        var previous = Environment.GetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, directory);
            var configuration = new ConfigurationBuilder().Build();
            string protectedValue;

            await using (var first = BuildProductionProvider(configuration))
            {
                foreach (var hosted in first.GetServices<IHostedService>())
                {
                    await hosted.StartAsync(CancellationToken.None);
                }

                protectedValue = first.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("NevaxisZentra.SaasIdentity.MfaSecret.v1")
                    .Protect("totp-secret");
            }

            await using var second = BuildProductionProvider(configuration);
            foreach (var hosted in second.GetServices<IHostedService>())
            {
                await hosted.StartAsync(CancellationToken.None);
            }

            Assert.Single(KeyFiles());
            Assert.Equal(
                "totp-secret",
                second.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("NevaxisZentra.SaasIdentity.MfaSecret.v1")
                    .Unprotect(protectedValue));
        }
        finally
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void HostsWithoutAnEnvironmentKeepTheEphemeralRingAndCreateNothingOnDisk()
    {
        var previous = Environment.GetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, directory);
            var services = new ServiceCollection();
            services.AddNevaxisZentraPersistence(new ConfigurationBuilder().Build());
            using var provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<IDataProtectionProvider>();

            Assert.False(Directory.Exists(directory));
        }
        finally
        {
            Environment.SetEnvironmentVariable(DataProtectionKeyRing.PathEnvironmentVariable, previous);
        }
    }

    private static ServiceProvider BuildProductionProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNevaxisZentraPersistence(configuration, new TestHostEnvironment("Production"));
        return services.BuildServiceProvider();
    }
}
