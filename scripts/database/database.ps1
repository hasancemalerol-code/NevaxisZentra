[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('config', 'verify', 'state', 'tenant-isolation-verify', 'authentication-membership-verify', 'authentication-membership-bootstrap', 'authentication-membership-cleanup', 'permission-authorization-verify', 'permission-authorization-bootstrap', 'permission-authorization-cleanup', 'audit-trail-verify', 'audit-trail-bootstrap', 'audit-trail-cleanup', 'development-pilot-migration-prerequisites', 'development-pilot-operator-access-ensure', 'migrations-list', 'migrations-add', 'migrations-apply', 'migrations-rollback', 'migrations-script')]
    [string]$Operation,

    [Parameter(Position = 1)]
    [string]$MigrationName
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$composeFile = Join-Path $repositoryRoot 'infrastructure\compose.yaml'
$migrationProject = Join-Path $repositoryRoot 'packages\NevaxisZentra.Persistence.Migrations\NevaxisZentra.Persistence.Migrations.csproj'
$databaseToolProject = Join-Path $repositoryRoot 'tools\NevaxisZentra.DatabaseTool\NevaxisZentra.DatabaseTool.csproj'

function Import-EnvironmentFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) {
            continue
        }

        $parts = $trimmed.Split('=', 2)
        if ($parts.Count -eq 2 -and -not [Environment]::GetEnvironmentVariable($parts[0], 'Process')) {
            [Environment]::SetEnvironmentVariable($parts[0], $parts[1], 'Process')
        }
    }
}

function Set-PersistenceValue {
    param([string]$Suffix, [string]$LegacyName, [string]$DefaultValue)

    $name = "NEVAXIS_FACTOS__PERSISTENCE__$Suffix"
    if ([Environment]::GetEnvironmentVariable($name, 'Process')) {
        return
    }

    $value = [Environment]::GetEnvironmentVariable($LegacyName, 'Process')
    if (-not $value) {
        $value = $DefaultValue
    }

    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

function Get-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $windowsPath = 'C:\Program Files\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $windowsPath) {
        return $windowsPath
    }

    throw 'The .NET SDK is unavailable. Install the SDK selected by global.json and ensure dotnet is on PATH.'
}

function Assert-PostgreSqlContainerHealthy {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker is unavailable. Install Docker Desktop and ensure docker is on PATH.'
    }

    & docker info --format '{{.ServerVersion}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker is unavailable. Start Docker Desktop before running this database command.'
    }

    & docker compose -f $composeFile exec -T postgres pg_isready -U $env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME -d $env:NEVAXIS_FACTOS__PERSISTENCE__DATABASE *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'The PostgreSQL container is absent or unhealthy. Run pnpm infra:start and pnpm infra:status.'
    }
}

function Invoke-DotNet {
    param([string[]]$Arguments)

    & $script:dotnetPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

Import-EnvironmentFile (Join-Path $repositoryRoot 'infrastructure\.env')
Set-PersistenceValue 'ENABLED' 'NEVAXIS_FACTOS_PERSISTENCE_ENABLED' 'true'
Set-PersistenceValue 'HOST' 'NEVAXIS_FACTOS_POSTGRES_HOST' '127.0.0.1'
Set-PersistenceValue 'PORT' 'NEVAXIS_FACTOS_POSTGRES_PORT' '5432'
Set-PersistenceValue 'DATABASE' 'NEVAXIS_FACTOS_POSTGRES_DATABASE' 'crevafactos_local'
Set-PersistenceValue 'USERNAME' 'NEVAXIS_FACTOS_PERSISTENCE_USERNAME' 'crevafactos_runtime'
Set-PersistenceValue 'PASSWORD' 'NEVAXIS_FACTOS_PERSISTENCE_PASSWORD' 'local_runtime_only_change_me'
Set-PersistenceValue 'SSL_MODE' 'NEVAXIS_FACTOS_PERSISTENCE_SSL_MODE' 'Disable'
Set-PersistenceValue 'CONNECTION_TIMEOUT_SECONDS' 'NEVAXIS_FACTOS_PERSISTENCE_CONNECTION_TIMEOUT_SECONDS' '15'
Set-PersistenceValue 'COMMAND_TIMEOUT_SECONDS' 'NEVAXIS_FACTOS_PERSISTENCE_COMMAND_TIMEOUT_SECONDS' '30'
Set-PersistenceValue 'POOLING' 'NEVAXIS_FACTOS_PERSISTENCE_POOLING' 'true'

function Set-MigrationValue {
    param([string]$Suffix, [string]$LegacyName, [string]$DefaultValue)

    $name = "NEVAXIS_FACTOS__MIGRATIONS__$Suffix"
    if ([Environment]::GetEnvironmentVariable($name, 'Process')) {
        return
    }

    $value = [Environment]::GetEnvironmentVariable($LegacyName, 'Process')
    if (-not $value) {
        $value = $DefaultValue
    }

    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

Set-MigrationValue 'HOST' 'NEVAXIS_FACTOS_POSTGRES_HOST' '127.0.0.1'
Set-MigrationValue 'PORT' 'NEVAXIS_FACTOS_POSTGRES_PORT' '5432'
Set-MigrationValue 'DATABASE' 'NEVAXIS_FACTOS_POSTGRES_DATABASE' 'crevafactos_local'
Set-MigrationValue 'USERNAME' 'NEVAXIS_FACTOS_POSTGRES_USERNAME' 'crevafactos'
Set-MigrationValue 'PASSWORD' 'NEVAXIS_FACTOS_POSTGRES_PASSWORD' 'local_dev_only_change_me'
Set-MigrationValue 'SSL_MODE' 'NEVAXIS_FACTOS_MIGRATIONS_SSL_MODE' 'Disable'
Set-MigrationValue 'CONNECTION_TIMEOUT_SECONDS' 'NEVAXIS_FACTOS_MIGRATIONS_CONNECTION_TIMEOUT_SECONDS' '15'
Set-MigrationValue 'COMMAND_TIMEOUT_SECONDS' 'NEVAXIS_FACTOS_MIGRATIONS_COMMAND_TIMEOUT_SECONDS' '30'

$script:dotnetPath = Get-DotNetPath
$efPrefix = @('tool', 'run', 'dotnet-ef', '--')
$efOptions = @(
    '--project', $migrationProject,
    '--startup-project', $migrationProject,
    '--context', 'NevaxisZentraDbContext'
)

Push-Location $repositoryRoot
try {
    switch ($Operation) {
        'config' {
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'verify', '--configuration-only')
        }
        'verify' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'verify')
        }
        'state' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'state')
        }
        'tenant-isolation-verify' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'tenant-isolation-verify')
        }
        'authentication-membership-verify' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'authentication-membership-verify')
        }
        'authentication-membership-bootstrap' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'authentication-membership-bootstrap')
        }
        'authentication-membership-cleanup' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'authentication-membership-cleanup')
        }
        'permission-authorization-verify' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'permission-authorization-verify')
        }
        'permission-authorization-bootstrap' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'permission-authorization-bootstrap')
        }
        'permission-authorization-cleanup' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'permission-authorization-cleanup')
        }
        'audit-trail-verify' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'audit-trail-verify')
        }
        'audit-trail-bootstrap' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'audit-trail-bootstrap')
        }
        'audit-trail-cleanup' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'audit-trail-cleanup')
        }
        'development-pilot-migration-prerequisites' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'development-pilot-migration-prerequisites')
        }
        'development-pilot-operator-access-ensure' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet @('run', '--project', $databaseToolProject, '--no-launch-profile', '--', 'development-pilot-operator-access-ensure')
        }
        'migrations-list' {
            Invoke-DotNet ($efPrefix + @('migrations', 'list') + $efOptions)
        }
        'migrations-add' {
            if (-not $MigrationName -or $MigrationName -notmatch '^[A-Za-z][A-Za-z0-9]*$') {
                throw 'Provide an explicit PascalCase migration name containing only letters and digits.'
            }
            Invoke-DotNet ($efPrefix + @('migrations', 'add', $MigrationName) + $efOptions)
        }
        'migrations-apply' {
            Assert-PostgreSqlContainerHealthy
            Invoke-DotNet ($efPrefix + @('database', 'update') + $efOptions)
        }
        'migrations-rollback' {
            Assert-PostgreSqlContainerHealthy
            Write-Warning 'Destructive operation: rolling the application database back to migration zero.'
            Invoke-DotNet ($efPrefix + @('database', 'update', '0') + $efOptions)
        }
        'migrations-script' {
            $outputPath = Join-Path $repositoryRoot 'artifacts\database\NevaxisZentra.idempotent.sql'
            New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
            Invoke-DotNet ($efPrefix + @('migrations', 'script', '--idempotent', '--output', $outputPath) + $efOptions)
            Write-Host "Generated idempotent SQL: $outputPath"
        }
    }
}
finally {
    Pop-Location
}
