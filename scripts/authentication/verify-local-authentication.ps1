[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$ApiPort = 5095,

    [ValidateRange(10, 120)]
    [int]$TimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$databaseScript = Join-Path $repositoryRoot 'scripts\database\database.ps1'
$apiProject = Join-Path $repositoryRoot 'apps\api\CrevaFactOS.Api.csproj'
$artifactDirectory = Join-Path $repositoryRoot 'artifacts\authentication'
$apiBaseUrl = "http://127.0.0.1:$ApiPort"
$runtimePort = if ($env:CREVAFACTOS_POSTGRES_PORT) { $env:CREVAFACTOS_POSTGRES_PORT } else { '5432' }
$apiProcess = $null

function Get-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $windowsPath = 'C:\Program Files\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $windowsPath) {
        return $windowsPath
    }

    throw 'The .NET SDK is unavailable.'
}

function Invoke-TestRequest {
    param([hashtable]$Headers)

    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$apiBaseUrl/internal/tenant-context" -Headers $Headers -TimeoutSec 10
        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = [string]$response.Content }
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = $content }
    }
}

function Assert-Status {
    param([object]$Response, [int]$Expected, [string]$Scenario)

    if ($Response.Status -ne $Expected) {
        throw "$Scenario returned $($Response.Status); expected $Expected."
    }
}

function New-IdentityHeaders {
    param([string]$Subject, [string]$Tenant)

    return @{
        'X-CrevaFactOS-Dev-Issuer' = 'https://identity.local.test'
        'X-CrevaFactOS-Dev-Subject' = $Subject
        'X-CrevaFactOS-Tenant' = $Tenant
    }
}

$env:CREVAFACTOS__PERSISTENCE__ENABLED = 'true'
$env:CREVAFACTOS__PERSISTENCE__HOST = '127.0.0.1'
$env:CREVAFACTOS__PERSISTENCE__PORT = $runtimePort
$env:CREVAFACTOS__PERSISTENCE__DATABASE = 'crevafactos_local'
$env:CREVAFACTOS__PERSISTENCE__USERNAME = 'crevafactos_runtime'
$env:CREVAFACTOS__PERSISTENCE__PASSWORD = 'local_runtime_only_change_me'
$env:CREVAFACTOS__PERSISTENCE__SSL_MODE = 'Disable'
$env:CREVAFACTOS__PERSISTENCE__CONNECTION_TIMEOUT_SECONDS = '15'
$env:CREVAFACTOS__PERSISTENCE__COMMAND_TIMEOUT_SECONDS = '30'
$env:CREVAFACTOS__AUTHENTICATION__MODE = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $apiBaseUrl

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript authentication-membership-bootstrap
    if ($LASTEXITCODE -ne 0) {
        throw 'Authentication fixture bootstrap failed.'
    }

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    $dotnetPath = Get-DotNetPath
    $apiProcess = Start-Process `
        -FilePath $dotnetPath `
        -ArgumentList @('run', '--no-build', '--project', $apiProject, '--no-launch-profile') `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $artifactDirectory 'api.stdout.log') `
        -RedirectStandardError (Join-Path $artifactDirectory 'api.stderr.log')

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if ($apiProcess.HasExited) {
            throw "Development API exited before becoming ready (exit code $($apiProcess.ExitCode))."
        }

        try {
            $health = Invoke-WebRequest -UseBasicParsing -Uri "$apiBaseUrl/health/live" -TimeoutSec 2
            if ($health.StatusCode -eq 200) {
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    if ([DateTimeOffset]::UtcNow -ge $deadline) {
        throw 'Development API did not become healthy before the timeout.'
    }

    $activeByKey = Invoke-TestRequest -Headers (New-IdentityHeaders 'active-member' 'auth-a')
    $activeById = Invoke-TestRequest -Headers (New-IdentityHeaders 'active-member' 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1')
    Assert-Status $activeByKey 200 'Active membership by key'
    Assert-Status $activeById 200 'Active membership by UUID'

    $headerOnly = Invoke-TestRequest -Headers @{ 'X-CrevaFactOS-Tenant' = 'auth-a' }
    Assert-Status $headerOnly 401 'Header-only selection'

    $denied = @(
        (Invoke-TestRequest -Headers (New-IdentityHeaders 'active-member' 'auth-b')),
        (Invoke-TestRequest -Headers (New-IdentityHeaders 'no-membership' 'auth-a')),
        (Invoke-TestRequest -Headers (New-IdentityHeaders 'inactive-membership' 'auth-a')),
        (Invoke-TestRequest -Headers (New-IdentityHeaders 'inactive-tenant' 'auth-inactive')),
        (Invoke-TestRequest -Headers (New-IdentityHeaders 'active-member' 'auth-unknown'))
    )
    foreach ($response in $denied) {
        Assert-Status $response 403 'Tenant or membership denial'
        if ($response.Content -ne $denied[0].Content) {
            throw 'Tenant and membership denial responses are distinguishable.'
        }
    }

    $inactiveIdentity = Invoke-TestRequest -Headers (New-IdentityHeaders 'inactive-identity' 'auth-a')
    Assert-Status $inactiveIdentity 401 'Inactive identity'

    Write-Host '[verified] Development authentication and real active-membership tenant selection.'
    Write-Host '[verified] Header-only, cross-tenant, inactive, missing, and unknown cases fail closed.'
    Write-Host '[verified] Tenant and membership denial responses are indistinguishable.'
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript authentication-membership-cleanup
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'Authentication fixture cleanup failed.'
    }
}
