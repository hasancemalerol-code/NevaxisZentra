# Builds a pre-migrated and pre-seeded PostgreSQL template for Windows release.
# Uses the developer workstation's fully migrated FactOS database as source-of-truth.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$PostgresBinDirectory = "",
    [string]$TemplateOutputDirectory = "",
    [int]$Port = 55432
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$seedSqlPath = Join-Path (Split-Path $TemplateOutputDirectory -Parent) "factos-release-seed.sql"
$infraEnvFile = Join-Path $RepositoryRoot "infrastructure\.env"
$secretsFile = Join-Path $RepositoryRoot "apps\desktop\runtime-packaging\config\release-demo-secrets.env"

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

Import-EnvFile $infraEnvFile

$sourceHost = if ($env:NEVAXIS_FACTOS__MIGRATIONS__HOST) { $env:NEVAXIS_FACTOS__MIGRATIONS__HOST } elseif ($env:NEVAXIS_FACTOS_POSTGRES_HOST) { $env:NEVAXIS_FACTOS_POSTGRES_HOST } else { "127.0.0.1" }
$sourcePort = if ($env:NEVAXIS_FACTOS__MIGRATIONS__PORT) { $env:NEVAXIS_FACTOS__MIGRATIONS__PORT } elseif ($env:NEVAXIS_FACTOS_POSTGRES_PORT) { $env:NEVAXIS_FACTOS_POSTGRES_PORT } else { "5432" }
$sourceDatabase = if ($env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE) { $env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE } elseif ($env:NEVAXIS_FACTOS_POSTGRES_DATABASE) { $env:NEVAXIS_FACTOS_POSTGRES_DATABASE } else { "crevafactos_local" }
$migrationUser = if ($env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME) { $env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME } elseif ($env:NEVAXIS_FACTOS_POSTGRES_USERNAME) { $env:NEVAXIS_FACTOS_POSTGRES_USERNAME } else { "crevafactos" }
$migrationPassword = if ($env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD) { $env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD } elseif ($env:NEVAXIS_FACTOS_POSTGRES_PASSWORD) { $env:NEVAXIS_FACTOS_POSTGRES_PASSWORD } else { "local_dev_only_change_me" }
$persistenceUser = if ($env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME) { $env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME } else { "crevafactos_runtime" }
$persistencePassword = if ($env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD) { $env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD } else { "local_runtime_only_change_me" }

$psql = Join-Path $PostgresBinDirectory "psql.exe"
$pgDump = Join-Path $PostgresBinDirectory "pg_dump.exe"
if (-not (Test-Path $psql) -or -not (Test-Path $pgDump)) {
    throw "Bundled PostgreSQL client tools not found in $PostgresBinDirectory"
}

function Test-PostgresCredential([hashtable]$Candidate, [switch]$RequireDumpPrivileges) {
    if ([string]::IsNullOrWhiteSpace($Candidate.User) -or [string]::IsNullOrWhiteSpace($Candidate.Password)) {
        return $false
    }
    $env:PGPASSWORD = $Candidate.Password
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($RequireDumpPrivileges) {
            $probeFile = [System.IO.Path]::GetTempFileName()
            try {
                & $pgDump -h $sourceHost -p $sourcePort -U $Candidate.User -d $sourceDatabase --schema-only --table=public.tenants --file $probeFile 2>$null | Out-Null
                return $LASTEXITCODE -eq 0
            }
            finally {
                if (Test-Path $probeFile) { Remove-Item -Force $probeFile }
            }
        }
        & $psql -h $sourceHost -p $sourcePort -U $Candidate.User -d $sourceDatabase -v ON_ERROR_STOP=1 -tA -c "SELECT 1;" 2>$null | Out-Null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

function Resolve-SourceCredential([switch]$RequireDumpPrivileges) {
    $candidates = @(
        @{ User = $env:NEVAXIS_FACTOS_POSTGRES_USERNAME; Password = $env:NEVAXIS_FACTOS_POSTGRES_PASSWORD },
        @{ User = $migrationUser; Password = $migrationPassword },
        @{ User = "crevafactos"; Password = "local_dev_only_change_me" },
        @{ User = "NevaxisZentra"; Password = "local_dev_only_change_me" },
        @{ User = $persistenceUser; Password = $persistencePassword }
    )
    foreach ($candidate in $candidates) {
        if (Test-PostgresCredential -Candidate $candidate -RequireDumpPrivileges:$RequireDumpPrivileges) {
            return $candidate
        }
    }
    if ($RequireDumpPrivileges) {
        throw "No configured database credential can perform pg_dump against the source database."
    }
    throw "Source database is unreachable with configured persistence/migration credentials."
}

Write-Host "Verifying source FactOS database at ${sourceHost}:${sourcePort}/${sourceDatabase}..."
$sourceCredential = Resolve-SourceCredential
$sourceUser = $sourceCredential.User
$sourcePassword = $sourceCredential.Password
$env:PGPASSWORD = $sourcePassword

$tableCountRaw = & $psql -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase -tA -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';"
$tableCount = [int]($tableCountRaw.Trim())
if ($tableCount -lt 50) {
    throw "Source database has insufficient schema (public tables=$tableCount). Apply migrations and bootstrap before building release template."
}

foreach ($requiredTable in @("golden_demo_production_events", "golden_demo_customs_file_outcomes", "tenants")) {
    $existsRaw = & $psql -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase -tA -c "SELECT to_regclass('public.$requiredTable') IS NOT NULL;"
    if ($existsRaw.Trim() -ne "t") {
        throw "Source database is missing required table: $requiredTable"
    }
}

Write-Host "Creating PostgreSQL seed dump from source database ($tableCount public tables)..."
$dumpCredential = Resolve-SourceCredential -RequireDumpPrivileges
$sourceUser = $dumpCredential.User
$sourcePassword = $dumpCredential.Password
$env:PGPASSWORD = $sourcePassword
New-Item -ItemType Directory -Force -Path (Split-Path $seedSqlPath -Parent) | Out-Null
& $pgDump -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase --no-owner --no-privileges --file $seedSqlPath
if ($LASTEXITCODE -ne 0) { throw "pg_dump failed." }
$seedSize = (Get-Item $seedSqlPath).Length
if ($seedSize -lt 1048576) {
    throw "Seed SQL dump is missing or too small ($seedSize bytes). Expected migrated+seeded database content."
}

Import-EnvFile $secretsFile
$runtimePassword = $env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD
$migrationPassword = $env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
if ([string]::IsNullOrWhiteSpace($runtimePassword) -or [string]::IsNullOrWhiteSpace($migrationPassword)) {
    throw "Release demo secrets must define persistence and migration passwords."
}

Write-Host "Building Windows PostgreSQL template cluster..."
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-release-db-" + [Guid]::NewGuid().ToString("N"))
$dataDir = Join-Path $tempRoot "data"
$logDir = Join-Path $tempRoot "logs"
New-Item -ItemType Directory -Force -Path $dataDir, $logDir, $TemplateOutputDirectory | Out-Null

$initdb = Join-Path $PostgresBinDirectory "initdb.exe"
$pgCtl = Join-Path $PostgresBinDirectory "pg_ctl.exe"
$env:PATH = "$PostgresBinDirectory;$env:PATH"

& $initdb -D $dataDir -U postgres --encoding=UTF8 --locale=C --auth-local=trust --auth-host=scram-sha-256
if ($LASTEXITCODE -ne 0) { throw "initdb failed." }

$confPath = Join-Path $dataDir "postgresql.conf"
$confLines = @(Get-Content $confPath) + @("port = $Port", "listen_addresses = '127.0.0.1'")
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllLines($confPath, $confLines, $utf8NoBom)

$hbaPath = Join-Path $dataDir "pg_hba.conf"
$buildHba = @(
    "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
    "local   all             all                                     trust",
    "host    all             all             127.0.0.1/32            trust",
    "host    all             all             ::1/128                 trust"
)
[System.IO.File]::WriteAllLines($hbaPath, $buildHba, $utf8NoBom)

$logFile = Join-Path $logDir "postgresql.log"
& $pgCtl start -D $dataDir -l $logFile -w -o "-p $Port"
if ($LASTEXITCODE -ne 0) { throw "pg_ctl start failed." }

function Get-PsqlRoleName([string]$User) {
    if ($User -eq "NevaxisZentra") {
        return "nevaxiszentra"
    }
    return $User
}

function Invoke-Psql([string]$User, [string]$Database, [string]$Command) {
    $User = Get-PsqlRoleName $User
    $previousUser = $env:PGUSER
    $env:PGUSER = $User
    try {
        & $psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -p $Port -d $Database -c $Command
        if ($LASTEXITCODE -ne 0) {
            throw "psql failed for user '$User'."
        }
    }
    finally {
        if ($null -eq $previousUser) {
            Remove-Item Env:PGUSER -ErrorAction SilentlyContinue
        }
        else {
            $env:PGUSER = $previousUser
        }
    }
}

function Invoke-PsqlFile([string]$User, [string]$Database, [string]$FilePath) {
    $User = Get-PsqlRoleName $User
    $previousUser = $env:PGUSER
    $env:PGUSER = $User
    try {
        & $psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -p $Port -d $Database -f $FilePath
        if ($LASTEXITCODE -ne 0) {
            throw "psql file restore failed for user '$User'."
        }
    }
    finally {
        if ($null -eq $previousUser) {
            Remove-Item Env:PGUSER -ErrorAction SilentlyContinue
        }
        else {
            $env:PGUSER = $previousUser
        }
    }
}

try {
    Invoke-Psql -User "postgres" -Database "postgres" -Command "CREATE ROLE NevaxisZentra LOGIN SUPERUSER PASSWORD '$migrationPassword';"
    Invoke-Psql -User "postgres" -Database "postgres" -Command "CREATE ROLE crevafactos_runtime LOGIN PASSWORD '$runtimePassword';"
    Invoke-Psql -User "postgres" -Database "postgres" -Command "CREATE DATABASE crevafactos_local OWNER NevaxisZentra;"
    Invoke-PsqlFile -User "NevaxisZentra" -Database "crevafactos_local" -FilePath $seedSqlPath

    $productionHba = @(
        "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
        "local   all             all                                     trust",
        "host    all             all             127.0.0.1/32            scram-sha-256",
        "host    all             all             ::1/128                 scram-sha-256"
    )
    [System.IO.File]::WriteAllLines($hbaPath, $productionHba, $utf8NoBom)
}
finally {
    & $pgCtl stop -D $dataDir -m fast
}

if (Test-Path $TemplateOutputDirectory) {
    Remove-Item -Recurse -Force $TemplateOutputDirectory
}
New-Item -ItemType Directory -Force -Path $TemplateOutputDirectory | Out-Null
Get-ChildItem $dataDir -Force | Copy-Item -Destination $TemplateOutputDirectory -Recurse -Force
Write-Host "Database template ready at $TemplateOutputDirectory"
Write-Host "Seed SQL retained at $seedSqlPath"
