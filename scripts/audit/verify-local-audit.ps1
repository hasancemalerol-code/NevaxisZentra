[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$ApiPort = 5097,

    [ValidateRange(10, 120)]
    [int]$TimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$databaseScript = Join-Path $repositoryRoot 'scripts\database\database.ps1'
$apiProject = Join-Path $repositoryRoot 'apps\api\CrevaFactOS.Api.csproj'
$artifactDirectory = Join-Path $repositoryRoot 'artifacts\audit'
$apiBaseUrl = "http://127.0.0.1:$ApiPort"
$runtimePort = if ($env:CREVAFACTOS_POSTGRES_PORT) { $env:CREVAFACTOS_POSTGRES_PORT } else { '5432' }
$apiProcess = $null

function Get-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    if (Test-Path -LiteralPath 'C:\Program Files\dotnet\dotnet.exe') { return 'C:\Program Files\dotnet\dotnet.exe' }
    throw 'The .NET SDK is unavailable.'
}

function New-AuditHeaders {
    param([string]$Subject, [string]$Tenant)
    return @{
        'X-CrevaFactOS-Dev-Issuer' = 'https://audit.local.test'
        'X-CrevaFactOS-Dev-Subject' = $Subject
        'X-CrevaFactOS-Tenant' = $Tenant
    }
}

function Invoke-TestRequest {
    param([hashtable]$Headers)
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$apiBaseUrl/internal/audit/recent" -Headers $Headers -TimeoutSec 10
        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = [string]$response.Content; Headers = $response.Headers }
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw }
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = $content; Headers = $response.Headers }
    }
}

$env:CREVAFACTOS__PERSISTENCE__ENABLED = 'true'
$env:CREVAFACTOS__PERSISTENCE__HOST = '127.0.0.1'
$env:CREVAFACTOS__PERSISTENCE__PORT = $runtimePort
$env:CREVAFACTOS__PERSISTENCE__DATABASE = 'crevafactos_local'
$env:CREVAFACTOS__PERSISTENCE__USERNAME = 'crevafactos_runtime'
$env:CREVAFACTOS__PERSISTENCE__PASSWORD = 'local_runtime_only_change_me'
$env:CREVAFACTOS__PERSISTENCE__SSL_MODE = 'Disable'
$env:CREVAFACTOS__AUTHENTICATION__MODE = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $apiBaseUrl

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript audit-trail-bootstrap
    if ($LASTEXITCODE -ne 0) { throw 'Audit fixture bootstrap failed.' }

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    $apiProcess = Start-Process `
        -FilePath (Get-DotNetPath) `
        -ArgumentList @('run', '--no-build', '--project', $apiProject, '--no-launch-profile') `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $artifactDirectory 'api.stdout.log') `
        -RedirectStandardError (Join-Path $artifactDirectory 'api.stderr.log')

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if ($apiProcess.HasExited) { throw "Development API exited with code $($apiProcess.ExitCode)." }
        try {
            if ((Invoke-WebRequest -UseBasicParsing -Uri "$apiBaseUrl/health/live" -TimeoutSec 2).StatusCode -eq 200) { break }
        }
        catch { Start-Sleep -Milliseconds 500 }
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    if ([DateTimeOffset]::UtcNow -ge $deadline) { throw 'Development API did not become healthy.' }

    $correlationId = [Guid]::NewGuid().ToString('D')
    $allowedHeaders = New-AuditHeaders 'tenant-a-auditor' 'audit-a'
    $allowedHeaders['X-CrevaFactOS-Correlation-Id'] = $correlationId
    $allowed = Invoke-TestRequest $allowedHeaders
    if ($allowed.Status -ne 200) { throw "Authorized audit read returned $($allowed.Status)." }
    if ($allowed.Content -notmatch 'a7000000-0000-4000-8000-000000000041') { throw 'Tenant A audit record was not returned.' }
    if ($allowed.Content -match 'a7000000-0000-4000-8000-000000000042') { throw 'Tenant B audit record leaked to tenant A.' }
    if ($allowed.Headers['X-CrevaFactOS-Correlation-Id'] -ne $correlationId) { throw 'Correlation ID was not preserved.' }

    $membershipOnly = Invoke-TestRequest (New-AuditHeaders 'membership-only' 'audit-a')
    if ($membershipOnly.Status -ne 403) { throw "Membership-only audit read returned $($membershipOnly.Status)." }

    Write-Host '[verified] Real audit diagnostic read requires exact permission and remains tenant-scoped.'
    Write-Host '[verified] Correlation response propagation and membership-only denial passed.'
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript audit-trail-cleanup
    if ($LASTEXITCODE -ne 0) { Write-Warning 'Audit fixture cleanup failed.' }
}
