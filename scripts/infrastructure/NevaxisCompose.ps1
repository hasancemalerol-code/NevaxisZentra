# Resolves docker compose arguments for this checkout.
# A normal development checkout keeps infrastructure/compose.yaml and its
# NevaxisZentra-local project. The release-audit checkout uses a separate
# project, container, volume, and network.

function Get-NevaxisComposeArguments {
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot
    )

    $baseFile = Join-Path $RepositoryRoot 'infrastructure\compose.yaml'
    $auditFile = Join-Path $RepositoryRoot 'infrastructure\compose.audit.yaml'
    $useAuditOverlay = (Split-Path -Leaf $RepositoryRoot) -eq 'NevaxisZentra-release-audit'

    if (-not $useAuditOverlay) {
        $environmentFile = Join-Path $RepositoryRoot 'infrastructure\.env'
        if (Test-Path -LiteralPath $environmentFile) {
            foreach ($line in Get-Content -LiteralPath $environmentFile) {
                if ($line -match '^\s*NEVAXIS_ZENTRA_INFRA_PROFILE\s*=\s*audit\s*$') {
                    $useAuditOverlay = $true
                    break
                }
            }
        }
    }

    if ($useAuditOverlay) {
        if (-not (Test-Path -LiteralPath $auditFile)) {
            throw "Release-audit compose overlay is missing: $auditFile"
        }

        return @(
            '-p', 'nevaxiszentra-release-audit',
            '-f', $baseFile,
            '-f', $auditFile)
    }

    return @('-f', $baseFile)
}
