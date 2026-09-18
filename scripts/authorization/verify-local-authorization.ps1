[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$ApiPort = 5096,

    [ValidateRange(10, 180)]
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$databaseScript = Join-Path $repositoryRoot 'scripts\database\database.ps1'
$apiProject = Join-Path $repositoryRoot 'apps\api\NevaxisZentra.Api.csproj'
$artifactDirectory = Join-Path $repositoryRoot 'artifacts\authorization'
$apiBaseUrl = "http://127.0.0.1:$ApiPort"
$apiProcess = $null

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
$runtimePort = if ($env:NEVAXIS_FACTOS__PERSISTENCE__PORT) {
    $env:NEVAXIS_FACTOS__PERSISTENCE__PORT
} elseif ($env:NEVAXIS_FACTOS_POSTGRES_PORT) {
    $env:NEVAXIS_FACTOS_POSTGRES_PORT
} else {
    '5433'
}

function Get-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    if (Test-Path -LiteralPath 'C:\Program Files\dotnet\dotnet.exe') { return 'C:\Program Files\dotnet\dotnet.exe' }
    throw 'The .NET SDK is unavailable.'
}

function Invoke-TestRequest {
    param([string]$Path, [hashtable]$Headers = @{})
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$apiBaseUrl$Path" -Headers $Headers -TimeoutSec 10
        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = [string]$response.Content }
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw }
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
        return [PSCustomObject]@{ Status = [int]$response.StatusCode; Content = $content }
    }
}

function New-IdentityHeaders {
    param([string]$Subject, [string]$Tenant)
    return @{
        'X-crevafactos-dev-Issuer' = 'https://authorization.local.test'
        'X-crevafactos-dev-Subject' = $Subject
        'X-NevaxisZentra-Tenant' = $Tenant
    }
}

function Assert-Status {
    param([object]$Response, [int]$Expected, [string]$Scenario)
    if ($Response.Status -ne $Expected) {
        throw "$Scenario returned $($Response.Status); expected $Expected."
    }
}

$env:NEVAXIS_FACTOS__PERSISTENCE__ENABLED = 'true'
$env:NEVAXIS_FACTOS__PERSISTENCE__HOST = '127.0.0.1'
$env:NEVAXIS_FACTOS__PERSISTENCE__PORT = $runtimePort
$env:NEVAXIS_FACTOS__PERSISTENCE__DATABASE = 'crevafactos_local'
$env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME = 'crevafactos_runtime'
$env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = 'local_runtime_only_change_me'
$env:NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE = 'Disable'
$env:NEVAXIS_FACTOS__PERSISTENCE__CONNECTION_TIMEOUT_SECONDS = '15'
$env:NEVAXIS_FACTOS__PERSISTENCE__COMMAND_TIMEOUT_SECONDS = '30'
$env:NEVAXIS_FACTOS__AUTHENTICATION__MODE = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $apiBaseUrl

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript permission-authorization-bootstrap
    if ($LASTEXITCODE -ne 0) { throw 'Permission fixture bootstrap failed.' }

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    & (Get-DotNetPath) @('build', $apiProject, '-v', 'q') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Development API build failed.' }

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

    $tenantAHeaders = New-IdentityHeaders 'tenant-a-member' 'authorization-a'
    Assert-Status (Invoke-TestRequest '/internal/tenant-context' $tenantAHeaders) 200 'Exact assigned permission'
    Assert-Status (Invoke-TestRequest '/internal/tenant-context' (New-IdentityHeaders 'membership-only' 'authorization-a')) 403 'Membership alone'
    Assert-Status (Invoke-TestRequest '/internal/tenant-context' (New-IdentityHeaders 'tenant-a-member' 'authorization-b')) 403 'Cross-tenant selection'
    Assert-Status (Invoke-TestRequest '/internal/tenant-context' (New-IdentityHeaders 'tenant-b-member' 'authorization-b')) 200 'Tenant B exact assignment'

    $checks = @(
        [PSCustomObject]@{ Key = 'tenant.context.read'; Expected = $true },
        [PSCustomObject]@{ Key = 'Tenant.Context.Read'; Expected = $false },
        [PSCustomObject]@{ Key = 'tenant.context'; Expected = $false },
        [PSCustomObject]@{ Key = 'tenant.context.*'; Expected = $false },
        [PSCustomObject]@{ Key = '*'; Expected = $false },
        [PSCustomObject]@{ Key = 'missing.infrastructure.read'; Expected = $false }
    )
    foreach ($entry in $checks) {
        $encoded = [Uri]::EscapeDataString($entry.Key)
        $response = Invoke-TestRequest "/internal/permission-check/$encoded" $tenantAHeaders
        Assert-Status $response 200 "Permission check $($entry.Key)"
        $body = $response.Content | ConvertFrom-Json
        if ([bool]$body.granted -ne $entry.Expected) {
            throw "Permission check $($entry.Key) returned granted=$($body.granted)."
        }
    }

    Assert-Status (Invoke-TestRequest '/health/live') 200 'Anonymous health endpoint'
    Write-Host '[verified] Real API authorization uses exact tenant-scoped database assignments.'
    Write-Host '[verified] Membership-only, cross-tenant, case, prefix, wildcard, and unassigned checks fail closed.'
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File $databaseScript permission-authorization-cleanup
    if ($LASTEXITCODE -ne 0) { Write-Warning 'Permission fixture cleanup failed.' }
}
