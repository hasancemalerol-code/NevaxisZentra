# Loads infrastructure/.env (when present) and starts the NevaxisZentra API for local development.
# Enforces a single NevaxisZentra development API instance for this repository.

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotNetArgs
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path $repositoryRoot 'apps\api\NevaxisZentra.Api.csproj'
$devApiPort = 5000
$devApiUrl = "http://127.0.0.1:$devApiPort"
$repoRootLower = $repositoryRoot.ToLowerInvariant()
$startupLockPath = Join-Path $repositoryRoot '.local\dev-api-start.lock'
$script:StartupLockStream = $null

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

function Enter-DevApiStartupLock {
    $lockDirectory = Split-Path -Parent $startupLockPath
    if (-not (Test-Path -LiteralPath $lockDirectory)) {
        New-Item -ItemType Directory -Path $lockDirectory -Force | Out-Null
    }

    try {
        $script:StartupLockStream = [System.IO.File]::Open(
            $startupLockPath,
            [System.IO.FileMode]::OpenOrCreate,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch {
        throw "Another start-dev-api.ps1 invocation is already running for this repository. Wait for it to finish or remove stale lock at '$startupLockPath'."
    }
}

function Exit-DevApiStartupLock {
    if ($script:StartupLockStream) {
        $script:StartupLockStream.Dispose()
        $script:StartupLockStream = $null
    }
}

function Get-ProcessDetails {
    param([int]$ProcessId)

    if ($ProcessId -le 0) {
        return $null
    }

    $cim = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction SilentlyContinue
    if (-not $cim) {
        return $null
    }

    $path = $null
    try {
        $path = (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue).Path
    }
    catch {
        $path = $null
    }

    return [PSCustomObject]@{
        Id = $ProcessId
        Name = [string]$cim.Name
        CommandLine = [string]$cim.CommandLine
        ParentProcessId = [int]$cim.ParentProcessId
        Path = [string]$path
    }
}

function Test-StringContainsRepoRoot {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.ToLowerInvariant().Contains($repoRootLower)
}

function Test-IsRepoDevApiProcess {
    param(
        [AllowNull()]
        $ProcessDetails
    )

    if ($null -eq $ProcessDetails) {
        return $false
    }

    $haystack = ("$($ProcessDetails.CommandLine) $($ProcessDetails.Path)").ToLowerInvariant()
    if (-not (Test-StringContainsRepoRoot $haystack)) {
        return $false
    }

    if ($ProcessDetails.Name -eq 'NevaxisZentra.Api.exe') {
        return $true
    }

    if ($ProcessDetails.Name -eq 'dotnet.exe') {
        return $haystack.Contains('nevaxiszentra.api.csproj') -or
            ($haystack.Contains('apps\api') -and $haystack.Contains('dotnet run'))
    }

    if ($ProcessDetails.Name -in @('powershell.exe', 'pwsh.exe')) {
        return $haystack.Contains('start-dev-api.ps1') -or
            $haystack.Contains('nevaxiszentra.api.csproj')
    }

    return $false
}

function Get-ProtectedProcessIds {
    $protected = [System.Collections.Generic.HashSet[int]]::new()
    $currentProcessId = $PID

    while ($currentProcessId -gt 0) {
        if (-not $protected.Add($currentProcessId)) {
            break
        }

        $current = Get-ProcessDetails -ProcessId $currentProcessId
        if (-not $current) {
            break
        }

        $currentProcessId = $current.ParentProcessId
    }

    return $protected
}

function Get-RepoDevApiProcessDetails {
    $protectedProcessIds = Get-ProtectedProcessIds

    return @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        ForEach-Object {
            Get-ProcessDetails -ProcessId $_.ProcessId
        } |
        Where-Object {
            $_ -and
            -not $protectedProcessIds.Contains($_.Id) -and
            (Test-IsRepoDevApiProcess -ProcessDetails $_)
        })
}

function Expand-RepoDevApiProcessTree {
    param(
        [Parameter(Mandatory = $true)]
        [int[]]$SeedProcessIds
    )

    $collected = [System.Collections.Generic.HashSet[int]]::new()
    $queue = [System.Collections.Generic.Queue[int]]::new()

    foreach ($seedProcessId in $SeedProcessIds) {
        if ($seedProcessId -le 0) {
            continue
        }

        if (-not (Get-Process -Id $seedProcessId -ErrorAction SilentlyContinue)) {
            continue
        }

        if ($collected.Add($seedProcessId)) {
            $queue.Enqueue($seedProcessId)
        }
    }

    while ($queue.Count -gt 0) {
        $currentProcessId = $queue.Dequeue()
        $current = Get-ProcessDetails -ProcessId $currentProcessId
        if ($null -eq $current) {
            continue
        }

        $parentProcessId = $current.ParentProcessId
        if ($parentProcessId -le 0 -or $collected.Contains($parentProcessId)) {
            continue
        }

        if (-not (Get-Process -Id $parentProcessId -ErrorAction SilentlyContinue)) {
            continue
        }

        $parent = Get-ProcessDetails -ProcessId $parentProcessId
        if ($null -eq $parent) {
            continue
        }

        if (Test-IsRepoDevApiProcess -ProcessDetails $parent) {
            if ($collected.Add($parentProcessId)) {
                $queue.Enqueue($parentProcessId)
            }
        }
    }

    return @($collected)
}

function Get-PortListenerDetails {
    param([int]$Port)

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if (-not $connection) {
        return $null
    }

    return Get-ProcessDetails -ProcessId $connection.OwningProcess
}

function Wait-PortReleased {
    param(
        [int]$Port,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $listener = Get-PortListenerDetails -Port $Port
        if (-not $listener) {
            return $true
        }

        Start-Sleep -Milliseconds 400
    }

    return $false
}

function Stop-RepoDevApiProcessTree {
    param(
        [Parameter(Mandatory = $true)]
        [int[]]$ProcessIds
    )

    if ($ProcessIds.Count -eq 0) {
        return
    }

    $details = @(
        $ProcessIds |
            ForEach-Object { Get-ProcessDetails -ProcessId $_ } |
            Where-Object { $_ }
    )

    $stopOrder = @{
        'NevaxisZentra.Api.exe' = 0
        'dotnet.exe' = 1
        'powershell.exe' = 2
        'pwsh.exe' = 2
    }

    $ordered = $details |
        Sort-Object `
            @{ Expression = { if ($stopOrder.ContainsKey($_.Name)) { $stopOrder[$_.Name] } else { 99 } } }, `
            @{ Expression = { $_.Id } }

    foreach ($process in $ordered) {
        if (-not (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
            continue
        }

        Write-Host "  Stopping PID $($process.Id) ($($process.Name))"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Resolve-ExistingDevApiInstance {
    $protectedProcessIds = Get-ProtectedProcessIds
    $repoProcesses = Get-RepoDevApiProcessDetails
    $portListener = Get-PortListenerDetails -Port $devApiPort
    $seedProcessIds = @($repoProcesses | ForEach-Object { $_.Id })

    if ($portListener) {
        if ($protectedProcessIds.Contains($portListener.Id)) {
            return @()
        }

        if (Test-IsRepoDevApiProcess -ProcessDetails $portListener) {
            $seedProcessIds += $portListener.Id
        }
        else {
            $commandLine = if ([string]::IsNullOrWhiteSpace($portListener.CommandLine)) {
                '<empty>'
            }
            else {
                $portListener.CommandLine
            }

            $path = if ([string]::IsNullOrWhiteSpace($portListener.Path)) {
                '<unknown>'
            }
            else {
                $portListener.Path
            }

            throw @"
Port $devApiPort is already in use by a non-NevaxisZentra development API process.
PID: $($portListener.Id)
Process: $($portListener.Name)
Path: $path
Command line: $commandLine
Stop that process manually or choose a different port for the other application.
"@
        }
    }

    $seedProcessIds = @(
        $seedProcessIds |
            Where-Object {
                -not $protectedProcessIds.Contains($_) -and
                (Get-Process -Id $_ -ErrorAction SilentlyContinue)
            } |
            Select-Object -Unique
    )
    if ($seedProcessIds.Count -eq 0 -and -not $portListener) {
        return @()
    }

    Write-Host 'Existing NevaxisZentra API found'
    Write-Host 'Stopping process tree...'

    $processTree = @()
    if ($seedProcessIds.Count -gt 0) {
        $processTree = @(
            Expand-RepoDevApiProcessTree -SeedProcessIds $seedProcessIds |
                Where-Object { -not $protectedProcessIds.Contains($_) }
        )
        Stop-RepoDevApiProcessTree -ProcessIds $processTree
    }

    $remainingRepoProcesses = @(
        Get-RepoDevApiProcessDetails |
            ForEach-Object { $_.Id } |
            Where-Object { -not $protectedProcessIds.Contains($_) }
    )
    if ($remainingRepoProcesses.Count -gt 0) {
        $extraTree = @(
            Expand-RepoDevApiProcessTree -SeedProcessIds $remainingRepoProcesses |
                Where-Object { -not $protectedProcessIds.Contains($_) }
        )
        Stop-RepoDevApiProcessTree -ProcessIds $extraTree
        $processTree += $extraTree
    }

    if ($portListener -or (Get-PortListenerDetails -Port $devApiPort)) {
        if (-not (Wait-PortReleased -Port $devApiPort)) {
            throw "Timed out waiting for port $devApiPort to be released."
        }

        Write-Host "Port $devApiPort released"
    }

    return $processTree
}

function Wait-ApiListening {
    param([int]$TimeoutSeconds = 90)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Get-PortListenerDetails -Port $devApiPort) {
            return $true
        }

        Start-Sleep -Seconds 1
    }

    return $false
}

Import-EnvironmentFile (Join-Path $repositoryRoot 'infrastructure\.env')
Import-EnvironmentFile (Join-Path $repositoryRoot 'infrastructure\.env.local')

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

Enter-DevApiStartupLock
$apiProcess = $null
try {
    Resolve-ExistingDevApiInstance | Out-Null

    Write-Host 'Starting API...'

    $dotnetArguments = if ($DotNetArgs -and $DotNetArgs.Count -gt 0) {
        @('run', '--project', $apiProject) + $DotNetArgs
    }
    else {
        @('run', '--project', $apiProject, '--launch-profile', 'http')
    }

    $apiProcess = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList $dotnetArguments `
        -WorkingDirectory $repositoryRoot `
        -PassThru `
        -NoNewWindow

    if (-not (Wait-ApiListening)) {
        if (-not $apiProcess.HasExited) {
            Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        }

        throw "Timed out waiting for API to listen on $devApiUrl"
    }

    Write-Host "API listening on $devApiUrl"
}
finally {
    Exit-DevApiStartupLock
}

if ($apiProcess) {
    Wait-Process -Id $apiProcess.Id

    if ($apiProcess.ExitCode -ne 0) {
        throw "dotnet run failed with exit code $($apiProcess.ExitCode)."
    }
}
