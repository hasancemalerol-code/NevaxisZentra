# Fails packaging when the built Next.js app does not proxy the required /api/* routes to the API.
# next.config.ts computes rewrites at BUILD time from NEVAXIS_FACTOS_API_BASE_URL; when that value
# is absent the packaged web server silently has no proxy and browser sign-in cannot work.

[CmdletBinding()]
param(
    [string]$WebAppRoot = "",
    [string]$ManifestPath = "",
    [string]$ApiBaseUrl = "http://127.0.0.1:5000"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    if ([string]::IsNullOrWhiteSpace($WebAppRoot)) {
        throw "Provide -WebAppRoot (directory containing .next) or -ManifestPath."
    }
    $ManifestPath = Join-Path $WebAppRoot ".next\routes-manifest.json"
}

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "Release gate failed: routes manifest not found at $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$rewrites = @()
foreach ($phase in 'beforeFiles', 'afterFiles', 'fallback') {
    if ($manifest.rewrites -and $manifest.rewrites.PSObject.Properties.Name -contains $phase) {
        $rewrites += @($manifest.rewrites.$phase)
    }
}
if ($manifest.rewrites -is [System.Array]) {
    $rewrites += @($manifest.rewrites)
}

$base = $ApiBaseUrl.TrimEnd('/')
$required = @(
    '/api/identity/:path*',
    '/api/admin-access/:path*',
    '/api/saas-subscription/:path*',
    '/api/public/ai-task-access/:path*'
)

$missing = @()
foreach ($source in $required) {
    $expectedDestination = "$base$source"
    $match = $rewrites | Where-Object { $_.source -eq $source -and $_.destination -eq $expectedDestination }
    if (-not $match) { $missing += $source }
}

if ($missing.Count -gt 0) {
    throw ("Release gate failed: packaged web build is missing API rewrites to $base for: " + ($missing -join ', ') +
        ". Build the web app with NEVAXIS_FACTOS_API_BASE_URL=$base.")
}

Write-Host "WEB_API_REWRITES_VALIDATION: PASS ($($required.Count) rewrites -> $base)"
