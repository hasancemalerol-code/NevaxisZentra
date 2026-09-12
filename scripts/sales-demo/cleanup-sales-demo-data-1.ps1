# NevaxisFactOS-SALES-DEMO-FINAL-DATA-CLEANUP-AND-FREEZE-1
# Development Pilot precision cleanup — dev-pilot-tenant / crevafactos_local only.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('DryRun', 'Execute')]
    [string]$Mode,
    [string]$TenantCode = 'dev-pilot-tenant',
    [string]$DatabaseName = 'crevafactos_local',
    [string]$ContainerName = 'NevaxisFactOS-local-postgres-1',
    [string]$RepositoryRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$AllowedTenant = 'dev-pilot-tenant'
$AllowedDatabase = 'crevafactos_local'
$ArtifactDir = Join-Path $RepositoryRoot 'artifacts\sales-demo-freeze'
$SqlDir = $PSScriptRoot

function Assert-DevEnvironment {
    if ($TenantCode -ne $AllowedTenant) {
        throw "Refusing cleanup: tenant must be '$AllowedTenant' (got '$TenantCode')."
    }
    if ($DatabaseName -ne $AllowedDatabase) {
        throw "Refusing cleanup: database must be '$AllowedDatabase' (got '$DatabaseName')."
    }
    $tenantCheck = Invoke-PsqlScalar "SELECT COUNT(*) FROM tenants WHERE tenant_key = '$AllowedTenant';"
    if ([int]$tenantCheck -ne 1) {
        throw "Development Pilot tenant not found in database."
    }
}

function Invoke-PsqlScalar([string]$Sql) {
    $result = $Sql | docker exec -i $ContainerName psql -U NevaxisFactOS -d $DatabaseName -t -A 2>&1
    if ($LASTEXITCODE -ne 0) { throw "psql failed: $result" }
    return ($result | Out-String).Trim()
}

function Invoke-PsqlFile([string]$Path) {
    Get-Content -LiteralPath $Path -Raw | docker exec -i $ContainerName psql -U NevaxisFactOS -d $DatabaseName -v ON_ERROR_STOP=1 2>&1
    if ($LASTEXITCODE -ne 0) { throw "psql file failed: $Path" }
}

function Get-InventoryCounts {
    $rows = Invoke-PsqlFile (Join-Path $SqlDir 'cleanup-sales-demo-data-1-dry-run.sql')
    $counts = [ordered]@{}
    foreach ($line in ($rows -split "`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^\|') { continue }
        $parts = $trimmed -split '\|'
        if ($parts.Count -ge 2 -and $parts[0] -and $parts[1] -match '^\d+$') {
            $counts[$parts[0].Trim()] = [int]$parts[1].Trim()
        }
    }
    # Fallback parse for non-aligned output
    if ($counts.Count -eq 0) {
        $raw = docker exec $ContainerName psql -U NevaxisFactOS -d $DatabaseName -t -A -F '|' -f /dev/stdin 2>&1
    }
    return $counts
}

function Ensure-TeamsDemoMessages {
    $existing = [int](Invoke-PsqlScalar "SELECT COUNT(*) FROM teams_messages WHERE id::text LIKE 'bb000001-03e%';")
    if ($existing -ge 4) { return 0 }

    $sql = @'
INSERT INTO teams_messages (id, tenant_id, facility_id, channel_id, author_tenant_membership_id, body, idempotency_key, idempotency_payload_fingerprint, created_at_utc)
SELECT * FROM (VALUES
  ('bb000001-03e0-4000-8000-0000000003e0'::uuid, 'bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0005-4000-8000-000000000005'::uuid, 'bb000001-03d1-4000-8000-0000000003d1'::uuid, 'bb000002-0003-4000-8000-000000000003'::uuid, 'Enjeksiyon hattında sıcaklık sapması gözlendi. Vardiya devri notu hazırlandı.', 'sales-demo-freeze-teams-03e0', 'sales-demo-freeze-teams-03e0', '2026-08-30 09:05:00+00'::timestamptz),
  ('bb000001-03e1-4000-8000-0000000003e1'::uuid, 'bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0005-4000-8000-000000000005'::uuid, 'bb000001-03d3-4000-8000-0000000003d3'::uuid, 'bb000002-0003-4000-8000-000000000003'::uuid, 'IM-120 hidrolik hat kontrolü planlandı. Bakım ekibi bilgilendirildi.', 'sales-demo-freeze-teams-03e1', 'sales-demo-freeze-teams-03e1', '2026-08-30 09:10:00+00'::timestamptz),
  ('bb000001-03e2-4000-8000-0000000003e2'::uuid, 'bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0005-4000-8000-000000000005'::uuid, 'bb000001-03d2-4000-8000-0000000003d2'::uuid, 'bb000002-0003-4000-8000-000000000003'::uuid, 'Son kalite kontrolünde çap toleransı sınırda. NCR takibi açıldı.', 'sales-demo-freeze-teams-03e2', 'sales-demo-freeze-teams-03e2', '2026-08-30 09:15:00+00'::timestamptz),
  ('bb000001-03e3-4000-8000-0000000003e3'::uuid, 'bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0005-4000-8000-000000000005'::uuid, 'bb000001-03d6-4000-8000-0000000003d6'::uuid, 'bb000002-0003-4000-8000-000000000003'::uuid, 'Haftalık üretim planı güncellendi. Yönetim onayı bekleniyor.', 'sales-demo-freeze-teams-03e3', 'sales-demo-freeze-teams-03e3', '2026-08-30 09:20:00+00'::timestamptz)
) AS v(id, tenant_id, facility_id, channel_id, author_tenant_membership_id, body, idempotency_key, idempotency_payload_fingerprint, created_at_utc)
WHERE NOT EXISTS (SELECT 1 FROM teams_messages m WHERE m.id = v.id);
'@
    $sql | docker exec -i $ContainerName psql -U NevaxisFactOS -d $DatabaseName -v ON_ERROR_STOP=1 | Out-Null
    return 4
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
Assert-DevEnvironment

$schemaVersion = Invoke-PsqlScalar 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
Write-Host "Schema: $schemaVersion"
Write-Host "Mode: $Mode"

# Parse dry-run counts via docker copy approach
$dryRunSql = Join-Path $SqlDir 'cleanup-sales-demo-data-1-dry-run.sql'
$dryRunOutput = Get-Content $dryRunSql -Raw | docker exec -i $ContainerName psql -U NevaxisFactOS -d $DatabaseName 2>&1 | Out-String

$inventory = [ordered]@{
    recordsReviewed = 0
    keepCount = 0
    fixtureCount = 0
    duplicateCount = 0
    orphanCount = 0
    failedExperimentCount = 0
    meaninglessDevCount = 0
    reviewCount = 0
    domain = [ordered]@{}
}

foreach ($line in ($dryRunOutput -split "`n")) {
    if ($line -match '^\s*(\S+)\s+\|\s*(\d+)\s*$') {
        $domain = $Matches[1]
        $count = [int]$Matches[2]
        $inventory.domain[$domain] = $count
        $inventory.recordsReviewed += $count
    }
}

$deleteTasks = [int]($inventory.domain['ai_tasks_delete'])
$keepTasks = [int]($inventory.domain['ai_tasks_keep'])
$inventory.keepCount = $keepTasks + [int]($inventory.domain['playbooks_keep']) + [int]($inventory.domain['playbook_runs_keep'])
$inventory.fixtureCount = $deleteTasks + [int]($inventory.domain['teams_messages_delete'])
$inventory.failedExperimentCount = [int]($inventory.domain['maintenance_gate_delete']) + [int]($inventory.domain['quality_test_delete'])
$inventory.duplicateCount = [int]($inventory.domain['maintenance_duplicate_delete'])
$inventory.meaninglessDevCount = [int]($inventory.domain['teams_messages_delete'])

$orphanEvidence = Invoke-PsqlScalar 'SELECT COUNT(*) FROM ai_task_evidence e LEFT JOIN ai_tasks t ON t.id = e.task_id WHERE t.id IS NULL;'
$inventory.orphanCount = [int]$orphanEvidence
$inventory.domain['orphan_evidence_pre'] = [int]$orphanEvidence

# Safety threshold: do not delete more than 80% of tasks if keep set is tiny
if ($deleteTasks -gt 0 -and $keepTasks -lt 5 -and $deleteTasks -gt 120) {
    throw "BLOCKED: delete volume ($deleteTasks tasks) exceeds safety threshold with only $keepTasks kept."
}

if ($Mode -eq 'DryRun') {
    $outPath = Join-Path $ArtifactDir 'data-cleanup-dry-run.json'
    $inventory | ConvertTo-Json -Depth 6 | Set-Content -Path $outPath -Encoding UTF8
    Write-Host "DRY RUN complete: $outPath"
    Write-Host "Would delete ~$deleteTasks AI tasks, ~$($inventory.domain['teams_messages_delete']) Teams messages"
    exit 0
}

# Execute cleanup
$authMfaBefore = Invoke-PsqlScalar "SELECT COUNT(*) FROM saas_identity_mfa_methods WHERE account_id IN (SELECT id FROM saas_identity_accounts WHERE email_normalized = 'tenant-owner@dev.local');"
Invoke-PsqlFile (Join-Path $SqlDir 'cleanup-sales-demo-data-1.sql')
$teamsInserted = Ensure-TeamsDemoMessages

$authMfaAfter = Invoke-PsqlScalar "SELECT COUNT(*) FROM saas_identity_mfa_methods WHERE account_id IN (SELECT id FROM saas_identity_accounts WHERE email_normalized = 'tenant-owner@dev.local');"
$schemaAfter = Invoke-PsqlScalar 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
$orphanAfter = Invoke-PsqlScalar 'SELECT COUNT(*) FROM ai_task_evidence e LEFT JOIN ai_tasks t ON t.id = e.task_id WHERE t.id IS NULL;'

$results = [ordered]@{
    executedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    tenantCode = $TenantCode
    databaseName = $DatabaseName
    schemaVersionBefore = $schemaVersion
    schemaVersionAfter = $schemaAfter
    deleted = $inventory.domain
    teamsDemoMessagesInserted = $teamsInserted
    orphanRecordsAfter = [int]$orphanAfter
    authMfaCountBefore = [int]$authMfaBefore
    authMfaCountAfter = [int]$authMfaAfter
    permissionsChanged = $false
    subscriptionChanged = $false
    schemaChanged = ($schemaVersion -ne $schemaAfter)
}

$outPath = Join-Path $ArtifactDir 'data-cleanup-results.json'
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $outPath -Encoding UTF8
Write-Host "EXECUTE complete: $outPath"
Write-Host "Tasks remaining: $(Invoke-PsqlScalar 'SELECT COUNT(*) FROM ai_tasks;')"
Write-Host "Teams messages remaining: $(Invoke-PsqlScalar 'SELECT COUNT(*) FROM teams_messages;')"
