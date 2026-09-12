# Fix incorrect CrevaFactOS -> NevaxisFactOS replacements in env keys and DB infrastructure names.
# CREVAFACTOS/crevafactos were corrupted because CrevaFactOS is a case-insensitive substring.

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $repoRoot

function Test-ExcludedPath {
    param([string]$RelativePath)
    $normalized = $RelativePath -replace '\\', '/'
    $excludePatterns = @(
        '.cursor-screenshots/',
        '.build/',
        '/bin/',
        '/obj/',
        '/node_modules/',
        '/artifacts/',
        '/.git/'
    )
    foreach ($pattern in $excludePatterns) {
        if ($normalized -like "*$pattern*") { return $true }
    }
    $fileName = Split-Path -Leaf $normalized
    if ($fileName -like 'CREVAFACTOS-*') { return $true }
    return $false
}

$extensions = @(
    '.cs', '.csproj', '.sln', '.json', '.md', '.yaml', '.yml', '.ps1', '.mjs', '.js', '.ts', '.tsx',
    '.sql', '.py', '.props', '.targets', '.xaml', '.config', '.env', '.txt', '.sh', '.xml', '.html', '.css', '.wxs', '.rtf'
)

$replacements = @(
    @{ Pattern = 'NEVAXIS_FACTOS__'; Replacement = 'NEVAXIS_FACTOS__' },
    @{ Pattern = 'NEVAXIS_FACTOS_POSTGRES_'; Replacement = 'NEVAXIS_FACTOS_POSTGRES_' },
    @{ Pattern = 'NEVAXIS_FACTOS_RABBITMQ_'; Replacement = 'NEVAXIS_FACTOS_RABBITMQ_' },
    @{ Pattern = 'NEVAXIS_FACTOS_API_BASE_URL'; Replacement = 'NEVAXIS_FACTOS_API_BASE_URL' },
    @{ Pattern = 'NEVAXIS_FACTOS_API_BASE'; Replacement = 'NEVAXIS_FACTOS_API_BASE' },
    @{ Pattern = 'NEVAXIS_FACTOS_WEB_BASE_URL'; Replacement = 'NEVAXIS_FACTOS_WEB_BASE_URL' },
    @{ Pattern = 'NEVAXIS_FACTOS_WEB_APPLICATION_NAME'; Replacement = 'NEVAXIS_FACTOS_WEB_APPLICATION_NAME' },
    @{ Pattern = 'NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE'; Replacement = 'NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE' },
    @{ Pattern = 'NEVAXIS_FACTOS_DEV_AUTH_ISSUER'; Replacement = 'NEVAXIS_FACTOS_DEV_AUTH_ISSUER' },
    @{ Pattern = 'NEVAXIS_FACTOS_DEV_AUTH_SUBJECT'; Replacement = 'NEVAXIS_FACTOS_DEV_AUTH_SUBJECT' },
    @{ Pattern = 'NEVAXIS_FACTOS_DEV_TENANT_KEY'; Replacement = 'NEVAXIS_FACTOS_DEV_TENANT_KEY' },
    @{ Pattern = 'NEVAXIS_FACTOS_POSTGRES_PASSWORD'; Replacement = 'NEVAXIS_FACTOS_POSTGRES_PASSWORD' },
    @{ Pattern = 'crevafactos_local'; Replacement = 'crevafactos_local' },
    @{ Pattern = 'crevafactos_runtime'; Replacement = 'crevafactos_runtime' },
    @{ Pattern = 'crevafactos-dev'; Replacement = 'crevafactos-dev' },
    @{ Pattern = '"crevafactos"'; Replacement = '"crevafactos"' },
    @{ Pattern = "'crevafactos'"; Replacement = "'crevafactos'" },
    @{ Pattern = 'crevafactos-api'; Replacement = 'crevafactos-api' },
    @{ Pattern = 'NEVAXIS_FACTOS_'; Replacement = 'NEVAXIS_FACTOS_' }
)

$updatedCount = 0
Get-ChildItem -Path $repoRoot -Recurse -File |
    Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1)
        if (Test-ExcludedPath -RelativePath $relative) { return }

        $content = [System.IO.File]::ReadAllText($_.FullName)
        $original = $content
        foreach ($map in $replacements) {
            $content = $content -replace [regex]::Escape($map.Pattern), $map.Replacement
        }

        if ($content -ne $original) {
            [System.IO.File]::WriteAllText($_.FullName, $content)
            $updatedCount++
        }
    }

Write-Host "Fixed $updatedCount files."
