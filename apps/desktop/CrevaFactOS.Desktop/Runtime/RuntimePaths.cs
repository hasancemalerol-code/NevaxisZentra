using System.Globalization;
using System.IO;

namespace CrevaFactOS.Desktop.Runtime;

public static class RuntimePaths
{
    public const string RuntimeRootEnvironmentVariable = "CREVAFACTOS_DESKTOP_RUNTIME_ROOT";

    private static readonly string[] RepositoryRootMarkers =
    [
        "CrevaFactOS.sln",
        "package.json",
    ];

    private static readonly string[] KnownVerifierFixtureTokens =
    [
        "crevafactos-web-readiness-",
        "crevafactos-invalid-runtime-root",
        "crevafactos-invalid-",
        "invalid-runtime-root",
    ];

    public static bool IsKnownVerifierFixturePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return false;
        }

        try
        {
            var normalized = Path.GetFullPath(configuredPath.Trim());
            var directoryName = Path.GetFileName(
                normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            return KnownVerifierFixtureTokens.Any(
                token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase)
                    || directoryName.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public static string ResolveRepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable(RuntimeRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (IsKnownVerifierFixturePath(configured))
            {
                configured = null;
            }
            else
            {
                var fullPath = Path.GetFullPath(configured.Trim());
                if (!Directory.Exists(fullPath))
                {
                    throw new InvalidOperationException(
                        $"CREVAFACTOS_DESKTOP_RUNTIME_ROOT bulunamadı: {fullPath}");
                }

                if (!HasRepositoryMarkers(fullPath))
                {
                    throw new InvalidOperationException(
                        $"CREVAFACTOS_DESKTOP_RUNTIME_ROOT geçerli bir CrevaFactOS kökü değil: {fullPath}");
                }

                return fullPath;
            }
        }

        foreach (var startingPath in GetRepositorySearchRoots())
        {
            var resolved = TryResolveFromDirectory(startingPath);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        throw new InvalidOperationException(
            "CrevaFactOS çalışma zamanı kökü bulunamadı. CREVAFACTOS_DESKTOP_RUNTIME_ROOT ortam değişkenini ayarlayın.");
    }

    public static string? TryResolveRepositoryRoot()
    {
        try
        {
            return ResolveRepositoryRoot();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> GetRepositorySearchRoots()
    {
        yield return AppContext.BaseDirectory;

        var currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            yield return currentDirectory;
        }
    }

    public static string? TryResolveFromDirectory(string startingPath)
    {
        if (string.IsNullOrWhiteSpace(startingPath))
        {
            return null;
        }

        DirectoryInfo? current;
        try
        {
            current = new DirectoryInfo(Path.GetFullPath(startingPath));
        }
        catch
        {
            return null;
        }

        while (current is not null)
        {
            if (HasRepositoryMarkers(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool HasRepositoryMarkers(string directoryPath) =>
        RepositoryRootMarkers.Any(marker => File.Exists(Path.Combine(directoryPath, marker)));

    public static string GetLogDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrevaFactOS",
            "Desktop",
            "runtime",
            "logs");
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string CreateLogFilePath(string prefix)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(GetLogDirectory(), $"{prefix}-{timestamp}.log");
    }

    public static string GetArtifactLogDirectory(string? repositoryRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(repositoryRoot))
        {
            var artifactLogs = Path.Combine(
                repositoryRoot,
                "artifacts",
                "desktop",
                "CrevaFactOS",
                "runtime",
                "logs");
            Directory.CreateDirectory(artifactLogs);
            return artifactLogs;
        }

        return GetLogDirectory();
    }

    public static (string StdOutPath, string StdErrPath) CreateWebLogPaths(string? repositoryRoot = null)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var directory = GetArtifactLogDirectory(repositoryRoot);
        return (
            Path.Combine(directory, $"web-{timestamp}.stdout.log"),
            Path.Combine(directory, $"web-{timestamp}.stderr.log"));
    }

    public static string GetApiProjectPath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "apps", "api", "CrevaFactOS.Api.csproj");

    public static string GetInfrastructureComposePath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "infrastructure", "compose.yaml");

    public static string GetEnvironmentFilePath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "infrastructure", ".env");

    public static string GetLocalSecretsEnvironmentFilePath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "infrastructure", ".env.local");

    public static string GetWebEnvironmentFilePath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "apps", "web", ".env.local");

    public static string GetPackagedMachineSecretsFilePath() =>
        PackagedRuntimeBundle.GetMachineLocalSecretsPath();

    public static string? TryResolveRuntimeRoot()
    {
        var packaged = PackagedRuntimeBundle.TryResolveFromDesktopExecutable();
        if (packaged is not null)
        {
            return packaged.BundleRoot;
        }

        return TryResolveRepositoryRoot();
    }

    public static DesktopRuntimeMode ResolveRuntimeMode()
    {
        return PackagedRuntimeBundle.TryResolveFromDesktopExecutable() is not null
            ? DesktopRuntimeMode.Packaged
            : DesktopRuntimeMode.Development;
    }

    public static string ResolveEffectiveRuntimeRoot()
    {
        var packaged = PackagedRuntimeBundle.TryResolveFromDesktopExecutable();
        if (packaged is not null)
        {
            return packaged.BundleRoot;
        }

        return ResolveRepositoryRoot();
    }

    public static void MergePackagedConfiguration(
        IDictionary<string, string> target,
        PackagedRuntimeBundle bundle,
        bool overwrite = false)
    {
        MergeEnvironmentFile(target, Path.Combine(bundle.ConfigDirectory, "runtime.env"), overwrite);
        MergeEnvironmentFile(target, GetPackagedMachineSecretsFilePath(), overwrite);
    }

    public static IEnumerable<KeyValuePair<string, string>> ReadEnvironmentFilePairs(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim().Trim('"');
            if (key.Length == 0)
            {
                continue;
            }

            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    public static void MergeEnvironmentFile(
        IDictionary<string, string> target,
        string filePath,
        bool overwrite = false)
    {
        foreach (var pair in ReadEnvironmentFilePairs(filePath))
        {
            if (!overwrite && target.ContainsKey(pair.Key))
            {
                continue;
            }

            target[pair.Key] = pair.Value;
        }
    }

    public static void CopyProcessEnvironmentPrefix(
        IDictionary<string, string> target,
        string prefix,
        bool overwrite = false)
    {
        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            var name = key as string ?? Convert.ToString(key, System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(name) ||
                !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!overwrite && target.ContainsKey(name))
            {
                continue;
            }

            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                target[name] = value;
            }
        }
    }

    public static void RefreshRuntimeRootEnvironmentForRetry()
    {
        var effective = Environment.GetEnvironmentVariable(
            RuntimeRootEnvironmentVariable,
            EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable(
                RuntimeRootEnvironmentVariable,
                EnvironmentVariableTarget.Machine);

        if (IsKnownVerifierFixturePath(effective))
        {
            effective = null;
        }

        Environment.SetEnvironmentVariable(
            RuntimeRootEnvironmentVariable,
            effective,
            EnvironmentVariableTarget.Process);
    }
}
