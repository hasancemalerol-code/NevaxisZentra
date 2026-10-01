[CmdletBinding()]
param(
    [ValidateRange(10, 600)]
    [int] $TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $PSScriptRoot 'NevaxisCompose.ps1')
$script:composeArguments = @(Get-NevaxisComposeArguments -RepositoryRoot $repositoryRoot)
$composePath = Join-Path $repositoryRoot 'infrastructure\compose.yaml'
$exampleEnvironmentPath = Join-Path $repositoryRoot 'infrastructure\.env.example'
$localEnvironmentPath = Join-Path $repositoryRoot 'infrastructure\.env'

function Read-EnvironmentFile {
    param(
        [Parameter(Mandatory)]
        [string] $Path,
        [Parameter(Mandatory)]
        [hashtable] $Values
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path -Encoding utf8) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf('=')
        if ($separatorIndex -lt 1) {
            throw "Invalid environment entry in ${Path}: $line"
        }

        $name = $trimmed.Substring(0, $separatorIndex).Trim()
        $value = $trimmed.Substring($separatorIndex + 1).Trim()
        $Values[$name] = $value
    }
}

function Get-Setting {
    param(
        [Parameter(Mandatory)]
        [string] $Name,
        [Parameter(Mandatory)]
        [hashtable] $Values
    )

    $processValue = [Environment]::GetEnvironmentVariable($Name)
    if (-not [string]::IsNullOrWhiteSpace($processValue)) {
        return $processValue
    }

    if ($Values.ContainsKey($Name) -and -not [string]::IsNullOrWhiteSpace($Values[$Name])) {
        return $Values[$Name]
    }

    throw "Required local setting '$Name' is missing. Copy infrastructure/.env.example to infrastructure/.env."
}

function Invoke-DockerCommand {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & docker @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [PSCustomObject]@{
        ExitCode = $exitCode
        Output = @($output)
    }
}

function Get-ComposeServiceHealth {
    param(
        [Parameter(Mandatory)]
        [string] $Service
    )

    $result = Invoke-DockerCommand -Arguments (@('compose') + $script:composeArguments + @(
        'ps',
        '--format',
        'json',
        $Service))
    if ($result.ExitCode -ne 0) {
        throw "Unable to inspect '$Service': $($result.Output -join [Environment]::NewLine)"
    }

    if ($result.Output.Count -eq 0) {
        return 'not-created'
    }

    $entry = ($result.Output -join [Environment]::NewLine) | ConvertFrom-Json
    if ($null -eq $entry) {
        return 'not-created'
    }

    return [string] $entry.Health
}

if ($null -eq (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI is unavailable. Install and start Docker Desktop before running infrastructure verification.'
}

$dockerInfo = Invoke-DockerCommand -Arguments @('info', '--format', '{{.ServerVersion}}')
if ($dockerInfo.ExitCode -ne 0) {
    throw "Docker Desktop is unavailable or not running: $($dockerInfo.Output -join [Environment]::NewLine)"
}

$settings = @{}
Read-EnvironmentFile -Path $exampleEnvironmentPath -Values $settings
Read-EnvironmentFile -Path $localEnvironmentPath -Values $settings

$deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
$serviceHealth = @{}

do {
    $serviceHealth['postgres'] = Get-ComposeServiceHealth -Service 'postgres'
    $serviceHealth['rabbitmq'] = Get-ComposeServiceHealth -Service 'rabbitmq'

    if ($serviceHealth['postgres'] -eq 'healthy' -and $serviceHealth['rabbitmq'] -eq 'healthy') {
        break
    }

    Start-Sleep -Seconds 2
} while ([DateTimeOffset]::UtcNow -lt $deadline)

foreach ($service in @('postgres', 'rabbitmq')) {
    if ($serviceHealth[$service] -ne 'healthy') {
        throw "Service '$service' is not healthy after $TimeoutSeconds seconds (state: $($serviceHealth[$service]))."
    }

    Write-Host "[healthy] $service"
}

$managementPort = Get-Setting -Name 'NEVAXIS_FACTOS_RABBITMQ_MANAGEMENT_PORT' -Values $settings
$rabbitUser = Get-Setting -Name 'NEVAXIS_FACTOS_RABBITMQ_USERNAME' -Values $settings
$rabbitPassword = Get-Setting -Name 'NEVAXIS_FACTOS_RABBITMQ_PASSWORD' -Values $settings
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${rabbitUser}:${rabbitPassword}"))
$managementUri = "http://127.0.0.1:${managementPort}/api/health/checks/local-alarms"

$response = Invoke-RestMethod `
    -Uri $managementUri `
    -Headers @{ Authorization = "Basic $credentials" } `
    -Method Get `
    -TimeoutSec 10

if ($response.status -ne 'ok') {
    throw "RabbitMQ management health endpoint returned status '$($response.status)'."
}

Write-Host "[reachable] RabbitMQ management API at $managementUri"
Write-Host '[ready] NevaxisZentra local infrastructure is healthy.'
