# Shared desktop verifier environment isolation helpers.
# Negative tests must never leak NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT beyond the child process.

function Get-RuntimeRootEnvironmentState {
    return [ordered]@{
        Process = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", "Process")
        User    = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", "User")
        Machine = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", "Machine")
    }
}

function Test-KnownVerifierRuntimeRootLeak {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    return $Value -match '(?i)NevaxisZentra-web-readiness-|NevaxisZentra-invalid-runtime|NevaxisZentra-invalid-|invalid-runtime-root|desktop-negative-runtime-'
}

function Clear-KnownDesktopVerifierEnvironmentLeaks {
    foreach ($scope in @("User", "Machine")) {
        $value = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", $scope)
        if (Test-KnownVerifierRuntimeRootLeak $value) {
            [Environment]::SetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", $null, $scope)
            Write-Host "Cleared leaked NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT from $scope scope: $value"
        }
    }

    if (Test-KnownVerifierRuntimeRootLeak $env:NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT) {
        Remove-Item Env:NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT -ErrorAction SilentlyContinue
        Write-Host "Cleared leaked NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT from Process scope"
    }
}

function Assert-RuntimeRootEnvironmentClean {
    param([string]$Context = "verifier")
    $state = Get-RuntimeRootEnvironmentState
    foreach ($scope in $state.Keys) {
        $value = $state[$scope]
        if (Test-KnownVerifierRuntimeRootLeak $value) {
            throw "[$Context] leaked NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT at $scope scope: $value"
        }
    }
    Write-Host "[$Context] runtime-root environment clean (Process/User/Machine)"
}

function Assert-RepositoryRootValid {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string]$Context = "verifier"
    )

    $markers = @("NevaxisZentra.sln", "package.json")
    $valid = $false
    foreach ($marker in $markers) {
        if (Test-Path (Join-Path $RepositoryRoot $marker)) {
            $valid = $true
            break
        }
    }

    if (-not $valid) {
        throw "[$Context] repository root validation failed: $RepositoryRoot"
    }

    if (Test-KnownVerifierRuntimeRootLeak $RepositoryRoot) {
        throw "[$Context] repository root resolves to verifier fixture: $RepositoryRoot"
    }
}

function Start-DesktopProcessWithEnvironment {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string]$WorkingDirectory = "",
        [hashtable]$EnvironmentOverrides = @{},
        [string[]]$StripInheritedEnvironmentVariables = @("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT")
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    if ($WorkingDirectory) {
        $psi.WorkingDirectory = $WorkingDirectory
    }
    $psi.UseShellExecute = $false

    foreach ($key in [Environment]::GetEnvironmentVariables("Process").Keys) {
        if ($StripInheritedEnvironmentVariables -contains [string]$key) { continue }
        $psi.Environment[[string]$key] = [Environment]::GetEnvironmentVariable([string]$key, "Process")
    }

    foreach ($entry in $EnvironmentOverrides.GetEnumerator()) {
        if ([string]::IsNullOrEmpty([string]$entry.Value)) {
            if ($psi.Environment.ContainsKey([string]$entry.Key)) {
                $psi.Environment.Remove([string]$entry.Key)
            }
        } else {
            $psi.Environment[[string]$entry.Key] = [string]$entry.Value
        }
    }

    return [System.Diagnostics.Process]::Start($psi)
}

function Invoke-NegativeRuntimeRootTest {
    param(
        [Parameter(Mandatory)][string]$ArtifactExe,
        [Parameter(Mandatory)][string]$ArtifactDir,
        [Parameter(Mandatory)][string]$InvalidRoot,
        [Parameter(Mandatory)][scriptblock]$OnFailureWindow,
        [int]$FailureWindowTimeoutSeconds = 60
    )

    New-Item -ItemType Directory -Force -Path $InvalidRoot | Out-Null
    $failLaunch = $null
    try {
        Assert-RuntimeRootEnvironmentClean -Context "negative-test-before"
        $failLaunch = Start-DesktopProcessWithEnvironment `
            -FilePath $ArtifactExe `
            -WorkingDirectory $ArtifactDir `
            -EnvironmentOverrides @{ NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT = $InvalidRoot }
        & $OnFailureWindow $failLaunch $FailureWindowTimeoutSeconds
        Assert-RuntimeRootEnvironmentClean -Context "negative-test-after"
        return $failLaunch
    }
    finally {
        Clear-KnownDesktopVerifierEnvironmentLeaks
        Assert-RuntimeRootEnvironmentClean -Context "negative-test-finally"
    }
}
