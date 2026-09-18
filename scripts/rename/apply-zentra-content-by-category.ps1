# Category-scoped content rename (post inventory classification)
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $RepoRoot

$targetRoots = @(
    "apps",
    "packages",
    "tests",
    "tools",
    "scripts",
    "infrastructure",
    "release",
    "docs",
    ".github",
    "README.md",
    "package.json",
    "pnpm-workspace.yaml",
    "NevaxisZentra.sln",
    ".gitignore"
)

$extensions = @(".cs", ".csproj", ".sln", ".json", ".md", ".ps1", ".mjs", ".ts", ".tsx", ".css", ".yaml", ".yml", ".wxs", ".wixproj", ".xaml", ".env", ".txt", ".rtf", ".props", ".sql", ".html", ".xml")

function Get-TargetFiles {
    $files = @()
    foreach ($root in $targetRoots) {
        $path = Join-Path $RepoRoot $root
        if (Test-Path $path -PathType Leaf) {
            $files += Get-Item $path
            continue
        }
        if (Test-Path $path -PathType Container) {
            $files += Get-ChildItem -Path $path -Recurse -File -ErrorAction SilentlyContinue |
                Where-Object {
                    $ext = $_.Extension.ToLowerInvariant()
                    ($extensions -contains $ext) -and
                    $_.FullName -notmatch '\\(bin|obj|node_modules|\.next)\\' -and
                    $_.FullName -notmatch '\\.visual-qa\\'
                }
        }
    }
    return $files | Sort-Object FullName -Unique
}

$replacements = @(
    @{ Old = "NevaxisZentra"; New = "NevaxisZentra"; Category = "ACTIVE TECHNICAL IDENTITY" },
    @{ Old = "nevaxiszentra"; New = "nevaxiszentra"; Category = "ACTIVE TECHNICAL IDENTITY" },
    @{ Old = "NEVAXISZENTRA"; New = "NEVAXISZENTRA"; Category = "ACTIVE TECHNICAL IDENTITY" },
    @{ Old = "Nevaxis Zentra"; New = "Nevaxis Zentra"; Category = "ACTIVE PRODUCT IDENTITY" },
    @{ Old = "NEVAXIS ZENTRA"; New = "NEVAXIS ZENTRA"; Category = "ACTIVE PRODUCT IDENTITY" }
)

# NEVAXIS_FACTOS_ handled separately in LegacyEnvironmentVariables — not bulk-replaced here.

$files = Get-TargetFiles
$changed = 0
foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $original = $content
    foreach ($r in $replacements) {
        $content = $content.Replace($r.Old, $r.New)
    }
    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $content)
        $changed++
    }
}
Write-Host "Category-scoped replacement updated $changed files under active roots."
