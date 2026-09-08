# Repairs Next.js standalone output produced from a pnpm monorepo so it can run
# outside the repository tree. External junctions are replaced with in-tree targets.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StandaloneRoot
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "desktop-runtime-filesystem.ps1")

if (-not (Test-Path -LiteralPath $StandaloneRoot)) {
    throw "Standalone root not found: $StandaloneRoot"
}

$normalizedRoot = (Resolve-Path -LiteralPath $StandaloneRoot).Path.TrimEnd('\')
$repaired = 0

function Get-JunctionTarget([System.IO.FileSystemInfo]$Item) {
    $target = $Item.Target
    if ($target -is [System.Array]) {
        $target = $target[0]
    }

    if ([string]::IsNullOrWhiteSpace($target)) {
        return $null
    }

    return [IO.Path]::GetFullPath($target)
}

function Replace-ReparsePointWithCopy {
    param(
        [string]$Path,
        [string]$TargetFull
    )

    $tempPath = "$Path.repair-temp"
    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $tempPath | Out-Null
    Invoke-RobocopyDirectory -Source $TargetFull -Destination $tempPath
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Force -Recurse
    }
    Move-Item -LiteralPath $tempPath -Destination $Path -Force
}

$nextPackageDir = Get-ChildItem -LiteralPath (Join-Path $normalizedRoot "node_modules\.pnpm") -Directory -Filter "next@*" |
    Sort-Object Name -Descending |
    Select-Object -First 1
if ($null -eq $nextPackageDir) {
    throw "Unable to locate traced next package under standalone node_modules/.pnpm"
}

$internalNextPath = Join-Path $nextPackageDir.FullName "node_modules\next"
if (-not (Test-Path -LiteralPath $internalNextPath)) {
    throw "Unable to locate internal traced next package at $internalNextPath"
}

$appNextPath = Join-Path $normalizedRoot "apps\web\node_modules\next"
if (Test-Path -LiteralPath $appNextPath) {
    $appNextItem = Get-Item -LiteralPath $appNextPath -Force
    if ($appNextItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        $target = Get-JunctionTarget $appNextItem
        if ($null -ne $target -and -not $target.StartsWith($normalizedRoot, [StringComparison]::OrdinalIgnoreCase)) {
            Replace-ReparsePointWithCopy -Path $appNextPath -TargetFull $internalNextPath
            $repaired++
        }
    }
}

$appNodeModules = Join-Path $normalizedRoot "apps\web\node_modules"
New-Item -ItemType Directory -Force -Path $appNodeModules | Out-Null
$internalPackageModules = Join-Path $nextPackageDir.FullName "node_modules"
Get-ChildItem -LiteralPath $internalPackageModules -Force |
    ForEach-Object {
        $destination = Join-Path $appNodeModules $_.Name
        if (Test-Path -LiteralPath $destination) {
            Remove-Item -LiteralPath $destination -Recurse -Force
        }

        New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
        Invoke-RobocopyDirectory -Source $_.FullName -Destination $destination
        $repaired++
    }

$reparsePoints = Get-ChildItem -LiteralPath $normalizedRoot -Recurse -Force |
    Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($item in $reparsePoints) {
    $targetFull = Get-JunctionTarget $item
    if ([string]::IsNullOrWhiteSpace($targetFull)) {
        continue
    }

    if ($targetFull.StartsWith($normalizedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if (-not (Test-Path -LiteralPath $targetFull)) {
        throw "Standalone junction target missing: $targetFull (from $($item.FullName))"
    }

    Replace-ReparsePointWithCopy -Path $item.FullName -TargetFull $targetFull
    $repaired++
}

Write-Host "STANDALONE_JUNCTION_REPAIR: repaired=$repaired root=$normalizedRoot"
Assert-StandaloneRuntimeModuleResolution -StandaloneRoot $normalizedRoot
