# Credential hygiene gate for a release payload (staging directory or MSI administrative image root).
# Fails when a reusable credential could ship to customers. Secret VALUES are never printed:
# only file names and counts are reported.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PayloadRoot,
    [ValidateSet("DEMO", "PRESENTATION", "PRODUCTION")]
    [string]$DeploymentChannel = "PRODUCTION",
    # Local files whose password/secret VALUES must not appear anywhere in the payload.
    [string[]]$KnownSecretSources = @(),
    [string]$EvidencePath = "",
    # Demo usernames/passwords that must never appear in a PRODUCTION payload ('|' separated or an array).
    [string[]]$DemoCredentialLiterals = @(
        'NevaxisDemo2026!',
        'admin@demo.nevaxiszentra.local|active-user@dev.local|deactivated-user@dev.local|no-membership@dev.local|invited-user@dev.local'
    )
)

$ErrorActionPreference = "Stop"
$PayloadRoot = (Resolve-Path -LiteralPath $PayloadRoot).Path
$failures = New-Object System.Collections.Generic.List[string]
$notes = New-Object System.Collections.Generic.List[string]

# 1. Files that must not exist ------------------------------------------------------------------
if ($DeploymentChannel -eq "PRODUCTION" -and (Test-Path -LiteralPath (Join-Path $PayloadRoot "config\release-demo-secrets.env"))) {
    $failures.Add("config/release-demo-secrets.env is packaged in a PRODUCTION payload")
}
$devSettings = @(Get-ChildItem -LiteralPath $PayloadRoot -Recurse -File -Filter "appsettings.Development.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\node_modules\\' })
foreach ($file in $devSettings) {
    # PRODUCTION must not ship it at all; other channels may keep a sanitized copy without credentials.
    $hasCredential = Select-String -LiteralPath $file.FullName -Pattern '"(Persistence|Password)"' -Quiet
    if ($DeploymentChannel -eq "PRODUCTION" -or $hasCredential) {
        $failures.Add("appsettings.Development.json packaged: " + $file.FullName.Substring($PayloadRoot.Length + 1))
    }
}

# 2. Shipped config must carry no credentials -------------------------------------------------------
$secretKeyPattern = '(PASSWORD|SECRET|TOKEN|PRIVATE_KEY|API_KEY)'
$configDir = Join-Path $PayloadRoot "config"
$configChecked = 0
if (Test-Path -LiteralPath $configDir) {
    foreach ($file in Get-ChildItem -LiteralPath $configDir -File) {
        $configChecked++
        foreach ($line in Get-Content -LiteralPath $file.FullName) {
            if ($line -match '^\s*#' -or $line -notmatch '=') { continue }
            $key, $value = $line.Split('=', 2)
            if ($key -match $secretKeyPattern -and -not [string]::IsNullOrWhiteSpace($value)) {
                $failures.Add("config/$($file.Name) carries a non-empty secret-like key: $($key.Trim())")
            }
        }
    }
}

# 3. Known secret values must not appear in shipped files -------------------------------------------
Add-Type -TypeDefinition @"
using System; using System.IO; using System.Text; using System.Collections.Generic;
public static class ZentraSecretScanner {
    public static List<string> Find(string root, string[] relativeDirs, string[] secrets) {
        return Find(root, relativeDirs, secrets, new string[0]);
    }
    public static List<string> Find(string root, string[] relativeDirs, string[] secrets, string[] skipExtensions) {
        var needles = new List<byte[]>();
        foreach (var s in secrets) { needles.Add(Encoding.UTF8.GetBytes(s)); needles.Add(Encoding.Unicode.GetBytes(s)); }
        var hits = new List<string>();
        foreach (var rel in relativeDirs) {
            var dir = Path.Combine(root, rel);
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)) {
                FileInfo info = new FileInfo(file);
                bool skip = false;
                foreach (var ext in skipExtensions) { if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) { skip = true; break; } }
                if (skip) continue;
                if (info.Length == 0 || info.Length > 64L * 1024 * 1024) continue;
                byte[] data;
                try { data = File.ReadAllBytes(file); } catch { continue; }
                foreach (var n in needles) { if (IndexOf(data, n) >= 0) { hits.Add(file.Substring(root.Length + 1)); break; } }
            }
        }
        return hits;
    }
    static int IndexOf(byte[] hay, byte[] needle) {
        if (needle.Length == 0 || hay.Length < needle.Length) return -1;
        int last = hay.Length - needle.Length;
        for (int i = 0; i <= last; i++) {
            if (hay[i] != needle[0]) continue;
            int j = 1; for (; j < needle.Length; j++) if (hay[i + j] != needle[j]) break;
            if (j == needle.Length) return i;
        }
        return -1;
    }
}
"@

$secrets = New-Object System.Collections.Generic.HashSet[string]
$KnownSecretSources = @($KnownSecretSources | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
foreach ($source in $KnownSecretSources) {
    if (-not (Test-Path -LiteralPath $source)) { continue }
    foreach ($line in Get-Content -LiteralPath $source) {
        if ($line -match '^\s*#' -or $line -notmatch '=') { continue }
        $key, $value = $line.Split('=', 2)
        $value = $value.Trim().Trim('"')
        if ($key -match $secretKeyPattern -and $key -notmatch 'DevelopmentSaasIdentity__' -and $value.Length -ge 8) { [void]$secrets.Add($value) }
    }
}
foreach ($placeholder in 'local_dev_only_change_me', 'local_runtime_only_change_me') { [void]$secrets.Remove($placeholder) }
$scopeDirs = @('config', 'api', 'tools', 'migrations', 'manifests', 'customer-import-templates', 'desktop', 'web\apps\web', 'postgres\template')
$secretHits = @()
if ($secrets.Count -gt 0) {
    $secretHits = @([ZentraSecretScanner]::Find($PayloadRoot, $scopeDirs, [string[]]$secrets))
    foreach ($hit in $secretHits) { $failures.Add("known reusable credential value found in: $hit") }
}

# In-code development default passwords (dev placeholders). PRODUCTION builds compile them out
# (ZENTRA_PRODUCTION_BUILD), so any occurrence in a PRODUCTION payload - binary or text - is a failure.
# Other channels may still carry them (they are not valid credentials on a provisioned install).
$placeholders = [string[]]@('local_dev_only_change_me', 'local_runtime_only_change_me')
$placeholderScope = @('api', 'desktop', 'tools', 'config', 'manifests', 'migrations', 'customer-import-templates', 'web', 'node')
$placeholderHits = @()
if ($DeploymentChannel -eq "PRODUCTION") {
    $placeholderHits = @([ZentraSecretScanner]::Find($PayloadRoot, $placeholderScope, $placeholders))
    foreach ($hit in $placeholderHits) { $failures.Add("development default password literal found in production artifact: $hit") }
}
else {
    $textHits = @([ZentraSecretScanner]::Find($PayloadRoot, $scopeDirs, $placeholders, [string[]]@('.dll', '.exe', '.pdb')))
    foreach ($hit in $textHits) { $failures.Add("development placeholder credential found in text/config: $hit") }
}
# 3b. PRODUCTION binaries/artifacts must not contain demo usernames or passwords ---------------------
# Demo identities exist only in Development/Demo builds (compiled out via ZENTRA_PRODUCTION_BUILD).
$demoHits = @()
if ($DeploymentChannel -eq "PRODUCTION") {
    $demoLiterals = [string[]]@($DemoCredentialLiterals | ForEach-Object { $_ -split '\|' } | Where-Object { $_ })
    $demoScope = @('api', 'desktop', 'tools', 'config', 'manifests', 'migrations', 'customer-import-templates', 'web', 'node')
    $demoHits = @([ZentraSecretScanner]::Find($PayloadRoot, $demoScope, $demoLiterals))
    foreach ($hit in $demoHits) { $failures.Add("demo credential literal found in production artifact: $hit") }
}

# 4. The embedded template must contain no role password verifier -------------------------------------
$verifierFiles = @()
$template = Join-Path $PayloadRoot "postgres\template"
if (Test-Path -LiteralPath $template) {
    $verifierFiles = @([ZentraSecretScanner]::Find($PayloadRoot, @('postgres\template\global'), [string[]]@('SCRAM-SHA-256$', 'md5')) |
        Where-Object { $_ -match 'global\\1260$' })
    foreach ($hit in $verifierFiles) { $failures.Add("template role catalog contains a password verifier: $hit") }
} else {
    $notes.Add("postgres/template absent (engineering payload?)")
}

$summary = [ordered]@{
    payloadRoot            = $PayloadRoot
    deploymentChannel      = $DeploymentChannel
    configFilesChecked     = $configChecked
    knownSecretValuesTried = $secrets.Count
    secretValueHits        = $secretHits.Count
    demoCredentialHits     = $demoHits.Count
    placeholderHits        = $placeholderHits.Count
    templateVerifierHits   = $verifierFiles.Count
    failures               = $failures.Count
}
if ($EvidencePath) { ($summary | ConvertTo-Json) | Set-Content -LiteralPath $EvidencePath -Encoding UTF8 }

foreach ($note in $notes) { Write-Host "note: $note" }
if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "CREDENTIAL_SCAN: FAIL ($($failures.Count) finding(s))"
}
Write-Host "CREDENTIAL_SCAN: PASS (config files checked=$configChecked, known secret values tried=$($secrets.Count), hits=0, demo credential literals=0, dev default passwords=0, template verifiers=0)"
