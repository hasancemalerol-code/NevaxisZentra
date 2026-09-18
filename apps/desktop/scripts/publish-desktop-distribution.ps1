param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$ProductionWebUrl = $env:NEVAXIS_FACTOS_DESKTOP_PRODUCTION_WEB_URL,

    [switch]$AllowDevelopmentDistribution,

    [switch]$IncludeDiagnostics,

    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot "..\..\..\artifacts\desktop-distribution"
}

. "$PSScriptRoot\..\distribution\DesktopDistributionVersion.ps1"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$desktopProject = Join-Path $repoRoot "apps\desktop\NevaxisZentra.Desktop\NevaxisZentra.Desktop.csproj"
$installerProject = Join-Path $repoRoot "apps\desktop\installer\NevaxisZentra.Desktop.Installer.wixproj"
$redistDir = Join-Path $repoRoot "apps\desktop\installer\redistributables"
$redistCacheDir = Join-Path $repoRoot "apps\desktop\installer\.cache\webview2"
$redistManifest = Join-Path $redistDir "webview2-bootstrapper.json"
$versionJsonPath = Join-Path $repoRoot "apps\desktop\distribution\version.json"

if (-not (Test-DesktopSemanticVersion -Version $Version)) {
    throw "Invalid version '$Version'."
}

$msiProductVersion = ConvertTo-DesktopMsiProductVersion -Version $Version
$assemblyVersion = ConvertTo-DesktopAssemblyVersion -Version $Version
$productCode = New-DesktopProductCode -Version $Version
$classification = "UNSIGNED_DEVELOPMENT"
$distributionMode = "Development"
$configuredOrigin = "http://127.0.0.1:3001"
$publishProperties = @(
    "-p:DesktopDistributionVersion=$Version"
    "-p:DesktopAssemblyVersion=$assemblyVersion"
    "-p:DesktopFileVersion=$assemblyVersion"
    "-p:DesktopDevelopmentDistribution=true"
)

$urlValidation = if ([string]::IsNullOrWhiteSpace($ProductionWebUrl)) {
    [pscustomobject]@{ Valid = $false; Reason = "missing" }
}
else {
    Test-DesktopProductionWebUrl -Url $ProductionWebUrl
}
if ($urlValidation.Valid) {
    $distributionMode = "Production"
    $configuredOrigin = $urlValidation.NormalizedOrigin
    $publishProperties = @(
        "-p:DesktopDistributionVersion=$Version"
        "-p:DesktopAssemblyVersion=$assemblyVersion"
        "-p:DesktopFileVersion=$assemblyVersion"
        "-p:DesktopProductionWebUrl=$($urlValidation.NormalizedOrigin)"
    )
}
elseif (-not $AllowDevelopmentDistribution) {
    throw "Production packaging blocked: NEVAXIS_FACTOS_DESKTOP_PRODUCTION_WEB_URL is missing or invalid ($($urlValidation.Reason)). Pass -AllowDevelopmentDistribution to produce a Development-only artifact."
}

$artifactDir = Join-Path $OutputRoot $Version
$publishDir = Join-Path $artifactDir "publish"
$stagingDir = Join-Path $artifactDir "staging"
$diagnosticsDir = Join-Path $artifactDir "diagnostics"

if (Test-Path $artifactDir) {
    Remove-Item -Recurse -Force $artifactDir
}
New-Item -ItemType Directory -Force -Path $publishDir, $stagingDir | Out-Null

Write-Host "Publishing NevaxisZentra Desktop $Version ($distributionMode)..."

dotnet publish $desktopProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishTrimmed=false `
    /p:PublishSingleFile=false `
    /p:DesktopIncludeDebugSymbolsInPublish=false `
    @publishProperties `
    -o $publishDir

$exePath = Join-Path $publishDir "NevaxisZentra.Desktop.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish did not produce NevaxisZentra.Desktop.exe"
}

function Ensure-WebView2Bootstrapper {
    $manifest = Get-Content $redistManifest -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($manifest.sha256) -or $manifest.sha256 -eq "PLACEHOLDER_WILL_BE_SET_AFTER_DOWNLOAD") {
        throw "WebView2 bootstrapper manifest is missing a pinned SHA-256."
    }

    New-Item -ItemType Directory -Force -Path $redistCacheDir | Out-Null
    $bootstrapperPath = Join-Path $redistCacheDir $manifest.fileName
    if (-not (Test-Path $bootstrapperPath)) {
        Write-Host "Downloading Microsoft WebView2 Evergreen bootstrapper..."
        Invoke-WebRequest -Uri $manifest.sourceUrl -OutFile $bootstrapperPath -UseBasicParsing
    }

    $hash = (Get-FileHash -Algorithm SHA256 -Path $bootstrapperPath).Hash.ToLowerInvariant()
    if ($manifest.sha256 -ne $hash) {
        Remove-Item -Force $bootstrapperPath
        throw "WebView2 bootstrapper SHA-256 mismatch. Expected $($manifest.sha256) but got $hash."
    }

    return $bootstrapperPath
}

$bootstrapperPath = Ensure-WebView2Bootstrapper

$supportPublishDir = Join-Path $artifactDir "publish-support"
if (Test-Path $supportPublishDir) { Remove-Item -Recurse -Force $supportPublishDir }
New-Item -ItemType Directory -Force -Path $supportPublishDir | Out-Null
Get-ChildItem -Path $publishDir -Force | Where-Object { $_.Name -ne "NevaxisZentra.Desktop.exe" } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination (Join-Path $supportPublishDir $_.Name) -Recurse -Force
}

$portableZipName = "NevaxisZentra-Desktop-$Version-win-x64.zip"
$portableZipPath = Join-Path $artifactDir $portableZipName
if (Test-Path $portableZipPath) { Remove-Item -Force $portableZipPath }
& tar.exe -a -c -f $portableZipPath -C $publishDir .
if ($LASTEXITCODE -ne 0) {
    throw "Failed to create portable ZIP at $portableZipPath"
}

$msiName = "NevaxisZentra-Desktop-Setup-$Version-x64.msi"
$msiPath = Join-Path $artifactDir $msiName

Push-Location (Join-Path $repoRoot "apps\desktop\installer")
try {
    $wixExe = (Get-Command wix.exe -ErrorAction Stop).Source
    $wixArgs = @(
        "build", "Package.wxs",
        "-arch", "x64",
        "-d", "ProductName=NevaxisZentra",
        "-d", "Publisher=NevaxisZentra",
        "-d", "ProductVersion=$msiProductVersion",
        "-d", "UpgradeCode={8C4E2F91-6D3A-4B17-9E2C-1A5F00000001}",
        "-d", "PublishDir=$publishDir",
        "-d", "SupportPublishDir=$supportPublishDir",
        "-d", "WebView2BootstrapperPath=$bootstrapperPath",
        "-ext", "WixToolset.Util.wixext",
        "-ext", "WixToolset.UI.wixext",
        "-out", $msiPath
    )
    & $wixExe @wixArgs
    if ($LASTEXITCODE -ne 0) {
        throw "WiX build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $msiPath)) {
    throw "MSI build failed: $msiPath not found."
}

function Invoke-OptionalAuthenticodeSigning {
    param(
        [Parameter(Mandatory = $true)][string[]]$Paths
    )

    $thumbprint = $env:NEVAXIS_FACTOS_DESKTOP_CODESIGN_CERT_THUMBPRINT
    $pfxPath = $env:NEVAXIS_FACTOS_DESKTOP_CODESIGN_PFX_PATH
    $pfxPassword = $env:NEVAXIS_FACTOS_DESKTOP_CODESIGN_PFX_PASSWORD
    $timestampUrl = if ($env:NEVAXIS_FACTOS_DESKTOP_CODESIGN_TIMESTAMP_URL) { $env:NEVAXIS_FACTOS_DESKTOP_CODESIGN_TIMESTAMP_URL } else { "http://timestamp.digicert.com" }

    if ([string]::IsNullOrWhiteSpace($thumbprint) -and [string]::IsNullOrWhiteSpace($pfxPath)) {
        return "UNSIGNED_DEVELOPMENT"
    }

    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (-not $signtool) {
        throw "signtool.exe not found but signing credentials were supplied."
    }

    foreach ($path in $Paths) {
        if ($thumbprint) {
            & signtool.exe sign /fd SHA256 /tr $timestampUrl /td SHA256 /sha1 $thumbprint $path | Out-Null
        }
        else {
            if ([string]::IsNullOrWhiteSpace($pfxPassword)) {
                throw "NEVAXIS_FACTOS_DESKTOP_CODESIGN_PFX_PASSWORD is required when using a PFX path."
            }

            & signtool.exe sign /fd SHA256 /tr $timestampUrl /td SHA256 /f $pfxPath /p $pfxPassword $path | Out-Null
        }

        & signtool.exe verify /pa $path | Out-Null
    }

    return "SIGNED_PRODUCTION"
}

$signTargets = @($exePath, $msiPath)
$classification = Invoke-OptionalAuthenticodeSigning -Paths $signTargets

function Get-Sha256Hex([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash.ToLowerInvariant()
}

$msiHash = Get-Sha256Hex $msiPath
$zipHash = Get-Sha256Hex $portableZipPath
$commitHash = (git -C $repoRoot rev-parse HEAD).Trim()
$buildUtc = (Get-Date).ToUniversalTime().ToString("o")

$manifest = [ordered]@{
    product = "NevaxisZentra"
    version = $Version
    architecture = "x64"
    buildConfiguration = "Release"
    targetFramework = "net10.0-windows"
    distributionClassification = $distributionMode
    configuredOrigin = $configuredOrigin
    signingState = $classification
    productCode = $productCode
    upgradeCode = "{8C4E2F91-6D3A-4B17-9E2C-1A5F00000001}"
    msiProductVersion = $msiProductVersion
    artifacts = [ordered]@{
        msi = $msiName
        portableZip = $portableZipName
    }
    sha256 = [ordered]@{
        msi = $msiHash
        portableZip = $zipHash
    }
    buildUtc = $buildUtc
    sourceCommit = $commitHash
}

$manifestPath = Join-Path $artifactDir "distribution-manifest.json"
Write-Utf8NoBomFile -Path $manifestPath -Content ($manifest | ConvertTo-Json -Depth 6)

$checksumPath = Join-Path $artifactDir "SHA256SUMS.txt"
@(
    "$msiHash  $msiName"
    "$zipHash  $portableZipName"
) | Set-Content -Path $checksumPath -Encoding ASCII

if ($IncludeDiagnostics) {
    New-Item -ItemType Directory -Force -Path $diagnosticsDir | Out-Null
    $diagZip = Join-Path $artifactDir "NevaxisZentra-Desktop-$Version-diagnostics-win-x64.zip"
    dotnet publish $desktopProject -c Release -r win-x64 --self-contained true /p:DesktopIncludeDebugSymbolsInPublish=true /p:DebugType=portable /p:DebugSymbols=true @publishProperties -o (Join-Path $diagnosticsDir "publish-symbols")
    if (Test-Path $diagZip) { Remove-Item -Force $diagZip }
    Compress-Archive -Path (Join-Path $diagnosticsDir "*") -DestinationPath $diagZip
}

# Keep canonical version file aligned for verifiers
Write-Utf8NoBomFile -Path $versionJsonPath -Content (@{ semanticVersion = $Version; packagedAtUtc = $buildUtc; mode = $distributionMode } | ConvertTo-Json)

Write-Host ""
Write-Host "Distribution artifacts:"
Write-Host "  MSI:      $msiPath"
Write-Host "  ZIP:      $portableZipPath"
Write-Host "  Manifest: $manifestPath"
Write-Host "  Checksum: $checksumPath"
Write-Host "  Mode:     $distributionMode"
Write-Host "  Signing:  $classification"
