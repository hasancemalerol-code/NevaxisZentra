param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ComposeArgs
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $PSScriptRoot 'NevaxisCompose.ps1')

if (-not $ComposeArgs -or $ComposeArgs.Count -eq 0) {
    throw 'Provide docker compose arguments, for example: up -d postgres'
}

& docker compose @(Get-NevaxisComposeArguments -RepositoryRoot $repositoryRoot) @ComposeArgs
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit code $LASTEXITCODE."
}
