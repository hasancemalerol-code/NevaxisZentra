# Controlled NevaxisZentra -> NevaxisZentra technical migration helper.
# Run from repository root: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/migration/rename-to-nevaxiszentra.ps1

[CmdletBinding()]
param(
    [switch]$SkipRenames,
    [switch]$SkipContent
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $repoRoot

function Invoke-GitMove {
    param(
        [Parameter(Mandatory = $true)][string]$From,
        [Parameter(Mandatory = $true)][string]$To
    )

    if (-not (Test-Path -LiteralPath $From)) {
        Write-Host "SKIP (missing): $From"
        return
    }

    if (Test-Path -LiteralPath $To) {
        Write-Host "SKIP (exists): $To"
        return
    }

    $toParent = Split-Path -Parent $To
    if ($toParent -and -not (Test-Path -LiteralPath $toParent)) {
        New-Item -ItemType Directory -Path $toParent -Force | Out-Null
    }

    git mv -- "$From" "$To"
    if ($LASTEXITCODE -ne 0) {
        throw "git mv failed: $From -> $To"
    }

    Write-Host "RENAMED: $From -> $To"
}

function Test-ExcludedPath {
    param([string]$RelativePath)

    $normalized = $RelativePath -replace '\\', '/'

    $excludePatterns = @(
        '.cursor-screenshots/',
        '.build/',
        '/bin/',
        '/obj/',
        '/node_modules/',
        '/artifacts/',
        '/.git/'
    )

    foreach ($pattern in $excludePatterns) {
        if ($normalized -like "*$pattern*") {
            return $true
        }
    }

    $fileName = Split-Path -Leaf $normalized
    if ($fileName -like 'NevaxisZentra-*-*.md') {
        return $true
    }

    if ($fileName -like 'NevaxisZentra-*.md') {
        return $true
    }

    return $false
}

function Get-TextExtensions {
    @(
        '.cs', '.csproj', '.sln', '.json', '.md', '.yaml', '.yml', '.ps1', '.mjs', '.js', '.ts', '.tsx',
        '.sql', '.py', '.props', '.targets', '.xaml', '.config', '.env', '.txt', '.sh', '.xml', '.html', '.css'
    )
}

if (-not $SkipRenames) {
    Write-Host "==> Phase 1: directory and file renames"

    $directoryRenames = @(
        @{ From = "packages/NevaxisZentra.Configuration"; To = "packages/NevaxisZentra.Configuration" },
        @{ From = "packages/NevaxisZentra.Persistence"; To = "packages/NevaxisZentra.Persistence" },
        @{ From = "packages/NevaxisZentra.Persistence.Migrations"; To = "packages/NevaxisZentra.Persistence.Migrations" },
        @{ From = "tests/NevaxisZentra.SmokeTests"; To = "tests/NevaxisZentra.SmokeTests" },
        @{ From = "tests/NevaxisZentra.Persistence.Tests"; To = "tests/NevaxisZentra.Persistence.Tests" },
        @{ From = "tests/NevaxisZentra.Desktop.Tests"; To = "tests/NevaxisZentra.Desktop.Tests" },
        @{ From = "tools/NevaxisZentra.DatabaseTool"; To = "tools/NevaxisZentra.DatabaseTool" },
        @{ From = "tools/NevaxisZentra.LogoBridge"; To = "tools/NevaxisZentra.LogoBridge" },
        @{ From = "tools/NevaxisZentra.MikroBridge"; To = "tools/NevaxisZentra.MikroBridge" },
        @{ From = "tools/NevaxisZentra.NetsisBridge"; To = "tools/NevaxisZentra.NetsisBridge" },
        @{ From = "tools/NevaxisZentra.MachineSimulator"; To = "tools/NevaxisZentra.MachineSimulator" },
        @{ From = "apps/desktop/NevaxisZentra.Desktop"; To = "apps/desktop/NevaxisZentra.Desktop" }
    )

    foreach ($rename in $directoryRenames) {
        Invoke-GitMove -From $rename.From -To $rename.To
    }

    $fileRenames = @(
        @{ From = "NevaxisZentra.sln"; To = "NevaxisZentra.sln" },
        @{ From = "apps/api/NevaxisZentra.Api.csproj"; To = "apps/api/NevaxisZentra.Api.csproj" },
        @{ From = "apps/worker/NevaxisZentra.Worker.csproj"; To = "apps/worker/NevaxisZentra.Worker.csproj" },
        @{ From = "apps/edge-agent/NevaxisZentra.EdgeAgent.csproj"; To = "apps/edge-agent/NevaxisZentra.EdgeAgent.csproj" },
        @{ From = "packages/NevaxisZentra.Configuration/NevaxisZentra.Configuration.csproj"; To = "packages/NevaxisZentra.Configuration/NevaxisZentra.Configuration.csproj" },
        @{ From = "packages/NevaxisZentra.Persistence/NevaxisZentra.Persistence.csproj"; To = "packages/NevaxisZentra.Persistence/NevaxisZentra.Persistence.csproj" },
        @{ From = "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj"; To = "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj" },
        @{ From = "tests/NevaxisZentra.SmokeTests/NevaxisZentra.SmokeTests.csproj"; To = "tests/NevaxisZentra.SmokeTests/NevaxisZentra.SmokeTests.csproj" },
        @{ From = "tests/NevaxisZentra.Persistence.Tests/NevaxisZentra.Persistence.Tests.csproj"; To = "tests/NevaxisZentra.Persistence.Tests/NevaxisZentra.Persistence.Tests.csproj" },
        @{ From = "tests/NevaxisZentra.Desktop.Tests/NevaxisZentra.Desktop.Tests.csproj"; To = "tests/NevaxisZentra.Desktop.Tests/NevaxisZentra.Desktop.Tests.csproj" },
        @{ From = "tools/NevaxisZentra.DatabaseTool/NevaxisZentra.DatabaseTool.csproj"; To = "tools/NevaxisZentra.DatabaseTool/NevaxisZentra.DatabaseTool.csproj" },
        @{ From = "tools/NevaxisZentra.LogoBridge/NevaxisZentra.LogoBridge.csproj"; To = "tools/NevaxisZentra.LogoBridge/NevaxisZentra.LogoBridge.csproj" },
        @{ From = "tools/NevaxisZentra.MikroBridge/NevaxisZentra.MikroBridge.csproj"; To = "tools/NevaxisZentra.MikroBridge/NevaxisZentra.MikroBridge.csproj" },
        @{ From = "tools/NevaxisZentra.NetsisBridge/NevaxisZentra.NetsisBridge.csproj"; To = "tools/NevaxisZentra.NetsisBridge/NevaxisZentra.NetsisBridge.csproj" },
        @{ From = "tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj"; To = "tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj" },
        @{ From = "apps/desktop/NevaxisZentra.Desktop/NevaxisZentra.Desktop.csproj"; To = "apps/desktop/NevaxisZentra.Desktop/NevaxisZentra.Desktop.csproj" }
    )

    foreach ($rename in $fileRenames) {
        Invoke-GitMove -From $rename.From -To $rename.To
    }

    $optionalIconRename = @{
        From = "apps/desktop/scripts/generate-NevaxisZentra-app-icon.py"
        To   = "apps/desktop/scripts/generate-nevaxiszentra-app-icon.py"
    }
    if ((Test-Path -LiteralPath $optionalIconRename.From) -and -not (Test-Path -LiteralPath $optionalIconRename.To)) {
        Invoke-GitMove -From $optionalIconRename.From -To $optionalIconRename.To
    }
}

if (-not $SkipContent) {
    Write-Host "==> Phase 2: controlled content replacements"

    $extensions = Get-TextExtensions
    $files = Get-ChildItem -Path $repoRoot -Recurse -File |
        Where-Object {
            $extensions -contains $_.Extension.ToLowerInvariant()
        }

    $updatedCount = 0
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($repoRoot.Length + 1)
        if (Test-ExcludedPath -RelativePath $relative) {
            continue
        }

        $content = [System.IO.File]::ReadAllText($file.FullName)
        $original = $content

        $content = $content -replace 'crevafactos', 'crevafactos'
        $content = $content -replace '@NevaxisZentra/', '@nevaxiszentra/'
        $content = $content -replace '"name": "crevafactos"', '"name": "crevafactos"'

        if ($content -ne $original) {
            [System.IO.File]::WriteAllText($file.FullName, $content)
            $updatedCount++
        }
    }

    Write-Host "Updated $updatedCount files with namespace/project replacements."
}

Write-Host "==> Migration script completed."
