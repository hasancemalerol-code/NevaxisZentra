# Scans release artifacts for prohibited secrets and development files.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ArtifactRoot
)

$ErrorActionPreference = "Stop"

$prohibitedPatterns = @(
    "\.env$",
    "\.env\.",
    "appsettings\.Development",
    "\\.git\\",
    "node_modules",
    "id_rsa",
    "private\.key",
    "secret",
    "password\s*="
)

$failures = @()
Get-ChildItem $ArtifactRoot -Recurse -File | ForEach-Object {
    foreach ($pattern in $prohibitedPatterns) {
        if ($_.FullName -match $pattern -or $_.Name -match $pattern) {
            $failures += $_.FullName
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Error ("Security scan failed. Prohibited artifacts:`n" + ($failures -join "`n"))
}

Write-Host "Security artifact scan PASS for $ArtifactRoot"
