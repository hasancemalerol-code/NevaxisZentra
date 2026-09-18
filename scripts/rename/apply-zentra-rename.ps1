# Controlled Nevaxis Zentra -> Nevaxis Zentra technical rename
# Run from repository root. Does NOT use git add -A.

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $RepoRoot

function Should-SkipPath([string]$Path) {
    $rel = $Path.Replace($RepoRoot, "").TrimStart("\", "/")
    $skipFragments = @(
        ".git",
        "node_modules",
        "\bin\",
        "\obj\",
        ".next",
        "artifacts",
        ".visual-qa",
        ".cursor-screenshots",
        "smoke-test-output.txt"
    )
    foreach ($p in $skipFragments) {
        if ($rel -like "*$p*") { return $true }
    }
    # Immutable historical evidence reports at repo root
    if ($rel -match '^(CREVAFACTOS|NEVAXISZENTRA)-.*-(REPORT|RUNBOOK|MANIFEST|PLAN|CARD)\.md$') { return $true }
    return $false
}

Write-Host "== Phase 1: git mv project paths =="

$pathRenames = @(
    @{ From = "NevaxisZentra.sln"; To = "NevaxisZentra.sln" },
    @{ From = "packages\NevaxisZentra.Configuration"; To = "packages\NevaxisZentra.Configuration" },
    @{ From = "packages\NevaxisZentra.Persistence"; To = "packages\NevaxisZentra.Persistence" },
    @{ From = "packages\NevaxisZentra.Persistence.Migrations"; To = "packages\NevaxisZentra.Persistence.Migrations" },
    @{ From = "packages\NevaxisZentra.EdgeBuffer"; To = "packages\NevaxisZentra.EdgeBuffer" },
    @{ From = "apps\desktop\NevaxisZentra.Desktop"; To = "apps\desktop\NevaxisZentra.Desktop" },
    @{ From = "apps\api\NevaxisZentra.Api.csproj"; To = "apps\api\NevaxisZentra.Api.csproj" },
    @{ From = "apps\worker\NevaxisZentra.Worker.csproj"; To = "apps\worker\NevaxisZentra.Worker.csproj" },
    @{ From = "apps\edge-agent\NevaxisZentra.EdgeAgent.csproj"; To = "apps\edge-agent\NevaxisZentra.EdgeAgent.csproj" },
    @{ From = "tools\NevaxisZentra.DatabaseTool"; To = "tools\NevaxisZentra.DatabaseTool" },
    @{ From = "tools\NevaxisZentra.LogoBridge"; To = "tools\NevaxisZentra.LogoBridge" },
    @{ From = "tools\NevaxisZentra.MikroBridge"; To = "tools\NevaxisZentra.MikroBridge" },
    @{ From = "tools\NevaxisZentra.NetsisBridge"; To = "tools\NevaxisZentra.NetsisBridge" },
    @{ From = "tools\NevaxisZentra.MachineSimulator"; To = "tools\NevaxisZentra.MachineSimulator" },
    @{ From = "tests\NevaxisZentra.Persistence.Tests"; To = "tests\NevaxisZentra.Persistence.Tests" },
    @{ From = "tests\NevaxisZentra.Desktop.Tests"; To = "tests\NevaxisZentra.Desktop.Tests" },
    @{ From = "tests\NevaxisZentra.SmokeTests"; To = "tests\NevaxisZentra.SmokeTests" },
    @{ From = "apps\desktop\installer\NevaxisZentra.Desktop.Installer.wixproj"; To = "apps\desktop\installer\NevaxisZentra.Desktop.Installer.wixproj" },
    @{ From = "apps\desktop\installer\NevaxisZentra.Desktop.FullInstaller.wixproj"; To = "apps\desktop\installer\NevaxisZentra.Desktop.FullInstaller.wixproj" }
)

foreach ($r in $pathRenames) {
    $from = Join-Path $RepoRoot $r.From
    $to = Join-Path $RepoRoot $r.To
    if (Test-Path $from) {
        if (-not (Test-Path $to)) {
            Write-Host "  mv $($r.From) -> $($r.To)"
            git mv $from $to
        }
    }
}

# csproj renames inside renamed directories
$csprojRenames = @(
    "packages\NevaxisZentra.Configuration\NevaxisZentra.Configuration.csproj",
    "packages\NevaxisZentra.Persistence\NevaxisZentra.Persistence.csproj",
    "packages\NevaxisZentra.Persistence.Migrations\NevaxisZentra.Persistence.Migrations.csproj",
    "packages\NevaxisZentra.EdgeBuffer\NevaxisZentra.EdgeBuffer.csproj",
    "apps\desktop\NevaxisZentra.Desktop\NevaxisZentra.Desktop.csproj",
    "tools\NevaxisZentra.DatabaseTool\NevaxisZentra.DatabaseTool.csproj",
    "tools\NevaxisZentra.LogoBridge\NevaxisZentra.LogoBridge.csproj",
    "tools\NevaxisZentra.MikroBridge\NevaxisZentra.MikroBridge.csproj",
    "tools\NevaxisZentra.NetsisBridge\NevaxisZentra.NetsisBridge.csproj",
    "tools\NevaxisZentra.MachineSimulator\NevaxisZentra.MachineSimulator.csproj",
    "tests\NevaxisZentra.Persistence.Tests\NevaxisZentra.Persistence.Tests.csproj",
    "tests\NevaxisZentra.Desktop.Tests\NevaxisZentra.Desktop.Tests.csproj",
    "tests\NevaxisZentra.SmokeTests\NevaxisZentra.SmokeTests.csproj"
)

foreach ($oldRel in $csprojRenames) {
    $oldPath = Join-Path $RepoRoot $oldRel
    if (Test-Path $oldPath) {
        $newName = (Split-Path $oldRel -Leaf).Replace("NevaxisZentra", "NevaxisZentra")
        $newPath = Join-Path (Split-Path $oldPath -Parent) $newName
        Write-Host "  mv $oldRel -> $(Split-Path $newPath -Parent | Split-Path -Leaf)\$newName"
        git mv $oldPath $newPath
    }
}

Write-Host "== Phase 2: content replacements =="

$textReplacements = @(
    @{ Old = "NevaxisZentra"; New = "NevaxisZentra" },
    @{ Old = "nevaxiszentra"; New = "nevaxiszentra" },
    @{ Old = "NEVAXISZENTRA"; New = "NEVAXISZENTRA" },
    @{ Old = "Nevaxis Zentra"; New = "Nevaxis Zentra" },
    @{ Old = "NEVAXIS ZENTRA"; New = "NEVAXIS ZENTRA" },
    @{ Old = "NEVAXIS_FACTOS_"; New = "NEVAXIS_ZENTRA_" }
)

$extensions = @("*.cs", "*.csproj", "*.sln", "*.json", "*.md", "*.ps1", "*.mjs", "*.ts", "*.tsx", "*.css", "*.yaml", "*.yml", "*.wxs", "*.wixproj", "*.xaml", "*.env*", "*.txt", "*.rtf", "*.props", "*.targets", "*.sql", "*.html", "*.xml")

$files = Get-ChildItem -Path $RepoRoot -Recurse -File -Include $extensions -ErrorAction SilentlyContinue |
    Where-Object { -not (Should-SkipPath $_.FullName) }

$changed = 0
foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $original = $content
    foreach ($r in $textReplacements) {
        $content = $content.Replace($r.Old, $r.New)
    }
    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $content)
        $changed++
    }
}

Write-Host "  Updated $changed files"

Write-Host "== Phase 3: web brand file rename =="
$brandOld = Join-Path $RepoRoot "apps\web\src\lib\brand\factos-brand.ts"
$brandNew = Join-Path $RepoRoot "apps\web\src\lib\brand\zentra-brand.ts"
if (Test-Path $brandOld) {
    git mv $brandOld $brandNew
}

$authHeroOld = Join-Path $RepoRoot "apps\web\src\components\brand\factos-auth-hero.tsx"
$authHeroNew = Join-Path $RepoRoot "apps\web\src\components\brand\zentra-auth-hero.tsx"
if (Test-Path $authHeroOld) { git mv $authHeroOld $authHeroNew }

$lockupOld = Join-Path $RepoRoot "apps\web\src\components\brand\factos-brand-lockup.tsx"
$lockupNew = Join-Path $RepoRoot "apps\web\src\components\brand\zentra-brand-lockup.tsx"
if (Test-Path $lockupOld) { git mv $lockupOld $lockupNew }

Write-Host "== Done =="
