# Canonical local NevaxisZentra SaaS development runtime (API 5000 + web 3001).
# No secrets. Refuses duplicate NevaxisZentra listeners on the target ports.

[CmdletBinding()]
param(
    [switch]$SkipWeb,
    [switch]$SkipApi
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path $repositoryRoot 'apps\api\NevaxisZentra.Api.csproj'
$apiPort = 5000
$webPort = 3001
$apiBaseUrl = "http://127.0.0.1:$apiPort"
$webBaseUrl = "http://127.0.0.1:$webPort"

function Test-NevaxisZentraListener {
    param(
        [int]$Port,
        [string]$ExpectedPathFragment
    )

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $connection) {
        return $null
    }

    $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
    if (-not $process) {
        throw "Port $Port is in use by PID $($connection.OwningProcess), but the process could not be inspected."
    }

    $path = $process.Path
    if ($ExpectedPathFragment -and $path -and ($path -notlike "*$ExpectedPathFragment*")) {
        throw "Port $Port is owned by a non-NevaxisZentra process: $($process.ProcessName) ($path)."
    }

    return [PSCustomObject]@{
        Port = $Port
        ProcessId = $process.Id
        Path = $path
    }
}

function Wait-HttpStatus {
    param(
        [string]$Url,
        [int]$ExpectedStatus = 200,
        [int]$TimeoutSeconds = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq $ExpectedStatus) {
                return $true
            }
        }
        catch {
            if ($_.Exception.Response -and ([int]$_.Exception.Response.StatusCode -eq $ExpectedStatus)) {
                return $true
            }
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for $Url to return HTTP $ExpectedStatus."
}

Write-Host "NevaxisZentra local SaaS development launcher"
Write-Host "Repository: $repositoryRoot"

$existingApi = Test-NevaxisZentraListener -Port $apiPort -ExpectedPathFragment 'NevaxisZentra.Api'
$existingWeb = Test-NevaxisZentraListener -Port $webPort -ExpectedPathFragment 'node'

if ($existingApi -and -not $SkipApi) {
    throw "NevaxisZentra API already listening on port $apiPort (PID $($existingApi.ProcessId)). Stop it before launching a duplicate."
}

if ($existingWeb -and -not $SkipWeb) {
    throw "NevaxisZentra web already listening on port $webPort (PID $($existingWeb.ProcessId)). Stop it before launching a duplicate."
}

if (-not $SkipApi) {
    $apiCommand = @(
        "`$env:ASPNETCORE_ENVIRONMENT='Development';",
        "`$env:NEVAXIS_FACTOS__AUTHENTICATION__MODE='SessionCookie';",
        "dotnet run --project `"$apiProject`" --urls `"$apiBaseUrl`" --no-build"
    ) -join ' '

    Write-Host "Starting API on $apiBaseUrl with SessionCookie authentication..."
    Start-Process -FilePath 'powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-NoExit',
        '-Command',
        $apiCommand
    ) -WorkingDirectory $repositoryRoot | Out-Null

    Wait-HttpStatus -Url "$apiBaseUrl/health/ready" -ExpectedStatus 200
    Write-Host "API ready: $apiBaseUrl/health/ready -> 200"
}

if (-not $SkipWeb) {
    $webCommand = @(
        "`$env:NEVAXIS_FACTOS_API_BASE_URL='$apiBaseUrl';",
        "pnpm --filter @nevaxiszentra/web dev -- -p $webPort"
    ) -join ' '

    Write-Host "Starting web on $webBaseUrl with NEVAXIS_FACTOS_API_BASE_URL=$apiBaseUrl..."
    Start-Process -FilePath 'powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-NoExit',
        '-Command',
        $webCommand
    ) -WorkingDirectory $repositoryRoot | Out-Null

    Wait-HttpStatus -Url "$webBaseUrl/giris" -ExpectedStatus 200
    Write-Host "Web ready: $webBaseUrl/giris -> 200"
}

Write-Host 'LOCAL_SAAS_DEVELOPMENT_LAUNCH: READY'
