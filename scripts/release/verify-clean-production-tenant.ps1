# Proves a freshly enrolled Production tenant contains structural rows only (no pilot/demo/test/simulator business data).
# Run against a database right after first-account enrollment and before any customer import.
# Credentials are taken from the process environment (NEVAXIS_FACTOS__MIGRATIONS__PASSWORD) and are never printed.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PsqlPath,
    [string]$DbHost = "127.0.0.1",
    [Parameter(Mandatory = $true)][int]$Port,
    [string]$Database = "crevafactos_local",
    [string]$Username = "nevaxiszentra"
)

$ErrorActionPreference = "Stop"

# Tables that legitimately hold rows after first-account enrollment (tenant, first facility, owner identity,
# owner role + grants, packaged licence catalog/subscription, audit trail) plus the reference catalogs.
$structuralAllowList = @(
    '__EFMigrationsHistory',
    'permission_definitions',
    'tenants',
    'facilities',
    'facility_memberships',
    'identity_subjects',
    'tenant_memberships',
    'tenant_roles',
    'tenant_role_permissions',
    'tenant_membership_roles',
    'saas_identity_accounts',
    'saas_identity_password_credentials',
    'saas_identity_mfa_methods',
    'saas_identity_recovery_codes',
    'saas_identity_sessions',
    'saas_identity_security_audit_events',
    'saas_commercial_plans',
    'saas_commercial_plan_versions',
    'saas_commercial_audit_events',
    'saas_tenant_subscriptions',
    'saas_tenant_subscription_transition_history',
    'saas_subscription_audit_events'
)

if (-not $env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD) { throw "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD is not set." }
$env:PGPASSWORD = $env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD

$query = "select table_name, (xpath('/row/c/text()', query_to_xml(format('select count(*) as c from %I.%I', table_schema, table_name), false, true, '')))[1]::text::int as n from information_schema.tables where table_schema='public' and table_type='BASE TABLE' order by 1"
$rows = & $PsqlPath -h $DbHost -p $Port -U $Username -d $Database -At -F '=' -c $query
if ($LASTEXITCODE -ne 0) { throw "psql failed." }

$unexpected = @()
foreach ($row in $rows) {
    $name, $count = $row.Split('=', 2)
    if ([int]$count -gt 0 -and $structuralAllowList -notcontains $name) { $unexpected += "$name=$count" }
}

$tenantCount = [int](($rows | Where-Object { $_ -like 'tenants=*' }) -replace 'tenants=', '')
if ($tenantCount -ne 1) { $unexpected += "tenants=$tenantCount (expected exactly 1)" }

$demoProbe = & $PsqlPath -h $DbHost -p $Port -U $Username -d $Database -At -c "select count(*) from facilities where code ilike 'MIX-LINE-01%'"
if ([int]$demoProbe -ne 0) { $unexpected += "pilot facility code MIX-LINE-01 present" }

if ($unexpected.Count -gt 0) {
    Write-Error ("Clean Production tenant verification FAILED. Unexpected data: " + ($unexpected -join ', '))
    exit 1
}

Write-Host "Clean Production tenant verified: structural rows only (tenants=$tenantCount)."
