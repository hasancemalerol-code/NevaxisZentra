using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;

namespace NevaxisFactOS.Desktop.Runtime;

public sealed class RuntimeStartupResult
{
    public required Uri WebEntryUri { get; init; }

    public required Uri WebOriginUri { get; init; }

    public required int SelectedWebPort { get; init; }

    public required int PreferredWebPort { get; init; }

    public required string RepositoryRoot { get; init; }

    public required DesktopRuntimeMode RuntimeMode { get; init; }

    public required IReadOnlyList<string> Diagnostics { get; init; }
}

public sealed class DesktopRuntimeSupervisor : IAsyncDisposable
{
    private readonly List<RuntimeProcess> _ownedProcesses = [];
    private readonly List<string> _diagnostics = [];
    private string? _runtimeRoot;
    private PackagedRuntimeBundle? _packagedBundle;
    private EmbeddedPostgreSqlRuntime? _embeddedPostgres;
    private RuntimeWatchdog? _watchdog;
    private StartupSession? _session;

    public event Action<string>? StatusChanged;

    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public StartupSession? Session => _session;

    public string? LastErrorCode { get; private set; }

    public async Task<RuntimeStartupResult> StartAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        _session = new StartupSession();
        _diagnostics.Add($"startup-session={_session.SessionId}");
        _session.SetStage("desktop-start");

        _packagedBundle = PackagedRuntimeBundle.TryResolveFromDesktopExecutable();
        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is null)
        {
            LastErrorCode = FactOsStartupErrorCodes.BundleMissing;
            throw new StartupPreflightException(
                LastErrorCode,
                FactOsStartupErrorCodes.ToCustomerMessage(LastErrorCode),
                ["FAIL bundle=runtime-manifest.json not found"]);
        }

        StartupPreflightValidator.Validate(_session, _packagedBundle);
        _runtimeRoot = _packagedBundle?.BundleRoot ?? RuntimePaths.ResolveRepositoryRoot();
        _diagnostics.Add($"mode={options.Mode}");
        _diagnostics.Add($"desktop-directory={PackagedRuntimeBundle.ResolveDesktopExecutableDirectory()}");
        _diagnostics.Add($"packaged-bundle={(_packagedBundle?.BundleRoot ?? "null")}");
        _diagnostics.Add($"runtime-root={_runtimeRoot}");

        ReportStatus("Sistem hazırlanıyor");
        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null)
        {
            ImportPackagedConfiguration(_packagedBundle);
            PackagedFirstRunInitializer.EnsureMachineSecrets(_packagedBundle, _diagnostics);
        }
        else
        {
            ImportEnvironmentFile(RuntimePaths.GetEnvironmentFilePath(_runtimeRoot));
            ImportEnvironmentFile(RuntimePaths.GetLocalSecretsEnvironmentFilePath(_runtimeRoot));
        }

        if (options.ManageInfrastructure)
        {
            _session?.SetStage("database-start");
            ReportStatus("Veritabanı kontrol ediliyor");
            await EnsurePostgresInfrastructureAsync(options, cancellationToken).ConfigureAwait(false);
            _session?.SetStage("database-health");
        }

        if (options.ManageApi)
        {
            _session?.SetStage("api-start");
            ReportStatus("API hazırlanıyor");
            await EnsureApiReadyAsync(options, cancellationToken).ConfigureAwait(false);
            _session?.SetStage("api-health");
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
            _session?.SetStage("web-start");
            ReportStatus("Uygulama hazırlanıyor");
            var webOptions = options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null
                ? await ResolvePackagedWebOptionsAsync(options, cancellationToken).ConfigureAwait(false)
                : options;
            await EnsureWebReadyAsync(webOptions, cancellationToken).ConfigureAwait(false);
            _session?.SetStage("web-health");
            options = webOptions;
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

        _session?.SetStage("ready");
        _watchdog = new RuntimeWatchdog(_ownedProcesses, _diagnostics);
        ReportStatus("Uygulama hazırlanıyor");

        return new RuntimeStartupResult
        {
            WebEntryUri = options.WebEntryUri,
            WebOriginUri = options.WebOriginUri,
            SelectedWebPort = options.WebPort,
            PreferredWebPort = options.PreferredWebPort,
            RepositoryRoot = _runtimeRoot,
            RuntimeMode = options.Mode,
            Diagnostics = _diagnostics.ToArray(),
        };
    }

    private async Task<Configuration.DesktopRuntimeOptions> ResolvePackagedWebOptionsAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var occupied = new List<string>();

        foreach (var candidatePort in RuntimePortInspector.PackagedWebPortCandidates)
        {
            var candidateOptions = options.WithWebPort(candidatePort);
            if (!RuntimePortInspector.IsPortListening(candidatePort))
            {
                _diagnostics.Add($"web=selected-port={candidatePort} reason=free preferred={options.PreferredWebPort}");
                return candidateOptions;
            }

            if (await TryAttachHealthyWebAsync(candidateOptions, TimeSpan.FromSeconds(5), cancellationToken)
                    .ConfigureAwait(false))
            {
                _diagnostics.Add($"web=selected-port={candidatePort} reason=attached-existing preferred={options.PreferredWebPort}");
                return candidateOptions;
            }

            var listenerPid = RuntimePortInspector.TryGetListeningProcessId(candidatePort);
            if (listenerPid is null)
            {
                occupied.Add($"{candidatePort}:pid-unknown");
                continue;
            }

            var classification = RuntimePortInspector.ClassifyWebListener(
                listenerPid.Value,
                _runtimeRoot,
                _packagedBundle?.WebAppDirectory);
            _diagnostics.Add(
                $"web=port-{candidatePort}-listener pid={listenerPid} classification={classification}");

            if (classification is WebListenerClassification.NevaxisFactOSOwned
                or WebListenerClassification.NevaxisFactOSVerifierOrphan)
            {
                if (!RuntimePortInspector.TryTerminateNevaxisFactOSWebListener(
                        listenerPid.Value,
                        _runtimeRoot,
                        _packagedBundle?.WebAppDirectory,
                        _diagnostics))
                {
                    throw new InvalidOperationException(
                        $"Web portu {candidatePort} yanıt vermiyor ve güvenli şekilde temizlenemedi (PID {listenerPid}).");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                _diagnostics.Add(
                    $"web=selected-port={candidatePort} reason=stale-owned-listener-reclaimed preferred={options.PreferredWebPort}");
                return candidateOptions;
            }

            occupied.Add($"{candidatePort}:PID {listenerPid} ({classification})");
        }

        throw new InvalidOperationException(
            $"NevaxisFactOS için kullanılabilir yerel web portu bulunamadı. {string.Join(", ", occupied)}");
    }

    public async ValueTask DisposeAsync()
    {
        _watchdog?.Dispose();
        _watchdog = null;

        foreach (var process in _ownedProcesses)
        {
            process.Dispose();
        }

        _ownedProcesses.Clear();

        if (_embeddedPostgres is not null)
        {
            await _embeddedPostgres.DisposeAsync().ConfigureAwait(false);
            _embeddedPostgres = null;
        }
    }

    private async Task EnsurePostgresInfrastructureAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var postgresPort = ResolvePostgresPort();
        var postgresHost = ResolvePostgresHost();

        if (await RuntimeReadinessProbe.IsTcpPortOpenAsync(
                postgresHost,
                postgresPort,
                TimeSpan.FromSeconds(2),
                cancellationToken).ConfigureAwait(false))
        {
            _diagnostics.Add("postgres=attached-existing");
            await ValidateSchemaVersionIfRequiredAsync(options, postgresHost, postgresPort, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null &&
            UsesEmbeddedPostgres(_packagedBundle))
        {
            _embeddedPostgres = new EmbeddedPostgreSqlRuntime(_packagedBundle, _diagnostics);
            await _embeddedPostgres.EnsureReadyAsync(postgresPort, cancellationToken).ConfigureAwait(false);
            await ValidateSchemaVersionIfRequiredAsync(options, postgresHost, postgresPort, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (options.Mode == DesktopRuntimeMode.Packaged)
        {
            throw new InvalidOperationException(
                $"Paketlenmiş çalışma zamanı için PostgreSQL hazır değil ({postgresHost}:{postgresPort}).");
        }

        var composePath = RuntimePaths.GetInfrastructureComposePath(_runtimeRoot!);
        if (!File.Exists(composePath))
        {
            throw new InvalidOperationException("Yerel altyapı yapılandırması bulunamadı.");
        }

        await RunUtilityAsync(
            "docker",
            $"compose -f \"{composePath}\" up -d postgres",
            _runtimeRoot!,
            environment: null,
            logPrefix: "docker-postgres",
            ownProcess: false,
            cancellationToken).ConfigureAwait(false);

        var postgresReady = await RuntimeReadinessProbe.IsTcpPortOpenAsync(
            postgresHost,
            postgresPort,
            TimeSpan.FromSeconds(90),
            cancellationToken).ConfigureAwait(false);
        if (!postgresReady)
        {
            throw new InvalidOperationException("Veritabanı hazır değil.");
        }

        _diagnostics.Add("postgres=started");
        await ValidateSchemaVersionIfRequiredAsync(options, postgresHost, postgresPort, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureApiReadyAsync(
        Configuration.DesktopRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var apiBaseUri = new Uri($"http://127.0.0.1:{options.ApiPort}");

        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null)
        {
            var listenerPid = RuntimePortInspector.TryGetListeningProcessId(options.ApiPort);
            if (listenerPid is not null)
            {
                var commandLine = RuntimePortInspector.TryGetProcessCommandLine(listenerPid.Value) ?? string.Empty;
                var isPackagedListener = commandLine.Contains(
                    _packagedBundle.ApiDirectory,
                    StringComparison.OrdinalIgnoreCase);
                if (!isPackagedListener)
                {
                    _diagnostics.Add($"api=foreign-listener pid={listenerPid}");
                    if (!RuntimePortInspector.TryTerminateProcessTree(listenerPid.Value, _diagnostics))
                    {
                        throw new InvalidOperationException(
                            $"Paketlenmiş çalışma zamanı için yabancı API dinleyicisi sonlandırılamadı (PID {listenerPid}).");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
                options.ApiHealthUri,
                TimeSpan.FromSeconds(2),
                cancellationToken).ConfigureAwait(false))
        {
            if (await RuntimeIdentityReadinessProbe.WaitForApiIdentityReachableAsync(
                    apiBaseUri,
                    TimeSpan.FromSeconds(30),
                    cancellationToken).ConfigureAwait(false))
            {
                if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null)
                {
                    var listenerPid = RuntimePortInspector.TryGetListeningProcessId(options.ApiPort);
                    var commandLine = listenerPid is null
                        ? string.Empty
                        : RuntimePortInspector.TryGetProcessCommandLine(listenerPid.Value) ?? string.Empty;
                    if (!commandLine.Contains(
                            _packagedBundle.ApiDirectory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Paketlenmiş çalışma zamanı kaynak ağacı API'sine bağlanamaz.");
                    }
                }

                _diagnostics.Add("api=attached-existing");
                return;
            }

            _diagnostics.Add("api=attached-existing-identity-unhealthy");
        }

        var existing = FindListenerProcess(options.ApiPort, "NevaxisFactOS.Api");
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"API portu kullanımda ancak hazır değil (PID {existing.Id}).");
        }

        var apiProject = RuntimePaths.GetApiProjectPath(_runtimeRoot!);
        var environment = BuildApiEnvironment(options, _runtimeRoot, _packagedBundle);
        RuntimeProcess owned;
        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null)
        {
            var apiExecutable = _packagedBundle.ApiExecutablePath;
            if (!File.Exists(apiExecutable))
            {
                throw new InvalidOperationException($"Paketlenmiş API bulunamadı: {apiExecutable}");
            }

            owned = RuntimeProcess.StartHidden(
                apiExecutable,
                $"--urls http://127.0.0.1:{options.ApiPort}",
                _packagedBundle.ApiDirectory,
                environment,
                RuntimePaths.CreateLogFilePath("api"));
        }
        else
        {
            owned = RuntimeProcess.StartHidden(
                "dotnet",
                $"run --project \"{apiProject}\" --launch-profile http --urls http://127.0.0.1:{options.ApiPort}",
                _runtimeRoot!,
                environment,
                RuntimePaths.CreateLogFilePath("api"));
        }
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

        var apiIdentityReady = await RuntimeIdentityReadinessProbe.WaitForApiIdentityReachableAsync(
            apiBaseUri,
            options.StartupTimeout,
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

        var runtimeRoot = _runtimeRoot;
        if (RuntimePortInspector.IsPortListening(options.WebPort))
        {
            var listenerPid = RuntimePortInspector.TryGetListeningProcessId(options.WebPort);
            _diagnostics.Add(listenerPid is null
                ? $"web=port-{options.WebPort}-listener-unknown"
                : $"web=port-{options.WebPort}-listener pid={listenerPid}");

            if (listenerPid is not null)
            {
                var classification = RuntimePortInspector.ClassifyWebListener(
                    listenerPid.Value,
                    runtimeRoot,
                    _packagedBundle?.WebAppDirectory);
                _diagnostics.Add($"web=listener-classification={classification}");
                if (classification is WebListenerClassification.Foreign or WebListenerClassification.Unknown)
                {
                    throw new InvalidOperationException(
                        $"Web portu {options.WebPort} başka bir süreç tarafından kullanılıyor (PID {listenerPid}). NevaxisFactOS web dinleyicisi değil.");
                }

                _diagnostics.Add($"web=stale-listener-on-port-{options.WebPort}");
                if (!RuntimePortInspector.TryTerminateNevaxisFactOSWebListener(
                        listenerPid.Value,
                        runtimeRoot,
                        _packagedBundle?.WebAppDirectory,
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

        var (stdoutLog, stderrLog) = RuntimePaths.CreateWebLogPaths(
            options.Mode == DesktopRuntimeMode.Development ? runtimeRoot : null);

        var environment = BuildWebEnvironment(options, runtimeRoot, _packagedBundle);
        RuntimeProcess owned;
        if (options.Mode == DesktopRuntimeMode.Packaged && _packagedBundle is not null)
        {
            var serverScript = _packagedBundle.WebServerScriptPath;
            if (!File.Exists(serverScript))
            {
                throw new InvalidOperationException($"Paketlenmiş web sunucusu bulunamadı: {serverScript}");
            }

            var launchCommand = $"\"{serverScript}\"";
            _diagnostics.Add($"web=launch-command=node {launchCommand}");
            _diagnostics.Add($"web=working-directory={_packagedBundle.WebAppDirectory}");
            owned = RuntimeProcess.StartHidden(
                ResolveNodeExecutable(),
                launchCommand,
                _packagedBundle.WebAppDirectory,
                environment,
                stdoutLog,
                stderrLog);
        }
        else
        {
            var launchCommand =
                $"--filter @nevaxisfactos/web exec next dev -H 127.0.0.1 -p {options.WebPort.ToString(CultureInfo.InvariantCulture)}";
            _diagnostics.Add($"web=launch-command=pnpm {launchCommand}");
            _diagnostics.Add($"web=working-directory={runtimeRoot}");
            owned = RuntimeProcess.StartHidden(
                ResolvePnpmExecutable(),
                launchCommand,
                runtimeRoot!,
                environment,
                stdoutLog,
                stderrLog);
        }
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
                "Web kimlik doğrulama proxy'si hazır değil (/api/identity/sign-in). NEVAXIS_FACTOS_API_BASE_URL doğrulanamadı.");
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
        string? runtimeRoot,
        PackagedRuntimeBundle? packagedBundle)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (packagedBundle is not null)
        {
            RuntimePaths.MergePackagedConfiguration(environment, packagedBundle);
        }
        else if (!string.IsNullOrWhiteSpace(runtimeRoot))
        {
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetEnvironmentFilePath(runtimeRoot));
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetLocalSecretsEnvironmentFilePath(runtimeRoot));
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetWebEnvironmentFilePath(runtimeRoot));
        }

        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "DevelopmentSaasIdentity__");
        environment["NEVAXIS_FACTOS_API_BASE_URL"] = $"http://127.0.0.1:{options.ApiPort}";
        environment["PORT"] = options.WebPort.ToString(CultureInfo.InvariantCulture);
        environment["HOSTNAME"] = "127.0.0.1";
        environment["NODE_ENV"] = packagedBundle is null ? "development" : "production";
        return environment;
    }

    private static Dictionary<string, string> BuildApiEnvironment(
        Configuration.DesktopRuntimeOptions options,
        string? runtimeRoot,
        PackagedRuntimeBundle? packagedBundle)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["NEVAXIS_FACTOS__AUTHENTICATION__MODE"] = "SessionCookie",
        };

        if (packagedBundle is not null)
        {
            RuntimePaths.MergePackagedConfiguration(environment, packagedBundle, overwrite: true);
        }
        else if (!string.IsNullOrWhiteSpace(runtimeRoot))
        {
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetEnvironmentFilePath(runtimeRoot),
                overwrite: true);
            RuntimePaths.MergeEnvironmentFile(
                environment,
                RuntimePaths.GetLocalSecretsEnvironmentFilePath(runtimeRoot),
                overwrite: true);
        }

        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "DevelopmentSaasIdentity__");
        RuntimePaths.CopyProcessEnvironmentPrefix(environment, "NEVAXIS_FACTOS__");

        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__ENABLED", "true");
        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__HOST", "127.0.0.1");
        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__PORT", "5432");
        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__DATABASE", "crevafactos_local");
        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__USERNAME", "crevafactos_runtime");
        SetIfMissing(
            environment,
            "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD",
            "local_runtime_only_change_me");
        SetIfMissing(environment, "NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE", "Disable");

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

    private string ResolveNodeExecutable()
    {
        if (_packagedBundle is not null && File.Exists(_packagedBundle.NodeExecutablePath))
        {
            return _packagedBundle.NodeExecutablePath;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var segment in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(segment.Trim(), "node.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("node.exe bulunamadı. Node.js kurulu olmalıdır.");
    }

    private static bool UsesEmbeddedPostgres(PackagedRuntimeBundle bundle)
    {
        var manifest = bundle.TryReadManifest();
        if (manifest?.DatabaseArchitecture is not null)
        {
            return string.Equals(
                manifest.DatabaseArchitecture,
                "EMBEDDED_LOCAL",
                StringComparison.OrdinalIgnoreCase);
        }

        return Directory.Exists(bundle.PostgresBinDirectory);
    }

    private static void ImportPackagedConfiguration(PackagedRuntimeBundle bundle)
    {
        ImportEnvironmentFile(Path.Combine(bundle.ConfigDirectory, "runtime.env"));
        ImportEnvironmentFile(RuntimePaths.GetPackagedMachineSecretsFilePath());
    }

    private static string ResolvePostgresHost() =>
        Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__HOST") ?? "127.0.0.1";

    private static int ResolvePostgresPort()
    {
        var configuredPort = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__PORT");
        return int.TryParse(configuredPort, out var parsedPort) ? parsedPort : 5432;
    }

    private async Task ValidateSchemaVersionIfRequiredAsync(
        Configuration.DesktopRuntimeOptions options,
        string postgresHost,
        int postgresPort,
        CancellationToken cancellationToken)
    {
        if (_packagedBundle is null || !File.Exists(_packagedBundle.ExpectedSchemaVersionPath))
        {
            return;
        }

        var expected = File.ReadAllText(_packagedBundle.ExpectedSchemaVersionPath).Trim();
        if (string.IsNullOrWhiteSpace(expected))
        {
            return;
        }

        var database = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__DATABASE")
            ?? "crevafactos_local";
        var username = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__USERNAME")
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__USERNAME")
            ?? "crevafactos";
        var password = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__PASSWORD")
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__PASSWORD")
            ?? string.Empty;

        var query =
            "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1;";
        string? actual;
        if (_embeddedPostgres is not null)
        {
            actual = await _embeddedPostgres.QueryScalarAsync(
                postgresPort,
                database,
                username,
                password,
                query,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            actual = await QueryPostgresScalarAsync(
                postgresHost,
                postgresPort,
                database,
                username,
                password,
                query,
                cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(actual))
        {
            throw new InvalidOperationException("Veritabanı şema geçmişi bulunamadı.");
        }

        if (!string.Equals(actual.Trim(), expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Veritabanı şema sürümü uyumsuz. Beklenen={expected} Gerçek={actual.Trim()}");
        }

        _diagnostics.Add($"schema-version=matched {expected}");
    }

    private static async Task<string?> QueryPostgresScalarAsync(
        string host,
        int port,
        string database,
        string username,
        string password,
        string query,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add("NevaxisFactOS-local-postgres-1");
        startInfo.ArgumentList.Add("psql");
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(username);
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(database);
        startInfo.ArgumentList.Add("-tA");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        await process.StandardInput.WriteLineAsync(query).ConfigureAwait(false);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0 ? output.Trim() : null;
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
