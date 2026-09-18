using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryNetworkOptimizationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facility_capability_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technologies_json = table.Column<string>(type: "text", nullable: false),
                    lines_json = table.Column<string>(type: "text", nullable: false),
                    machine_families_json = table.Column<string>(type: "text", nullable: false),
                    mold_compatibility_json = table.Column<string>(type: "text", nullable: false),
                    materials_json = table.Column<string>(type: "text", nullable: false),
                    quality_certifications_json = table.Column<string>(type: "text", nullable: false),
                    shifts_json = table.Column<string>(type: "text", nullable: false),
                    capacity_json = table.Column<string>(type: "text", nullable: false),
                    risk_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facility_capability_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_network_facilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_network_facilities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_network_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autonomy_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    policy_envelope_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_network_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_networks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    planning_horizon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    timezone_policy_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_networks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "network_activity_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_activity_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "network_allocation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    source_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    destination_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    source_plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_allocation_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "network_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factory_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selected_scenario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    reason_json = table.Column<string>(type: "text", nullable: false),
                    expected_impact_json = table.Column<string>(type: "text", nullable: false),
                    actual_impact_json = table.Column<string>(type: "text", nullable: false),
                    initiated_by_subject_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "network_capacity_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    facility_capacity_json = table.Column<string>(type: "text", nullable: false),
                    demand_json = table.Column<string>(type: "text", nullable: false),
                    freshness_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_capacity_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "network_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scenario_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    score = table.Column<decimal>(type: "numeric", nullable: false),
                    comparison_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_scenarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_facility_capability_profiles_tenant_facility",
                table: "facility_capability_profiles",
                columns: new[] { "tenant_id", "facility_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_network_facilities_network_facility",
                table: "factory_network_facilities",
                columns: new[] { "tenant_id", "network_id", "facility_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_network_policies_tenant_network",
                table: "factory_network_policies",
                columns: new[] { "tenant_id", "network_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_networks_tenant_name",
                table: "factory_networks",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_network_activity_events_network_occurred",
                table: "network_activity_events",
                columns: new[] { "tenant_id", "network_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_network_allocation_items_allocation_order",
                table: "network_allocation_items",
                columns: new[] { "tenant_id", "allocation_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_network_allocations_tenant_network_status",
                table: "network_allocations",
                columns: new[] { "tenant_id", "network_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_network_capacity_snapshots_network_captured",
                table: "network_capacity_snapshots",
                columns: new[] { "tenant_id", "network_id", "captured_at_utc" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f64', 'factory-network.read', 'factory-network.read',
                     'View factory network workspace and capacity map.',
                     'Fabrika ağı görüntüle', 'factory-network', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f65', 'factory-network.simulate', 'factory-network.simulate',
                     'Simulate cross-plant allocation scenarios.',
                     'Fabrika ağı simülasyonu', 'factory-network', 'simulate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f66', 'factory-network.manage', 'factory-network.manage',
                     'Manage factory network allocations and evaluations.',
                     'Fabrika ağı yönet', 'factory-network', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f67', 'factory-network.approve', 'factory-network.approve',
                     'Approve cross-plant allocation decisions.',
                     'Fabrika ağı onayla', 'factory-network', 'approve',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f68', 'factory-network.execute', 'factory-network.execute',
                     'Execute approved cross-plant allocations.',
                     'Fabrika ağı yürüt', 'factory-network', 'execute',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f69', 'factory-network.policy.manage', 'factory-network.policy.manage',
                     'Manage factory network autonomy and policy envelope.',
                     'Fabrika ağı politikası yönet', 'factory-network', 'policy-manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 16:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.read', 'production.planning.read')
                JOIN permission_definitions read_perm ON read_perm.key = 'factory-network.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, sim_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.simulate', 'production.planning.simulate')
                JOIN permission_definitions sim_perm ON sim_perm.key = 'factory-network.simulate'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = sim_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.decide', 'production.planning.publish', 'production.planning.schedule')
                JOIN permission_definitions manage_perm ON manage_perm.key = 'factory-network.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, approve_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.approve', 'production.planning.approve', 'production.planning.review')
                JOIN permission_definitions approve_perm ON approve_perm.key = 'factory-network.approve'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = approve_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, exec_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.decide', 'production.planning.publish')
                JOIN permission_definitions exec_perm ON exec_perm.key = 'factory-network.execute'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = exec_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, pol_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.autonomy.manage', 'production.planning.policy.manage')
                JOIN permission_definitions pol_perm ON pol_perm.key = 'factory-network.policy.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = pol_perm.id);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO facilities (
                    id, tenant_id, code, name, status, time_zone_id, created_at_utc, updated_at_utc, version)
                VALUES (
                    'bb000001-0005-4000-8000-000000000008',
                    'bb000001-0001-4000-8000-000000000001',
                    'plant-b-gebze',
                    'Gebze Üretim',
                    'Active',
                    'Europe/Istanbul',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    1)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO facility_memberships (
                    id, tenant_id, facility_id, tenant_membership_id, status, created_at_utc, updated_at_utc, version)
                VALUES
                    ('bb000001-0008-4000-8000-000000000801',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0005-4000-8000-000000000008',
                     'bb000001-0003-4000-8000-000000000003',
                     'Active',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00',
                     1),
                    ('bb000001-0008-4000-8000-000000000802',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0005-4000-8000-000000000008',
                     'bb000001-0021-4000-8000-000000000021',
                     'Active',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00',
                     1)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO factory_networks (
                    id, tenant_id, name, planning_horizon, timezone_policy_json, status,
                    created_at_utc, updated_at_utc, concurrency_token)
                VALUES (
                    'bb000001-0008-4000-8000-000000000810',
                    'bb000001-0001-4000-8000-000000000001',
                    'Nevaxis Pilot Fabrika Ağı',
                    'operational',
                    '{"primaryTimezone":"Europe/Istanbul","crossPlantCoordination":true}',
                    'active',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    1)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO factory_network_facilities (
                    id, tenant_id, network_id, facility_id, role, sort_order)
                VALUES
                    ('bb000001-0008-4000-8000-000000000811',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0008-4000-8000-000000000810',
                     'bb000001-0005-4000-8000-000000000005',
                     'primary',
                     1),
                    ('bb000001-0008-4000-8000-000000000812',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0008-4000-8000-000000000810',
                     'bb000001-0005-4000-8000-000000000008',
                     'member',
                     2)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO facility_capability_profiles (
                    id, tenant_id, facility_id, technologies_json, lines_json, machine_families_json,
                    mold_compatibility_json, materials_json, quality_certifications_json, shifts_json,
                    capacity_json, risk_json, updated_at_utc)
                VALUES
                    ('bb000001-0008-4000-8000-000000000813',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0005-4000-8000-000000000005',
                     '["injection-molding"]',
                     '["Line-1","Line-2"]',
                     '["IM-07","IM-09"]',
                     '["M-014","M-018","M-022"]',
                     '["PP-HF","ABS"]',
                     '["ISO9001"]',
                     '["shift-a","shift-b"]',
                     '{"plannedLines":2,"maxUtilization":92}',
                     '{"maintenanceRisk":"medium"}',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00'),
                    ('bb000001-0008-4000-8000-000000000814',
                     'bb000001-0001-4000-8000-000000000001',
                     'bb000001-0005-4000-8000-000000000008',
                     '["injection-molding"]',
                     '["Line-3"]',
                     '["IM-11"]',
                     '["M-014","M-018"]',
                     '["PP-HF"]',
                     '["ISO9001"]',
                     '["shift-a"]',
                     '{"plannedLines":1,"maxUtilization":78}',
                     '{"maintenanceRisk":"low"}',
                     TIMESTAMPTZ '2026-09-13 16:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO factory_network_policies (
                    id, tenant_id, network_id, autonomy_mode, policy_envelope_json, updated_at_utc, concurrency_token)
                VALUES (
                    'bb000001-0008-4000-8000-000000000816',
                    'bb000001-0001-4000-8000-000000000001',
                    'bb000001-0008-4000-8000-000000000810',
                    'limited',
                    '{"approvalThresholdOrders":2,"maxTransferQuantity":500,"prohibitedPairs":[],"requireLogisticsKnown":false}',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    1)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO network_capacity_snapshots (
                    id, tenant_id, network_id, captured_at_utc, facility_capacity_json, demand_json, freshness_json)
                VALUES (
                    'bb000001-0008-4000-8000-000000000815',
                    'bb000001-0001-4000-8000-000000000001',
                    'bb000001-0008-4000-8000-000000000810',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    '[{"facilityId":"bb000001-0005-4000-8000-000000000005","utilizationPercent":91},{"facilityId":"bb000001-0005-4000-8000-000000000008","utilizationPercent":58}]',
                    '[{"orderId":"PO-1842","assignedFacilityId":"bb000001-0005-4000-8000-000000000005","remainingQuantity":240,"deadlineUtc":"2026-09-14T18:00:00Z","riskLevel":"high","requiredCapability":"injection-molding","materialReady":true,"moldReady":true},{"orderId":"PO-1901","assignedFacilityId":"bb000001-0005-4000-8000-000000000005","remainingQuantity":120,"deadlineUtc":"2026-09-15T12:00:00Z","riskLevel":"medium","requiredCapability":"injection-molding","materialReady":true,"moldReady":false}]',
                    '{"production":"2026-09-13T16:00:00Z","material":"2026-09-13T15:30:00Z"}')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO network_allocations (
                    id, tenant_id, network_id, factory_decision_id, selected_scenario_id, action_type, status,
                    source_facility_id, destination_facility_id, trigger_type, reason_json, expected_impact_json,
                    actual_impact_json, initiated_by_subject_id, published_at_utc, verified_at_utc,
                    created_at_utc, updated_at_utc, concurrency_token)
                VALUES (
                    'bb000001-0008-4000-8000-000000000817',
                    'bb000001-0001-4000-8000-000000000001',
                    'bb000001-0008-4000-8000-000000000810',
                    NULL,
                    NULL,
                    'move-full-order',
                    'draft',
                    'bb000001-0005-4000-8000-000000000005',
                    'bb000001-0005-4000-8000-000000000008',
                    'capacity-imbalance',
                    '{"summary":"PO-1842 kapasite kurtarma önerisi"}',
                    '{"expectedBenefitScore":0.78}',
                    '{}',
                    'planning-pilot-user',
                    NULL,
                    NULL,
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    TIMESTAMPTZ '2026-09-13 16:00:00+00',
                    1)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO network_allocation_items (
                    id, tenant_id, allocation_id, order_id, source_facility_id, destination_facility_id,
                    total_quantity, source_quantity, destination_quantity, source_plan_version_id,
                    destination_plan_version_id, status)
                VALUES (
                    'bb000001-0008-4000-8000-000000000818',
                    'bb000001-0001-4000-8000-000000000001',
                    'bb000001-0008-4000-8000-000000000817',
                    'PO-1842',
                    'bb000001-0005-4000-8000-000000000005',
                    'bb000001-0005-4000-8000-000000000008',
                    240,
                    0,
                    240,
                    NULL,
                    NULL,
                    'planned')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_networks TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_network_facilities TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE facility_capability_profiles TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE network_capacity_snapshots TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE network_scenarios TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE network_allocations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE network_allocation_items TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_network_policies TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE network_activity_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "factory_networks", "fn");
            ApplyTenantRls(migrationBuilder, "factory_network_facilities", "fnf");
            ApplyTenantRls(migrationBuilder, "facility_capability_profiles", "fcp");
            ApplyTenantRls(migrationBuilder, "network_capacity_snapshots", "ncs");
            ApplyTenantRls(migrationBuilder, "network_scenarios", "ns");
            ApplyTenantRls(migrationBuilder, "network_allocations", "na");
            ApplyTenantRls(migrationBuilder, "network_allocation_items", "nai");
            ApplyTenantRls(migrationBuilder, "factory_network_policies", "fnp");
            ApplyTenantRls(migrationBuilder, "network_activity_events", "nae");
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM network_allocation_items WHERE id = 'bb000001-0008-4000-8000-000000000818';
                DELETE FROM network_allocations WHERE id = 'bb000001-0008-4000-8000-000000000817';
                DELETE FROM network_capacity_snapshots WHERE id = 'bb000001-0008-4000-8000-000000000815';
                DELETE FROM factory_network_policies WHERE id = 'bb000001-0008-4000-8000-000000000816';
                DELETE FROM facility_capability_profiles WHERE id IN (
                    'bb000001-0008-4000-8000-000000000813',
                    'bb000001-0008-4000-8000-000000000814');
                DELETE FROM factory_network_facilities WHERE id IN (
                    'bb000001-0008-4000-8000-000000000811',
                    'bb000001-0008-4000-8000-000000000812');
                DELETE FROM factory_networks WHERE id = 'bb000001-0008-4000-8000-000000000810';
                DELETE FROM facility_memberships WHERE id IN (
                    'bb000001-0008-4000-8000-000000000801',
                    'bb000001-0008-4000-8000-000000000802');
                DELETE FROM facilities WHERE id = 'bb000001-0005-4000-8000-000000000008';
                DELETE FROM permission_definitions WHERE id IN (
                    '10000000-0000-4000-8000-000000000f64',
                    '10000000-0000-4000-8000-000000000f65',
                    '10000000-0000-4000-8000-000000000f66',
                    '10000000-0000-4000-8000-000000000f67',
                    '10000000-0000-4000-8000-000000000f68',
                    '10000000-0000-4000-8000-000000000f69');
                """);

            migrationBuilder.DropTable(
                name: "facility_capability_profiles");

            migrationBuilder.DropTable(
                name: "factory_network_facilities");

            migrationBuilder.DropTable(
                name: "factory_network_policies");

            migrationBuilder.DropTable(
                name: "factory_networks");

            migrationBuilder.DropTable(
                name: "network_activity_events");

            migrationBuilder.DropTable(
                name: "network_allocation_items");

            migrationBuilder.DropTable(
                name: "network_allocations");

            migrationBuilder.DropTable(
                name: "network_capacity_snapshots");

            migrationBuilder.DropTable(
                name: "network_scenarios");
        }
    }
}
