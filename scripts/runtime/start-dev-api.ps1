# Loads infrastructure/.env (when present) and starts the NevaxisFactOS API for local development.
# Matches the Import-EnvironmentFile contract used by scripts/database/database.ps1.

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotNetArgs
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path $repositoryRoot 'apps\api\NevaxisFactOS.Api.csproj'

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

Import-EnvironmentFile (Join-Path $repositoryRoot 'infrastructure\.env')

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

Set-PersistenceValue 'ENABLED' 'NEVAXIS_FACTOS_PERSISTENCE_ENABLED' 'true'
Set-PersistenceValue 'HOST' 'NEVAXIS_FACTOS_POSTGRES_HOST' '127.0.0.1'
Set-PersistenceValue 'PORT' 'NEVAXIS_FACTOS_POSTGRES_PORT' '5432'
Set-PersistenceValue 'DATABASE' 'NEVAXIS_FACTOS_POSTGRES_DATABASE' 'crevafactos_local'
Set-PersistenceValue 'USERNAME' 'NEVAXIS_FACTOS_PERSISTENCE_USERNAME' 'crevafactos_runtime'
Set-PersistenceValue 'PASSWORD' 'NEVAXIS_FACTOS_PERSISTENCE_PASSWORD' 'local_runtime_only_change_me'
Set-PersistenceValue 'SSL_MODE' 'NEVAXIS_FACTOS_PERSISTENCE_SSL_MODE' 'Disable'

Push-Location $repositoryRoot
try {
    if ($DotNetArgs -and $DotNetArgs.Count -gt 0) {
        & dotnet run --project $apiProject @DotNetArgs
    }
    else {
        & dotnet run --project $apiProject --launch-profile http
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
