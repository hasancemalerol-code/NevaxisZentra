using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;

namespace CrevaFactOS.Desktop.Runtime;

public sealed class RuntimeStartupResult
{
    public required Uri WebEntryUri { get; init; }

    public required Uri WebOriginUri { get; init; }

    public required string RepositoryRoot { get; init; }

    public required IReadOnlyList<string> Diagnostics { get; init; }
}

public sealed class DesktopRuntimeSupervisor : IAsyncDisposable
{
    private readonly List<RuntimeProcess> _ownedProcesses = [];
    private readonly List<string> _diagnostics = [];
    private string? _repositoryRoot;

    public event Action<string>? StatusChanged;

    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public async Task<RuntimeStartupResult> StartAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        _repositoryRoot = RuntimePaths.ResolveRepositoryRoot();
        _diagnostics.Add($"repository={_repositoryRoot}");

        ReportStatus("Sistem hazırlanıyor");
        ImportEnvironmentFile(RuntimePaths.GetEnvironmentFilePath(_repositoryRoot));

        if (options.ManageInfrastructure)
        {
            ReportStatus("Veritabanı kontrol ediliyor");
            await EnsurePostgresInfrastructureAsync(cancellationToken).ConfigureAwait(false);
        }

        if (options.ManageApi)
        {
            ReportStatus("API hazırlanıyor");
            await EnsureApiReadyAsync(options, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var apiReady = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
                options.ApiHealthUri,
                TimeSpan.FromSeconds(5),
                cancellationToken).ConfigureAwait(false);
            if (!apiReady)
            {
                throw new InvalidOperationException("API hazır değil.");
            }
        }

        if (options.ManageWeb)
        {
            ReportStatus("Uygulama hazırlanıyor");
            await EnsureWebReadyAsync(options, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var webReady = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
                new Uri(options.WebOriginUri, "/giris"),
                TimeSpan.FromSeconds(5),
                cancellationToken).ConfigureAwait(false);
            if (!webReady)
            {
                throw new InvalidOperationException("Web uygulaması hazır değil.");
            }
        }

        ReportStatus("Uygulama hazırlanıyor");

        return new RuntimeStartupResult
        {
            WebEntryUri = options.WebEntryUri,
            WebOriginUri = options.WebOriginUri,
            RepositoryRoot = _repositoryRoot,
            Diagnostics = _diagnostics.ToArray(),
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var process in _ownedProcesses)
        {
            process.Dispose();
        }

        _ownedProcesses.Clear();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task EnsurePostgresInfrastructureAsync(CancellationToken cancellationToken)
    {
        if (await RuntimeReadinessProbe.IsTcpPortOpenAsync("127.0.0.1", 5432, TimeSpan.FromSeconds(2), cancellationToken)
                .ConfigureAwait(false))
        {
            _diagnostics.Add("postgres=attached-existing");
            return;
        }

        var composePath = RuntimePaths.GetInfrastructureComposePath(_repositoryRoot!);
        if (!File.Exists(composePath))
        {
            throw new InvalidOperationException("Yerel altyapı yapılandırması bulunamadı.");
        }

        await RunUtilityAsync(
            "docker",
            $"compose -f \"{composePath}\" up -d postgres",
            _repositoryRoot!,
            environment: null,
            logPrefix: "docker-postgres",
            ownProcess: false,
            cancellationToken).ConfigureAwait(false);

        var postgresPort = 5432;
        var configuredPort = Environment.GetEnvironmentVariable("CREVAFACTOS__PERSISTENCE__PORT");
        if (int.TryParse(configuredPort, out var parsedPort))
        {
            postgresPort = parsedPort;
        }

        var postgresReady = await RuntimeReadinessProbe.IsTcpPortOpenAsync(
            "127.0.0.1",
            postgresPort,
            TimeSpan.FromSeconds(90),
            cancellationToken).ConfigureAwait(false);
        if (!postgresReady)
        {
            throw new InvalidOperationException("Veritabanı hazır değil.");
        }

        _diagnostics.Add("postgres=started");
    }

    private async Task EnsureApiReadyAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        if (await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
                options.ApiHealthUri,
                TimeSpan.FromSeconds(2),
                cancellationToken).ConfigureAwait(false))
        {
            var apiBase = new Uri($"http://127.0.0.1:{options.ApiPort}");
            if (await RuntimeIdentityReadinessProbe.IsApiIdentityReachableAsync(
                    apiBase,
                    cancellationToken).ConfigureAwait(false))
            {
                _diagnostics.Add("api=attached-existing");
                return;
            }

            _diagnostics.Add("api=attached-existing-identity-unhealthy");
        }

        var existing = FindListenerProcess(options.ApiPort, "CrevaFactOS.Api");
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"API portu kullanımda ancak hazır değil (PID {existing.Id}).");
        }

        var apiProject = RuntimePaths.GetApiProjectPath(_repositoryRoot!);
        var environment = BuildApiEnvironment(options, _repositoryRoot);
        var owned = RuntimeProcess.StartHidden(
            "dotnet",
            $"run --project \"{apiProject}\" --launch-profile http --urls http://127.0.0.1:{options.ApiPort}",
            _repositoryRoot!,
            environment,
            RuntimePaths.CreateLogFilePath("api"));
        _ownedProcesses.Add(owned);
        _diagnostics.Add($"api=started pid={owned.ProcessId}");

        var ready = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
            options.ApiHealthUri,
            options.StartupTimeout,
            cancellationToken).ConfigureAwait(false);
        if (!ready)
        {
            throw new TimeoutException("API başlatma zaman aşımına uğradı.");
        }

        var apiIdentityReady = await RuntimeIdentityReadinessProbe.IsApiIdentityReachableAsync(
            new Uri($"http://127.0.0.1:{options.ApiPort}"),
            cancellationToken).ConfigureAwait(false);
        if (!apiIdentityReady)
        {
            throw new InvalidOperationException(
                "API kimlik doğrulama uç noktası hazır değil (/api/identity/sign-in).");
        }
    }

    private async Task EnsureWebReadyAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var webProbe = options.WebReadinessUri;
        var startupStartedAt = DateTimeOffset.UtcNow;

        if (await TryAttachHealthyWebAsync(options, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        if (RuntimePortInspector.IsPortListening(options.WebPort))
        {
            var listenerPid = RuntimePortInspector.TryGetListeningProcessId(options.WebPort);
            _diagnostics.Add(listenerPid is null
                ? $"web=port-{options.WebPort}-listener-unknown"
                : $"web=port-{options.WebPort}-listener pid={listenerPid}");

            if (listenerPid is not null)
            {
                var listenerOwned = RuntimePortInspector.IsCrevaFactOSWebListener(listenerPid.Value, _repositoryRoot);
                _diagnostics.Add($"web=listener-classification={(listenerOwned ? "crevafactos-owned" : "foreign")}");
                if (!listenerOwned)
                {
                    throw new InvalidOperationException(
                        $"Web portu {options.WebPort} başka bir süreç tarafından kullanılıyor (PID {listenerPid}). CrevaFactOS web dinleyicisi değil.");
                }

                _diagnostics.Add($"web=stale-listener-on-port-{options.WebPort}");
                if (!RuntimePortInspector.TryTerminateCrevaFactOSWebListener(
                        listenerPid.Value,
                        _repositoryRoot,
                        _diagnostics))
                {
                    throw new InvalidOperationException(
                        $"Web portu {options.WebPort} yanıt vermiyor ve güvenli şekilde temizlenemedi (PID {listenerPid}).");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (await TryAttachHealthyWebAsync(options, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false))
                {
                    _diagnostics.Add("web=attached-existing-after-stale-probe");
                    return;
                }

                _diagnostics.Add($"web=stale-listener-on-port-{options.WebPort}-pid-unknown");
            }
        }

        var (stdoutLog, stderrLog) = RuntimePaths.CreateWebLogPaths(_repositoryRoot);
        _diagnostics.Add($"web=log-stdout={stdoutLog}");
        _diagnostics.Add($"web=log-stderr={stderrLog}");

        var environment = BuildWebEnvironment(options, _repositoryRoot);
        var launchCommand =
            $"--filter @crevafactos/web exec next dev -H 127.0.0.1 -p {options.WebPort.ToString(CultureInfo.InvariantCulture)}";
        _diagnostics.Add($"web=launch-command=pnpm {launchCommand}");
        _diagnostics.Add($"web=working-directory={_repositoryRoot}");
        _diagnostics.Add($"web=readiness-url={webProbe}");
        _diagnostics.Add($"web=timeout-seconds={options.StartupTimeout.TotalSeconds}");

        var owned = RuntimeProcess.StartHidden(
            ResolvePnpmExecutable(),
            launchCommand,
            _repositoryRoot!,
            environment,
            stdoutLog,
            stderrLog);
        _ownedProcesses.Add(owned);
        _diagnostics.Add($"web=started pid={owned.ProcessId}");

        var ready = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
            webProbe,
            options.StartupTimeout,
            cancellationToken).ConfigureAwait(false);
        var elapsedSeconds = (DateTimeOffset.UtcNow - startupStartedAt).TotalSeconds;
        _diagnostics.Add($"web=startup-elapsed-seconds={elapsedSeconds:F1}");

        if (!ready)
        {
            throw new TimeoutException(
                $"Web uygulaması başlatma zaman aşımına uğradı. stdout={stdoutLog} stderr={stderrLog}");
        }

        var webIdentityReady = await RuntimeIdentityReadinessProbe.IsWebIdentityProxyReachableAsync(
            options.WebOriginUri,
            cancellationToken).ConfigureAwait(false);
        if (!webIdentityReady)
        {
            throw new InvalidOperationException(
                "Web kimlik doğrulama proxy'si hazır değil (/api/identity/sign-in). CREVAFACTOS_API_BASE_URL doğrulanamadı.");
        }

        if (!RuntimePortInspector.IsPortListening(options.WebPort))
        {
            throw new InvalidOperationException(
                $"Web başlatıldı ancak beklenen port {options.WebPort} dinlenmiyor.");
        }
    }

    private async Task<bool> TryAttachHealthyWebAsync(
        Configuration.DesktopRuntimeOptions options,
        TimeSpan probeTimeout,
        CancellationToken cancellationToken)
    {
        if (!await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
                options.WebReadinessUri,
                probeTimeout,
                cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        if (!await RuntimeIdentityReadinessProbe.IsWebIdentityProxyReachableAsync(
                options.WebOriginUri,
                cancellationToken).ConfigureAwait(false))
        {
            _diagnostics.Add("web=http-ready-identity-proxy-unhealthy");
            return false;
        }

        _diagnostics.Add("web=attached-existing");
        return true;
    }

    private static Dictionary<string, string> BuildWebEnvironment(
        Configuration.DesktopRuntimeOptions options,
        string? repositoryRoot)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(repositoryRoot))
        {
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetEnvironmentFilePath(repositoryRoot));
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetWebEnvironmentFilePath(repositoryRoot));
        }

        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "DevelopmentSaasIdentity__");
        environment["CREVAFACTOS_API_BASE_URL"] = $"http://127.0.0.1:{options.ApiPort}";
        environment["PORT"] = options.WebPort.ToString(CultureInfo.InvariantCulture);
        environment["HOSTNAME"] = "127.0.0.1";
        environment["NODE_ENV"] = "development";
        return environment;
    }

    private static Dictionary<string, string> BuildApiEnvironment(
        Configuration.DesktopRuntimeOptions options,
        string? repositoryRoot)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["CREVAFACTOS__AUTHENTICATION__MODE"] = "SessionCookie",
        };

        if (!string.IsNullOrWhiteSpace(repositoryRoot))
        {
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetEnvironmentFilePath(repositoryRoot),
                overwrite: true);
        }

        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "DevelopmentSaasIdentity__");
        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "CREVAFACTOS__");

        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__ENABLED", "true");
        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__HOST", "127.0.0.1");
        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__PORT", "5432");
        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__DATABASE", "crevafactos_local");
        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__USERNAME", "crevafactos_runtime");
        SetIfMissing(
            environment,
            "CREVAFACTOS__PERSISTENCE__PASSWORD",
            "local_runtime_only_change_me");
        SetIfMissing(environment, "CREVAFACTOS__PERSISTENCE__SSL_MODE", "Disable");

        return environment;

        static void SetIfMissing(IDictionary<string, string> target, string key, string value)
        {
            if (!target.ContainsKey(key) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                target[key] = value;
            }
        }
    }

    private static void ImportEnvironmentFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(parts[0])))
            {
                Environment.SetEnvironmentVariable(parts[0], parts[1], EnvironmentVariableTarget.Process);
            }
        }
    }

    private static Process? FindListenerProcess(int port, string expectedPathFragment)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var endpoint = properties.GetActiveTcpListeners().FirstOrDefault(listener => listener.Port == port);
        if (endpoint is null)
        {
            return null;
        }

        var connections = properties.GetActiveTcpConnections()
            .Where(connection => connection.LocalEndPoint.Port == port)
            .ToArray();
        if (connections.Length == 0)
        {
            return null;
        }

        foreach (var connection in connections)
        {
            var process = TryGetProcessOwningPort(port);
            if (process is null)
            {
                continue;
            }

            using (process)
            {
                if (string.IsNullOrWhiteSpace(process.MainModule?.FileName))
                {
                    continue;
                }

                if (process.MainModule.FileName.Contains(expectedPathFragment, StringComparison.OrdinalIgnoreCase))
                {
                    return Process.GetProcessById(process.Id);
                }
            }
        }

        return null;
    }

    private static Process? TryGetProcessOwningPort(int port)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            var line = output
                .Split('\n')
                .Select(item => item.Trim())
                .FirstOrDefault(item => item.Contains($":{port}", StringComparison.Ordinal) && item.Contains("LISTENING", StringComparison.Ordinal));
            if (line is null)
            {
                return null;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(parts[^1], out var pid))
            {
                return null;
            }

            return Process.GetProcessById(pid);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolvePnpmExecutable()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var segment in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(segment.Trim(), "pnpm.cmd");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("pnpm bulunamadı. Node.js ve pnpm kurulu olmalıdır.");
    }

    private async Task RunUtilityAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        string logPrefix,
        bool ownProcess,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ownProcess)
        {
            var owned = RuntimeProcess.StartHidden(
                fileName,
                arguments,
                workingDirectory,
                environment,
                RuntimePaths.CreateLogFilePath(logPrefix));
            _ownedProcesses.Add(owned);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"{fileName} başlatılamadı.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            _diagnostics.Add(stdout.Trim());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            _diagnostics.Add(stderr.Trim());
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} komutu başarısız oldu.");
        }
    }

    private void ReportStatus(string status) => StatusChanged?.Invoke(status);
}
