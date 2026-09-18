Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-DesktopSemanticVersion {
    param([Parameter(Mandatory = $true)][string]$Version)

    return [bool]($Version -match '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')
}

function ConvertTo-DesktopMsiProductVersion {
    param([Parameter(Mandatory = $true)][string]$Version)

    if (-not (Test-DesktopSemanticVersion -Version $Version)) {
        throw "Invalid desktop distribution version '$Version'. Expected semantic version format, for example 1.0.0."
    }

    $core = ($Version -split '[+-]', 2)[0]
    $parts = $core.Split('.')
    while ($parts.Count -lt 4) {
        $parts += "0"
    }

    return ($parts[0..3] -join ".")
}

function ConvertTo-DesktopAssemblyVersion {
    param([Parameter(Mandatory = $true)][string]$Version)

    return ConvertTo-DesktopMsiProductVersion -Version $Version
}

function New-DesktopProductCode {
    param([Parameter(Mandatory = $true)][string]$Version)

    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("NevaxisZentra-Desktop-$Version"))
    }
    finally {
        $md5.Dispose()
    }

    $bytes = [byte[]]$hash
    $bytes[6] = ($bytes[6] -band 0x0F) -bor 0x40
    $bytes[8] = ($bytes[8] -band 0x3F) -bor 0x80
    return ([System.Guid]::new($bytes)).ToString().ToUpperInvariant()
}

function Test-DesktopProductionWebUrl {
    param([Parameter(Mandatory = $true)][string]$Url)

    if ([string]::IsNullOrWhiteSpace($Url)) {
        return [pscustomobject]@{ Valid = $false; Reason = "missing" }
    }

    $trimmed = $Url.Trim()
    if (-not [Uri]::TryCreate($trimmed, [UriKind]::Absolute, [ref]$null)) {
        return [pscustomobject]@{ Valid = $false; Reason = "invalid-uri" }
    }

    $uri = [Uri]$trimmed
    $blockedSchemes = @("file", "javascript", "data", "vbscript", "custom")
    if ($blockedSchemes -contains $uri.Scheme.ToLowerInvariant()) {
        return [pscustomobject]@{ Valid = $false; Reason = "unsafe-scheme" }
    }

    if ($uri.Scheme -ne "https") {
        return [pscustomobject]@{ Valid = $false; Reason = "https-required" }
    }

    $hostName = $uri.Host.ToLowerInvariant()
    if ($hostName -in @("localhost", "127.0.0.1", "::1")) {
        return [pscustomobject]@{ Valid = $false; Reason = "localhost-not-allowed" }
    }

    return [pscustomobject]@{ Valid = $true; Reason = "valid"; NormalizedOrigin = "$($uri.Scheme)://$($uri.Authority)" }
}
