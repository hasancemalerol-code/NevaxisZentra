# Deploys canonical Development Pilot secrets to machine-local packaged runtime config.
# Does NOT print secret values.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$deployScript = Join-Path $PSScriptRoot "deploy-packaged-runtime-local-secrets.mjs"
if (-not (Test-Path $deployScript)) {
    throw "Missing deploy script: $deployScript"
}

& node $deployScript $RepositoryRoot
if ($LASTEXITCODE -ne 0) {
    throw "Packaged runtime local secret deployment failed."
}

Write-Host "Target: $env:LOCALAPPDATA\NevaxisFactOS\config\runtime.env.local"
