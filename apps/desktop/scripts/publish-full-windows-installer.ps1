# Builds Nevaxis-FactOS-Setup.exe from release staging using WiX.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StagingRoot,
    [Parameter(Mandatory = $true)][string]$ArtifactRoot,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$SourceCommit
)

$ErrorActionPreference = "Stop"

function Write-Utf8NoBomFile {
    param([string]$Path, [string]$Content)
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-Sha256Hex([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash.ToLowerInvariant()
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$installerSource = Join-Path $repoRoot "apps\desktop\installer\Package-FullProduct.wxs"
$redistDir = Join-Path $repoRoot "apps\desktop\installer\redistributables"
$redistCacheDir = Join-Path $repoRoot "apps\desktop\installer\.cache\webview2"
$redistManifest = Join-Path $redistDir "webview2-bootstrapper.json"

if (-not (Test-Path $StagingRoot)) {
    throw "Staging root not found: $StagingRoot"
}

New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null
$stagingRootResolved = (Resolve-Path $StagingRoot).Path
if ($Version -match '^(\d+)\.(\d+)\.(\d+)') {
    $msiProductVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"
}
elseif ($Version -match '^(\d+)\.(\d+)') {
    $msiProductVersion = "$($Matches[1]).$($Matches[2]).0.0"
}
else {
    $msiProductVersion = "2026.9.16.0"
}
$versionSlug = ($Version -replace '[^\d\.A-Za-z\-]', '-')
$setupMsiName = "Nevaxis-FactOS-Setup.msi"
$versionedMsiName = "Nevaxis-FactOS-Setup-$versionSlug.msi"
$setupExeName = "Nevaxis-FactOS-Setup.exe"
$setupMsiPath = Join-Path $ArtifactRoot $setupMsiName
$setupExePath = Join-Path $ArtifactRoot $setupExeName

function Ensure-WebView2Bootstrapper {
    $manifest = Get-Content $redistManifest -Raw | ConvertFrom-Json
    New-Item -ItemType Directory -Force -Path $redistCacheDir | Out-Null
    $bootstrapperPath = Join-Path $redistCacheDir $manifest.fileName
    if (-not (Test-Path $bootstrapperPath)) {
        Invoke-WebRequest -Uri $manifest.sourceUrl -OutFile $bootstrapperPath -UseBasicParsing
    }
    return $bootstrapperPath
}

$bootstrapperPath = Ensure-WebView2Bootstrapper

Push-Location (Join-Path $repoRoot "apps\desktop\installer")
try {
    $wixExe = (Get-Command wix.exe -ErrorAction Stop).Source
    $wixArgs = @(
        "build", $installerSource,
        "-arch", "x64",
        "-d", "ProductVersion=$msiProductVersion",
        "-d", "StagingRoot=$stagingRootResolved",
        "-d", "WebView2BootstrapperPath=$bootstrapperPath",
        "-ext", "WixToolset.Util.wixext",
        "-ext", "WixToolset.UI.wixext",
        "-out", $setupMsiPath
    )
    & $wixExe @wixArgs
    if ($LASTEXITCODE -ne 0) { throw "WiX MSI build failed." }
}
finally {
    Pop-Location
}

if (-not (Test-Path $setupMsiPath)) {
    throw "Installer MSI not produced: $setupMsiPath"
}

$versionedMsiPath = Join-Path $ArtifactRoot $versionedMsiName
Copy-Item -Path $setupMsiPath -Destination $versionedMsiPath -Force

$setupHash = Get-Sha256Hex $setupMsiPath
$versionedHash = Get-Sha256Hex $versionedMsiPath
$setupSize = (Get-Item $setupMsiPath).Length

$readmePath = Join-Path $ArtifactRoot "README-DEMO.txt"
Write-Utf8NoBomFile -Path $readmePath -Content @"
NEVAXIS FACTOS
Factory Intelligence & Action OS

INSTALLATION
1. Run Nevaxis-FactOS-Setup.msi
2. Complete installation
3. Launch Nevaxis FactOS from Start Menu or Desktop
4. Login with the demo account below

DEMO LOGIN
Email: admin@demo.nevaxisfactos.local
Password: NevaxisDemo2026!

GOLDEN DEMO ACCESS
- Production Recovery: Altın Demo — Üretim Kurtarma
- Customs File Digital Worker: Altın Demo — Gümrük Dosyası

RECOVERY
- Close and relaunch the application
- Use demo reset controls inside each Golden Demo screen

"@

$checksumPath = Join-Path $ArtifactRoot "SHA256SUMS.txt"
@(
    "$setupHash  $setupMsiName",
    "$versionedHash  $versionedMsiName"
) | Set-Content -Path $checksumPath -Encoding ASCII

$zipPath = Join-Path $ArtifactRoot "Nevaxis-FactOS-Release-$versionSlug.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path $versionedMsiPath, $readmePath, $checksumPath -DestinationPath $zipPath -Force
$zipHash = Get-Sha256Hex $zipPath
Add-Content -Path $checksumPath -Value "$zipHash  Nevaxis-FactOS-Demo.zip" -Encoding ASCII

$manifestPath = Join-Path $ArtifactRoot "NEVAXISFACTOS-WINDOWS-RELEASE-MANIFEST.md"
Write-Utf8NoBomFile -Path $manifestPath -Content @"
# Nevaxis FactOS Windows Release Manifest

- Product: Nevaxis FactOS
- Release: Full Current Windows Release
- Version: $Version
- Source freeze: $SourceCommit
- Freeze tag: factos-final-sales-freeze-2026-09
- Installer: $setupMsiName
- Versioned installer: $versionedMsiName
- Installer size bytes: $setupSize
- Installer size MB: $([math]::Round($setupSize / 1MB, 2))
- Installer SHA-256: $setupHash
- Versioned installer SHA-256: $versionedHash
- ZIP: Nevaxis-FactOS-Release-$versionSlug.zip
- ZIP size bytes: $((Get-Item $zipPath).Length)
- ZIP SHA-256: $zipHash
- Database strategy: EMBEDDED_LOCAL PostgreSQL
- Persistent data: %PROGRAMDATA%\Nevaxis\FactOS
- Machine secrets: %LOCALAPPDATA%\NevaxisFactOS\config\runtime.env.local
"@

Write-Host "Installer: $setupMsiPath ($([math]::Round($setupSize / 1MB, 2)) MB)"
Write-Host "Distribution ZIP: $zipPath ($([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB)"
