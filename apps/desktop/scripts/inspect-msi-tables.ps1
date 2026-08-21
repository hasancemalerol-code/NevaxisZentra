param(
    [Parameter(Mandatory = $true)][string]$MsiPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-MsiRows {
    param(
        [Parameter(Mandatory = $true)][string]$Query
    )

    $installer = New-Object -ComObject WindowsInstaller.Installer
    $database = $installer.OpenDatabase($MsiPath, 0)
    $view = $database.OpenView($Query)
    $view.Execute()
    $rows = New-Object System.Collections.Generic.List[string]
    while ($record = $view.Fetch()) {
        $values = @()
        for ($i = 1; $i -le $record.FieldCount; $i++) {
            $values += $record.GetFieldData($i)
        }
        $rows.Add(($values -join " | "))
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($record)
    }
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($view)
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($database)
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($installer)
    return $rows
}

Write-Host "=== Media ==="
Get-MsiRows "SELECT Cabinet, LastSequence, DiskPrompt, VolumeLabel FROM Media"

Write-Host "=== Upgrade ==="
Get-MsiRows "SELECT UpgradeCode, VersionMin, VersionMax, Attributes, ActionProperty FROM Upgrade"

Write-Host "=== Directory ==="
Get-MsiRows "SELECT Directory, Directory_Parent, DefaultDir FROM Directory"

Write-Host "=== Component ==="
Get-MsiRows "SELECT Component, ComponentId, Directory_, Attributes, KeyPath FROM Component"

Write-Host "=== Shortcut ==="
Get-MsiRows "SELECT Shortcut, Directory_, Name, Component_, Target, Description FROM Shortcut"

Write-Host "=== Registry ==="
Get-MsiRows "SELECT Registry, Root, Key, Name, Value, Component_ FROM Registry"

Write-Host "=== Property ==="
Get-MsiRows "SELECT Property, Value FROM Property"
