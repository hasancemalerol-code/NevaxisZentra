# Shared Windows-safe filesystem helpers for Desktop Runtime packaging.

function Invoke-RobocopyDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Robocopy source not found: $Source"
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    & robocopy $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "Robocopy failed copying '$Source' -> '$Destination' (exit $exitCode)."
    }
}

function Test-StandaloneNextServerModule {
    param(
        [Parameter(Mandatory = $true)][string]$StandaloneRoot
    )

    $candidates = @(
        (Join-Path $StandaloneRoot "apps\web\node_modules\next\dist\server\next.js"),
        (Join-Path $StandaloneRoot "node_modules\next\dist\server\next.js")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    $nextPackageDir = Get-ChildItem -LiteralPath (Join-Path $StandaloneRoot "node_modules\.pnpm") -Directory -Filter "next@*" -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Select-Object -First 1
    if ($null -ne $nextPackageDir) {
        $pnpmCandidate = Join-Path $nextPackageDir.FullName "node_modules\next\dist\server\next.js"
        if (Test-Path -LiteralPath $pnpmCandidate) {
            return $pnpmCandidate
        }
    }

    return $null
}

function Assert-StandaloneRuntimeModuleResolution {
    param(
        [Parameter(Mandatory = $true)][string]$StandaloneRoot
    )

    $serverScript = Join-Path $StandaloneRoot "apps\web\server.js"
    if (-not (Test-Path -LiteralPath $serverScript)) {
        throw "Missing packaged web server.js at $serverScript"
    }

    $nextModule = Test-StandaloneNextServerModule -StandaloneRoot $StandaloneRoot
    if ([string]::IsNullOrWhiteSpace($nextModule)) {
        throw "Missing required Next.js runtime module next/dist/server/next.js under $StandaloneRoot"
    }

    Write-Host "STANDALONE_RUNTIME_MODULE_RESOLUTION=PASS"
    Write-Host "NEXT_SERVER_MODULE=$nextModule"
}
