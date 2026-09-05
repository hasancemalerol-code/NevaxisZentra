# Ensures canonical Development Pilot credentials are available for desktop flows.
# Does NOT rotate or reseed passwords — only validates local secret presence and readiness.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$loader = Join-Path $RepositoryRoot "scripts\shared\load-development-saas-identity-secrets.mjs"
$probe = Join-Path $RepositoryRoot "scripts\desktop\probe-development-saas-identity-sign-in.mjs"

if (-not (Test-Path $loader)) {
    throw "Missing secret loader: $loader"
}

Write-Host "Checking canonical Development Pilot secret sources..."
& node $loader
if ($LASTEXITCODE -ne 0) {
    throw @"
Development Pilot credentials are not configured.
Add DevelopmentSaasIdentity__TenantOwnerPassword and DevelopmentSaasIdentity__MfaSharedSecret
to infrastructure/.env.local (preferred) or infrastructure/.env.
This script does not rotate passwords.
"@
}

if (-not (Test-Path $probe)) {
    throw "Missing credential readiness probe: $probe"
}

Write-Host "Probing sign-in readiness (no password mutation)..."
& node $probe
if ($LASTEXITCODE -ne 0) {
    throw "Development Pilot credential readiness probe failed."
}

Write-Host "DEVELOPMENT_SAAS_IDENTITY_SECRETS: READY"
