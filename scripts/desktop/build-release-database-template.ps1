# Builds an empty customer PostgreSQL template (schema + migration history only, no business rows).

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

$migrationsDir = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence.Migrations\Migrations"
# A migration is registered when a [Migration("<id>")] attribute exists in its
# Designer file or, for hand-written migrations, inline in the migration file.
$registeredMigrations = Get-ChildItem $migrationsDir -Filter "*.cs" |
    Where-Object { $_.Name -notmatch 'Snapshot' -and (Select-String -LiteralPath $_.FullName -Pattern '\[Migration\("' -Quiet) } |
    ForEach-Object { $_.BaseName -replace '\.Designer$','' } |
    Select-Object -Unique
$expectedMigrationId = Get-ChildItem $migrationsDir -Filter "*.cs" |
    Where-Object { $_.Name -notmatch 'Designer|Snapshot' -and $registeredMigrations -contains $_.BaseName } |
    Sort-Object Name -Descending |
    Select-Object -First 1 -ExpandProperty BaseName

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

$psql = Join-Path $PostgresBinDirectory "psql.exe"
$pgDump = Join-Path $PostgresBinDirectory "pg_dump.exe"
if (-not (Test-Path $psql) -or -not (Test-Path $pgDump)) {
    throw "Bundled PostgreSQL client tools not found in $PostgresBinDirectory"
}

function Test-PostgresCredential([hashtable]$Candidate) {
    if ([string]::IsNullOrWhiteSpace($Candidate.User) -or [string]::IsNullOrWhiteSpace($Candidate.Password)) {
        return $false
    }
    $env:PGPASSWORD = $Candidate.Password
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $psql -h $sourceHost -p $sourcePort -U $Candidate.User -d $sourceDatabase -v ON_ERROR_STOP=1 -tA -c "SELECT 1;" 2>$null | Out-Null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

function Resolve-SourceCredential() {
    $candidates = @(
        @{ User = $env:NEVAXIS_FACTOS_POSTGRES_USERNAME; Password = $env:NEVAXIS_FACTOS_POSTGRES_PASSWORD },
        @{ User = "crevafactos"; Password = "local_dev_only_change_me" },
        @{ User = "nevaxiszentra"; Password = "local_dev_only_change_me" },
        @{ User = "NevaxisZentra"; Password = "local_dev_only_change_me" },
        @{ User = "crevafactos_runtime"; Password = "local_runtime_only_change_me" }
    )
    foreach ($candidate in $candidates) {
        if (Test-PostgresCredential -Candidate $candidate) {
            return $candidate
        }
    }
    throw "Source database is unreachable. Start local PostgreSQL and apply migrations before building the release template."
}

# No credential is baked into the template: roles are created without a password (NULL verifier)
# and the installed machine sets unique passwords on first run via PostgreSQL single-user mode.

Write-Host "Verifying source schema at ${sourceHost}:${sourcePort}/${sourceDatabase}..."
$sourceCredential = Resolve-SourceCredential
$sourceUser = $sourceCredential.User
$env:PGPASSWORD = $sourceCredential.Password

function Get-LatestAppliedMigrationId([string]$User) {
    $migrationHistorySql = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-migration-history-" + [Guid]::NewGuid().ToString("N") + ".sql")
    Set-Content -Path $migrationHistorySql -Value 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;' -Encoding UTF8
    $latestAppliedRaw = & $psql -h $sourceHost -p $sourcePort -U $User -d $sourceDatabase -tA -f $migrationHistorySql
    Remove-Item -Force $migrationHistorySql
    return $latestAppliedRaw.Trim()
}

$latestApplied = Get-LatestAppliedMigrationId $sourceUser
if ($latestApplied -ne $expectedMigrationId) {
    Write-Host "Source database behind HEAD ($latestApplied -> $expectedMigrationId). Applying pending EF migrations..."
    $migrationProject = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence.Migrations\NevaxisZentra.Persistence.Migrations.csproj"
    $connectionString = "Host=$sourceHost;Port=$sourcePort;Database=$sourceDatabase;Username=$sourceUser;Password=$($sourceCredential.Password);Ssl Mode=Disable"
    Push-Location $RepositoryRoot
    try {
        & dotnet tool run dotnet-ef -- database update --project $migrationProject --startup-project $migrationProject --context NevaxisZentraDbContext --connection $connectionString
        if ($LASTEXITCODE -ne 0) { throw "EF database update failed while bringing source database to HEAD." }
    }
    finally {
        Pop-Location
    }
    $latestApplied = Get-LatestAppliedMigrationId $sourceUser
    if ($latestApplied -ne $expectedMigrationId) {
        throw "Source database migration ($latestApplied) still does not match HEAD ($expectedMigrationId) after EF update."
    }
}

$schemaDump = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-release-schema-" + [Guid]::NewGuid().ToString("N") + ".sql")
$historyDump = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-release-migrations-" + [Guid]::NewGuid().ToString("N") + ".sql")
Write-Host "Exporting schema-only and migration history (no business data)..."
& $pgDump -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase --schema-only --no-owner --file $schemaDump
if ($LASTEXITCODE -ne 0) { throw "pg_dump --schema-only failed." }
$historyExportSql = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-export-history-" + [Guid]::NewGuid().ToString("N") + ".sql")
Set-Content -Path $historyExportSql -Value @'
SELECT format(
    'INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (%L, %L) ON CONFLICT DO NOTHING;',
    "MigrationId",
    "ProductVersion")
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId";
'@ -Encoding UTF8
$historyLines = & $psql -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase -tA -f $historyExportSql
Remove-Item -Force $historyExportSql
if ($LASTEXITCODE -ne 0 -or -not $historyLines) { throw "Failed to export __EFMigrationsHistory rows." }
Set-Content -Path $historyDump -Value $historyLines -Encoding UTF8

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

function Invoke-PsqlFile([string]$User, [string]$Database, [string]$FilePath) {
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

function Invoke-PsqlScalar([string]$User, [string]$Database, [string]$Query) {
    $previousUser = $env:PGUSER
    $env:PGUSER = $User
    $queryFile = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-query-" + [Guid]::NewGuid().ToString("N") + ".sql")
    Set-Content -Path $queryFile -Value $Query -Encoding UTF8
    try {
        $raw = & $psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -p $Port -d $Database -tA -f $queryFile
        if ($LASTEXITCODE -ne 0) {
            throw "psql scalar failed."
        }
        return $raw.Trim()
    }
    finally {
        if (Test-Path $queryFile) { Remove-Item -Force $queryFile }
        if ($null -eq $previousUser) {
            Remove-Item Env:PGUSER -ErrorAction SilentlyContinue
        }
        else {
            $env:PGUSER = $previousUser
        }
    }
}

try {
    $bootstrapSql = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-bootstrap-" + [Guid]::NewGuid().ToString("N") + ".sql")
    @"
CREATE ROLE nevaxiszentra LOGIN SUPERUSER;
CREATE ROLE crevafactos_runtime LOGIN;
CREATE DATABASE crevafactos_local OWNER nevaxiszentra;
"@ | Set-Content -Path $bootstrapSql -Encoding UTF8
    Invoke-PsqlFile -User "postgres" -Database "postgres" -FilePath $bootstrapSql
    Remove-Item -Force $bootstrapSql

    Invoke-PsqlFile -User "nevaxiszentra" -Database "crevafactos_local" -FilePath $schemaDump
    Invoke-PsqlFile -User "nevaxiszentra" -Database "crevafactos_local" -FilePath $historyDump

    foreach ($systemTable in @("permission_definitions")) {
        $systemDataDump = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-system-data-$systemTable-" + [Guid]::NewGuid().ToString("N") + ".sql")
        & $pgDump -h $sourceHost -p $sourcePort -U $sourceUser -d $sourceDatabase --data-only --no-owner --table="public.$systemTable" --file $systemDataDump
        if ($LASTEXITCODE -ne 0) { throw "pg_dump system data failed for $systemTable." }
        Invoke-PsqlFile -User "nevaxiszentra" -Database "crevafactos_local" -FilePath $systemDataDump
        Remove-Item -Force $systemDataDump
    }

    $hardenRolesSql = Join-Path ([System.IO.Path]::GetTempPath()) ("NevaxisZentra-harden-roles-" + [Guid]::NewGuid().ToString("N") + ".sql")
    @"
ALTER ROLE nevaxiszentra LOGIN SUPERUSER;
ALTER ROLE crevafactos_runtime LOGIN;
ALTER ROLE crevafactos_runtime NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
-- Read-only access to migration metadata lets startup verify the schema head without superuser credentials.
GRANT SELECT ON public."__EFMigrationsHistory" TO crevafactos_runtime;
-- MFA enrollment (and re-enrollment) creates and replaces these rows at runtime; migrations only grant read/update.
GRANT INSERT, DELETE ON public.saas_identity_mfa_methods TO crevafactos_runtime;
GRANT INSERT, DELETE ON public.saas_identity_recovery_codes TO crevafactos_runtime;
"@ | Set-Content -Path $hardenRolesSql -Encoding UTF8
    Invoke-PsqlFile -User "nevaxiszentra" -Database "crevafactos_local" -FilePath $hardenRolesSql
    Remove-Item -Force $hardenRolesSql

    $passwordBearingRoles = [int](Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query "SELECT COUNT(*) FROM pg_authid WHERE rolpassword IS NOT NULL;")
    if ($passwordBearingRoles -ne 0) {
        throw "Template must not contain any role password verifier (found $passwordBearingRoles)."
    }
    $runtimePrivilegeGaps = Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query @"
SELECT string_agg(item, ', ') FROM (VALUES
  ('mfa_methods:INSERT', has_table_privilege('crevafactos_runtime','public.saas_identity_mfa_methods','INSERT')),
  ('mfa_methods:DELETE', has_table_privilege('crevafactos_runtime','public.saas_identity_mfa_methods','DELETE')),
  ('recovery_codes:INSERT', has_table_privilege('crevafactos_runtime','public.saas_identity_recovery_codes','INSERT')),
  ('recovery_codes:DELETE', has_table_privilege('crevafactos_runtime','public.saas_identity_recovery_codes','DELETE')),
  ('migration_history:SELECT', has_table_privilege('crevafactos_runtime','public."__EFMigrationsHistory"','SELECT'))
) AS checks(item, granted) WHERE NOT granted;
"@
    if (-not [string]::IsNullOrWhiteSpace($runtimePrivilegeGaps)) {
        throw "Template runtime role is missing required privileges: $runtimePrivilegeGaps"
    }
    Write-Host "Template runtime-privilege contract PASS (MFA enrollment + schema-head read)."
    Write-Host "Template credential contract PASS: no role password verifier is baked into the template."

    $contractPath = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence\Authorization\acceptance-role-contract.json"
    if (-not (Test-Path $contractPath)) {
        throw "Acceptance role contract is missing at $contractPath"
    }
    $acceptanceContract = Get-Content $contractPath -Raw | ConvertFrom-Json
    $missingPermissionKeys = @()
    foreach ($permissionKey in $acceptanceContract.requiredPermissionKeys) {
        $escapedKey = $permissionKey.Replace("'", "''")
        $permissionCount = [int](Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query "SELECT COUNT(*) FROM permission_definitions WHERE key = '$escapedKey' AND status = 'Active';")
        if ($permissionCount -ne 1) {
            $missingPermissionKeys += $permissionKey
        }
    }
    if ($missingPermissionKeys.Count -gt 0) {
        throw "Template acceptance-role catalog contract failed. Missing active permission keys: $($missingPermissionKeys -join ', ')"
    }
    Write-Host "Acceptance role catalog contract PASS ($($acceptanceContract.catalogVersion)). Role grants are applied at customer bootstrap."

    $appliedMigration = Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
    if ($appliedMigration -ne $expectedMigrationId) {
        throw "Template migration mismatch. Expected=$expectedMigrationId Actual=$appliedMigration"
    }

    $demoChecks = @(
        @{ Name = "MIX-LINE-01"; Query = "SELECT COUNT(*) FROM mixed_production_lines WHERE code = 'MIX-LINE-01';" },
        @{ Name = "golden_demo_production_events"; Query = "SELECT COUNT(*) FROM golden_demo_production_events;" },
        @{ Name = "tenants"; Query = "SELECT COUNT(*) FROM tenants;" }
    )
    foreach ($check in $demoChecks) {
        $count = [int](Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query $check.Query)
        if ($count -ne 0) {
            throw "Customer template must not contain $($check.Name) rows (count=$count)."
        }
    }

    $rlsCount = [int](Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query "SELECT COUNT(*) FROM pg_policies WHERE schemaname = 'public' AND tablename = 'tenants';")
    if ($rlsCount -lt 1) {
        throw "Template is missing expected RLS policies on tenants."
    }

    $forceRls = Invoke-PsqlScalar -User "nevaxiszentra" -Database "crevafactos_local" -Query "SELECT relforcerowsecurity FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relname = 'tenants';"
    if ($forceRls -ne "t") {
        throw "Template tenants table does not have FORCE ROW LEVEL SECURITY enabled."
    }

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
    if (Test-Path $schemaDump) { Remove-Item -Force $schemaDump }
    if (Test-Path $historyDump) { Remove-Item -Force $historyDump }
}

if (Test-Path $TemplateOutputDirectory) {
    Remove-Item -Recurse -Force $TemplateOutputDirectory
}
New-Item -ItemType Directory -Force -Path $TemplateOutputDirectory | Out-Null
Get-ChildItem $dataDir -Force | Copy-Item -Destination $TemplateOutputDirectory -Recurse -Force

$templateFiles = (Get-ChildItem $TemplateOutputDirectory -Recurse -File).Count
$templateBytes = (Get-ChildItem $TemplateOutputDirectory -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "Database template ready at $TemplateOutputDirectory"
Write-Host "  files: $templateFiles"
Write-Host "  bytes: $templateBytes"
Write-Host "  schema: $expectedMigrationId"
