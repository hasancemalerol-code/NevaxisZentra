param(
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$ProductionWebUrl,
    [switch]$DevelopmentDistribution
)

$embeddedLiteral = if ([string]::IsNullOrWhiteSpace($ProductionWebUrl)) { "null" } else { "`"$ProductionWebUrl`"" }
$devLiteral = if ($DevelopmentDistribution) { "true" } else { "false" }

$lines = @(
    "#nullable enable"
    "namespace NevaxisFactOS.Desktop.Configuration;"
    ""
    "internal static class DesktopBuildConfiguration"
    "{"
    "    internal const string? EmbeddedProductionWebUrl = $embeddedLiteral;"
    "    internal const bool DevelopmentDistribution = $devLiteral;"
    "}"
)

$directory = Split-Path -Parent $OutputPath
if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8
