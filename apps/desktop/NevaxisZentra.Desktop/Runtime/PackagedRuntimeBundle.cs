using System.IO;
using System.Text.Json;

namespace NevaxisZentra.Desktop.Runtime;

public sealed class PackagedRuntimeBundle
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    public required DesktopRuntimeMode Mode { get; init; }

    public required string BundleRoot { get; init; }

    public required string DesktopDirectory { get; init; }

    public required string ApiDirectory { get; init; }

    public required string WebDirectory { get; init; }

    public string WebAppDirectory => Path.Combine(WebDirectory, "apps", "web");

    public required string ConfigDirectory { get; init; }

    public required string ManifestsDirectory { get; init; }

    public required string MigrationsDirectory { get; init; }

    public required string LogsDirectory { get; init; }

    public string PostgresBinDirectory => Path.Combine(BundleRoot, "postgres", "bin");

    public string PostgresTemplateDirectory => Path.Combine(BundleRoot, "postgres", "template");

    public string NodeDirectory => Path.Combine(BundleRoot, "node");

    public string NodeExecutablePath => Path.Combine(NodeDirectory, "node.exe");

    public string ToolsDirectory => Path.Combine(BundleRoot, "tools");

    public static string GetPersistentDataRoot()
    {
        var testRoot = Environment.GetEnvironmentVariable("FACTOS_TEST_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(testRoot))
        {
            return Path.GetFullPath(testRoot);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Nevaxis",
            "FactOS");
    }

    public string ManifestPath => Path.Combine(ManifestsDirectory, "runtime-manifest.json");

    public string RuntimeConfigPath => Path.Combine(ConfigDirectory, "runtime.json");

    public string ApiExecutablePath =>
        Path.Combine(ApiDirectory, "NevaxisZentra.Api.exe");

    public string WebServerScriptPath =>
        Path.Combine(WebAppDirectory, "server.js");

    public string ExpectedSchemaVersionPath =>
        Path.Combine(MigrationsDirectory, "expected-schema-version.txt");

    public static string GetMachineLocalSecretsPath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NevaxisZentra",
            "config");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "runtime.env.local");
    }

    public static PackagedRuntimeBundle? TryResolveFromDesktopExecutable(string? desktopExecutableDirectory = null)
    {
        var executableDirectory = NormalizeDirectoryPath(
            desktopExecutableDirectory ?? ResolveDesktopExecutableDirectory());

        foreach (var candidate in EnumerateBundleCandidates(executableDirectory))
        {
            if (File.Exists(Path.Combine(candidate.BundleRoot, "manifests", "runtime-manifest.json")))
            {
                return Create(candidate.BundleRoot, candidate.DesktopDirectory);
            }
        }

        return null;
    }

    private static IEnumerable<(string BundleRoot, string DesktopDirectory)> EnumerateBundleCandidates(
        string executableDirectory)
    {
        yield return ResolveCanonicalDesktopLayout(executableDirectory);

        if (File.Exists(Path.Combine(executableDirectory, "manifests", "runtime-manifest.json")))
        {
            var desktopDirectory = Directory.Exists(Path.Combine(executableDirectory, "desktop"))
                ? Path.Combine(executableDirectory, "desktop")
                : executableDirectory;
            yield return (executableDirectory, desktopDirectory);
        }

        var parentDirectory = Directory.GetParent(executableDirectory)?.FullName;
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            yield return ResolveCanonicalDesktopLayout(parentDirectory);
        }
    }

    private static (string BundleRoot, string DesktopDirectory) ResolveCanonicalDesktopLayout(string directory)
    {
        var desktopDirectoryName = Path.GetFileName(directory);
        if (string.Equals(desktopDirectoryName, "desktop", StringComparison.OrdinalIgnoreCase))
        {
            var bundleRoot = Directory.GetParent(directory)?.FullName ?? directory;
            return (bundleRoot, directory);
        }

        var nestedDesktopDirectory = Path.Combine(directory, "desktop");
        if (Directory.Exists(nestedDesktopDirectory))
        {
            return (directory, nestedDesktopDirectory);
        }

        return (Directory.GetParent(directory)?.FullName ?? directory, directory);
    }

    public static string ResolveDesktopExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var directory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return NormalizeDirectoryPath(directory);
            }
        }

        return NormalizeDirectoryPath(AppContext.BaseDirectory);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static PackagedRuntimeBundle Create(string bundleRoot, string? desktopDirectory = null)
    {
        var normalizedRoot = Path.GetFullPath(bundleRoot);
        var desktopDir = Path.GetFullPath(
            desktopDirectory ?? Path.Combine(normalizedRoot, "desktop"));

        return new PackagedRuntimeBundle
        {
            Mode = DesktopRuntimeMode.Packaged,
            BundleRoot = normalizedRoot,
            DesktopDirectory = desktopDir,
            ApiDirectory = Path.Combine(normalizedRoot, "api"),
            WebDirectory = Path.Combine(normalizedRoot, "web"),
            ConfigDirectory = Path.Combine(normalizedRoot, "config"),
            ManifestsDirectory = Path.Combine(normalizedRoot, "manifests"),
            MigrationsDirectory = Path.Combine(normalizedRoot, "migrations"),
            LogsDirectory = Path.Combine(normalizedRoot, "logs"),
        };
    }

    public RuntimeManifestDocument? TryReadManifest()
    {
        if (!File.Exists(ManifestPath))
        {
            return null;
        }

        var json = File.ReadAllText(ManifestPath);
        return JsonSerializer.Deserialize<RuntimeManifestDocument>(
            json,
            ManifestJsonOptions);
    }
}

public sealed class RuntimeManifestDocument
{
    public string? ProductName { get; init; }

    public string? RuntimeVersion { get; init; }

    public string? SourceCommit { get; init; }

    public string? SourceBranch { get; init; }

    public string? BuildTimestampUtc { get; init; }

    public int WebPort { get; init; } = 3001;

    public int ApiPort { get; init; } = 5000;

    public string? DatabaseHost { get; init; }

    public int DatabasePort { get; init; } = 5432;

    public string? DatabaseName { get; init; }

    public string? ExpectedSchemaVersion { get; init; }

    public string? DatabaseArchitecture { get; init; }

    public string? DeploymentChannel { get; init; }

    public int CanonicalRouteCount { get; init; } = 46;

    public bool IsDemoChannel =>
        string.Equals(DeploymentChannel, "DEMO", StringComparison.OrdinalIgnoreCase)
        || string.Equals(DeploymentChannel, "PRESENTATION", StringComparison.OrdinalIgnoreCase);
}
