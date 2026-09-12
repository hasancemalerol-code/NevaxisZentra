# Controlled NevaxisFactOS -> NevaxisFactOS technical migration helper.
# Run from repository root: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/migration/rename-to-nevaxisfactos.ps1

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
    if ($fileName -like 'NevaxisFactOS-*-*.md') {
        return $true
    }

    if ($fileName -like 'NevaxisFactOS-*.md') {
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
        @{ From = "packages/NevaxisFactOS.Configuration"; To = "packages/NevaxisFactOS.Configuration" },
        @{ From = "packages/NevaxisFactOS.Persistence"; To = "packages/NevaxisFactOS.Persistence" },
        @{ From = "packages/NevaxisFactOS.Persistence.Migrations"; To = "packages/NevaxisFactOS.Persistence.Migrations" },
        @{ From = "tests/NevaxisFactOS.SmokeTests"; To = "tests/NevaxisFactOS.SmokeTests" },
        @{ From = "tests/NevaxisFactOS.Persistence.Tests"; To = "tests/NevaxisFactOS.Persistence.Tests" },
        @{ From = "tests/NevaxisFactOS.Desktop.Tests"; To = "tests/NevaxisFactOS.Desktop.Tests" },
        @{ From = "tools/NevaxisFactOS.DatabaseTool"; To = "tools/NevaxisFactOS.DatabaseTool" },
        @{ From = "tools/NevaxisFactOS.LogoBridge"; To = "tools/NevaxisFactOS.LogoBridge" },
        @{ From = "tools/NevaxisFactOS.MikroBridge"; To = "tools/NevaxisFactOS.MikroBridge" },
        @{ From = "tools/NevaxisFactOS.NetsisBridge"; To = "tools/NevaxisFactOS.NetsisBridge" },
        @{ From = "tools/NevaxisFactOS.MachineSimulator"; To = "tools/NevaxisFactOS.MachineSimulator" },
        @{ From = "apps/desktop/NevaxisFactOS.Desktop"; To = "apps/desktop/NevaxisFactOS.Desktop" }
    )

    foreach ($rename in $directoryRenames) {
        Invoke-GitMove -From $rename.From -To $rename.To
    }

    $fileRenames = @(
        @{ From = "NevaxisFactOS.sln"; To = "NevaxisFactOS.sln" },
        @{ From = "apps/api/NevaxisFactOS.Api.csproj"; To = "apps/api/NevaxisFactOS.Api.csproj" },
        @{ From = "apps/worker/NevaxisFactOS.Worker.csproj"; To = "apps/worker/NevaxisFactOS.Worker.csproj" },
        @{ From = "apps/edge-agent/NevaxisFactOS.EdgeAgent.csproj"; To = "apps/edge-agent/NevaxisFactOS.EdgeAgent.csproj" },
        @{ From = "packages/NevaxisFactOS.Configuration/NevaxisFactOS.Configuration.csproj"; To = "packages/NevaxisFactOS.Configuration/NevaxisFactOS.Configuration.csproj" },
        @{ From = "packages/NevaxisFactOS.Persistence/NevaxisFactOS.Persistence.csproj"; To = "packages/NevaxisFactOS.Persistence/NevaxisFactOS.Persistence.csproj" },
        @{ From = "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj"; To = "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj" },
        @{ From = "tests/NevaxisFactOS.SmokeTests/NevaxisFactOS.SmokeTests.csproj"; To = "tests/NevaxisFactOS.SmokeTests/NevaxisFactOS.SmokeTests.csproj" },
        @{ From = "tests/NevaxisFactOS.Persistence.Tests/NevaxisFactOS.Persistence.Tests.csproj"; To = "tests/NevaxisFactOS.Persistence.Tests/NevaxisFactOS.Persistence.Tests.csproj" },
        @{ From = "tests/NevaxisFactOS.Desktop.Tests/NevaxisFactOS.Desktop.Tests.csproj"; To = "tests/NevaxisFactOS.Desktop.Tests/NevaxisFactOS.Desktop.Tests.csproj" },
        @{ From = "tools/NevaxisFactOS.DatabaseTool/NevaxisFactOS.DatabaseTool.csproj"; To = "tools/NevaxisFactOS.DatabaseTool/NevaxisFactOS.DatabaseTool.csproj" },
        @{ From = "tools/NevaxisFactOS.LogoBridge/NevaxisFactOS.LogoBridge.csproj"; To = "tools/NevaxisFactOS.LogoBridge/NevaxisFactOS.LogoBridge.csproj" },
        @{ From = "tools/NevaxisFactOS.MikroBridge/NevaxisFactOS.MikroBridge.csproj"; To = "tools/NevaxisFactOS.MikroBridge/NevaxisFactOS.MikroBridge.csproj" },
        @{ From = "tools/NevaxisFactOS.NetsisBridge/NevaxisFactOS.NetsisBridge.csproj"; To = "tools/NevaxisFactOS.NetsisBridge/NevaxisFactOS.NetsisBridge.csproj" },
        @{ From = "tools/NevaxisFactOS.MachineSimulator/NevaxisFactOS.MachineSimulator.csproj"; To = "tools/NevaxisFactOS.MachineSimulator/NevaxisFactOS.MachineSimulator.csproj" },
        @{ From = "apps/desktop/NevaxisFactOS.Desktop/NevaxisFactOS.Desktop.csproj"; To = "apps/desktop/NevaxisFactOS.Desktop/NevaxisFactOS.Desktop.csproj" }
    )

    foreach ($rename in $fileRenames) {
        Invoke-GitMove -From $rename.From -To $rename.To
    }

    $optionalIconRename = @{
        From = "apps/desktop/scripts/generate-NevaxisFactOS-app-icon.py"
        To   = "apps/desktop/scripts/generate-nevaxisfactos-app-icon.py"
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
        $content = $content -replace '@NevaxisFactOS/', '@nevaxisfactos/'
        $content = $content -replace '"name": "crevafactos"', '"name": "crevafactos"'

        if ($content -ne $original) {
            [System.IO.File]::WriteAllText($file.FullName, $content)
            $updatedCount++
        }
    }

    Write-Host "Updated $updatedCount files with namespace/project replacements."
}

Write-Host "==> Migration script completed."
