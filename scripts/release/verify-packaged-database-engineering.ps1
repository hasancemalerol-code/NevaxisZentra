# Engineering smoke: start embedded template from release staging and run DatabaseTool verifiers.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$StagingRoot = "",
    [int]$Port = 55432
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}
if ([string]::IsNullOrWhiteSpace($StagingRoot)) {
    $StagingRoot = Join-Path $RepositoryRoot "release\staging"
}

$templateRoot = Join-Path $StagingRoot "postgres\template"
$postgresBin = Join-Path $StagingRoot "postgres\bin"
$databaseToolProject = Join-Path $RepositoryRoot "tools\NevaxisZentra.DatabaseTool\NevaxisZentra.DatabaseTool.csproj"
$secretsFile = Join-Path $RepositoryRoot "apps\desktop\runtime-packaging\config\release-demo-secrets.env"
$runtimeEnv = Join-Path $StagingRoot "config\runtime.env"

if (-not (Test-Path $templateRoot)) {
    throw "Missing postgres/template under staging."
}

function Import-EnvFile([string]$Path) {
    if (-not (Test-Path $Path)) { return }
    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) { continue }
        $parts = $trimmed.Split("=", 2)
        if ($parts.Count -eq 2) {
            [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
        }
    }
}

Import-EnvFile $runtimeEnv

# The template carries no credentials: generate throwaway ones and apply them with PostgreSQL
# single-user mode, exactly as the installed machine does on first run.
function New-RandomSecret {
    $bytes = New-Object byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return ([Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'))
}
$env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = New-RandomSecret
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = New-RandomSecret

$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = "nevaxiszentra"
$env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME = "crevafactos_runtime"

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-packaged-db-smoke-" + [Guid]::NewGuid().ToString("N"))
$dataDir = Join-Path $tempRoot "data"
$logDir = Join-Path $tempRoot "logs"
New-Item -ItemType Directory -Force -Path $dataDir, $logDir | Out-Null
Copy-Item -Path (Join-Path $templateRoot "*") -Destination $dataDir -Recurse -Force

foreach ($requiredDir in 'pg_commit_ts','pg_dynshmem','pg_notify','pg_replslot','pg_serial','pg_snapshots','pg_stat_tmp','pg_tblspc','pg_twophase','pg_logical\mappings','pg_logical\snapshots','pg_wal\archive_status','pg_wal\summaries') {
    New-Item -ItemType Directory -Force -Path (Join-Path $dataDir $requiredDir) | Out-Null
}
$syncSql = @(
    "ALTER ROLE `"nevaxiszentra`" WITH LOGIN PASSWORD '$($env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD)'",
    "ALTER ROLE `"crevafactos_runtime`" WITH LOGIN PASSWORD '$($env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD)'"
) -join "`n"
$previousErrorAction = $ErrorActionPreference
$ErrorActionPreference = "Continue"   # postgres writes normal LOG lines to stderr
# PS 5.1 prefixes a BOM to text piped into native executables, which breaks the first statement:
# hand the SQL over through a BOM-less file and cmd redirection instead of a pipe.
$syncSqlFile = Join-Path $tempRoot "credential-sync.sql"
[System.IO.File]::WriteAllText($syncSqlFile, $syncSql + "`n", (New-Object System.Text.UTF8Encoding $false))
$syncOutputFile = Join-Path $tempRoot "credential-sync.out"
try {
    $postgresExe = Join-Path $postgresBin "postgres.exe"
    cmd /c "`"$postgresExe`" --single -D `"$dataDir`" -c password_encryption=scram-sha-256 postgres < `"$syncSqlFile`" > `"$syncOutputFile`" 2>&1"
    $syncExit = $LASTEXITCODE
    $syncOutput = Get-Content $syncOutputFile -Raw
}
finally { $ErrorActionPreference = $previousErrorAction; Remove-Item $syncSqlFile -Force -ErrorAction SilentlyContinue }
if ($syncExit -ne 0 -or ($syncOutput | Out-String) -match 'ERROR:') { throw "Single-user credential sync failed (exit $syncExit)." }
Write-Host "Credential sync via single-user mode: OK"

$pgCtl = Join-Path $postgresBin "pg_ctl.exe"
$psql = Join-Path $postgresBin "psql.exe"
$confPath = Join-Path $dataDir "postgresql.conf"
if (Test-Path $confPath) {
    $lines = Get-Content $confPath
    $lines = $lines | ForEach-Object {
        if ($_ -match '^\s*port\s*=') { "port = $Port" } else { $_ }
    }
    if (-not ($lines -match '^\s*port\s*=')) { $lines += "port = $Port" }
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($confPath, $lines, $utf8NoBom)
}

$logFile = Join-Path $logDir "postgresql.log"
& $pgCtl start -D $dataDir -l $logFile -w -o "-p $Port"
if ($LASTEXITCODE -ne 0) { throw "Failed to start packaged template cluster." }

try {
    $env:NEVAXIS_FACTOS__PERSISTENCE__HOST = "127.0.0.1"
    $env:NEVAXIS_FACTOS__PERSISTENCE__PORT = "$Port"
    $env:NEVAXIS_FACTOS__PERSISTENCE__HOST = "127.0.0.1"
    $env:NEVAXIS_FACTOS__PERSISTENCE__PORT = "$Port"
    $env:NEVAXIS_FACTOS__MIGRATIONS__HOST = "127.0.0.1"
    $env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$Port"

    & dotnet run --project $databaseToolProject -c Release --no-launch-profile -- verify
    if ($LASTEXITCODE -ne 0) { throw "DatabaseTool verify failed." }

    & dotnet run --project $databaseToolProject -c Release --no-launch-profile -- tenant-isolation-verify
    if ($LASTEXITCODE -ne 0) { throw "tenant-isolation-verify failed." }

    & dotnet run --project $databaseToolProject -c Release --no-launch-profile -- permission-authorization-verify
    if ($LASTEXITCODE -ne 0) { throw "permission-authorization-verify failed." }

    $env:PGPASSWORD = $env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
    $rls = & $psql -h 127.0.0.1 -p $Port -U nevaxiszentra -d crevafactos_local -tA -c "SELECT relforcerowsecurity FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relname = 'tenants';"
    if ($rls.Trim() -ne "t") { throw "FORCE RLS not enabled on tenants." }

    Write-Host "PACKAGED_DATABASE_ENGINEERING_SMOKE: PASS"
}
finally {
    & $pgCtl stop -D $dataDir -m fast 2>$null | Out-Null
    if (Test-Path $tempRoot) { Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue }
}
