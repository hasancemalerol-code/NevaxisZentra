# NevaxisZentra omnichannel closure — isolated database constants (no credentials in reports)
$script:ClosureDatabaseName = "NEVAXIS_FACTOS_omnichannel_closure"
$script:ClosurePostgresContainer = "NevaxisZentra-local-postgres-1"
$script:ClosurePostgresHost = "127.0.0.1"
$script:ClosurePostgresPort = 5433
$script:ClosureMigrationUser = "crevafactos"
$script:ClosureRuntimeUser = "crevafactos_runtime"

# Deterministic closure fixture IDs (tenant A = dev-pilot shape on isolated DB)
$script:ClosureTenantAId = "cc000001-0001-4000-8000-000000000001"
$script:ClosureTenantBId = "cc000002-0001-4000-8000-000000000001"
$script:ClosureFacilityAId = "cc000001-0005-4000-8000-000000000005"
$script:ClosureFacilityBId = "cc000002-0005-4000-8000-000000000005"
$script:ClosureTaskAId = "cc000001-0010-4000-8000-000000000010"
$script:ClosureTaskBId = "cc000002-0010-4000-8000-000000000010"
$script:ClosureSubjectAId = "cc000001-0002-4000-8000-000000000002"
$script:ClosureSubjectBId = "cc000002-0002-4000-8000-000000000002"
$script:ClosureMembershipAId = "cc000001-0003-4000-8000-000000000003"
$script:ClosureMembershipBId = "cc000002-0003-4000-8000-000000000003"
$script:ClosureRoleAId = "cc000001-0004-4000-8000-000000000004"
$script:ClosureRoleBId = "cc000002-0004-4000-8000-000000000004"
$script:ClosureFacilityMembershipAId = "cc000001-0006-4000-8000-000000000006"
$script:ClosureFacilityMembershipBId = "cc000002-0006-4000-8000-000000000006"
