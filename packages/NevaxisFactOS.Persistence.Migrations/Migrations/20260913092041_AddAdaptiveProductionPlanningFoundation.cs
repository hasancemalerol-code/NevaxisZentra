using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveProductionPlanningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "active_plan_version_id",
                table: "production_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "adaptive_lifecycle_status",
                table: "production_plans",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "adaptive_planning_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frozen_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    nervousness_cooldown_minutes = table.Column<int>(type: "integer", nullable: false),
                    min_benefit_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    autonomy_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    policy_envelope_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_planning_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan_activity_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_activity_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    initiated_by_subject_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    diff_json = table.Column<string>(type: "text", nullable: false),
                    reason_json = table.Column<string>(type: "text", nullable: false),
                    expected_benefit_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan_deviations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deviation_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_deviations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planning_constraint_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planning_horizon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    orders_json = table.Column<string>(type: "text", nullable: false),
                    machines_json = table.Column<string>(type: "text", nullable: false),
                    materials_json = table.Column<string>(type: "text", nullable: false),
                    molds_json = table.Column<string>(type: "text", nullable: false),
                    maintenance_json = table.Column<string>(type: "text", nullable: false),
                    quality_json = table.Column<string>(type: "text", nullable: false),
                    shifts_json = table.Column<string>(type: "text", nullable: false),
                    capacity_json = table.Column<string>(type: "text", nullable: false),
                    incidents_json = table.Column<string>(type: "text", nullable: false),
                    freshness_json = table.Column<string>(type: "text", nullable: false),
                    hard_constraints_json = table.Column<string>(type: "text", nullable: false),
                    soft_constraints_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planning_constraint_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planning_scenario_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    metric_value = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planning_scenario_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planning_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    constraint_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    score = table.Column<decimal>(type: "numeric", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    comparison_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planning_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_plan_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    operation_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    machine_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    line_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    mold_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    shift_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    setup_minutes = table.Column<decimal>(type: "numeric", nullable: false),
                    material_readiness_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sequence_rank = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_plan_operations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    planning_horizon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    objective_profile_json = table.Column<string>(type: "text", nullable: false),
                    constraint_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    score = table.Column<decimal>(type: "numeric", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    superseded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    change_summary_json = table.Column<string>(type: "text", nullable: false),
                    expected_impact_json = table.Column<string>(type: "text", nullable: false),
                    actual_impact_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_plan_versions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_planning_policies_tenant_id_facility_id",
                table: "adaptive_planning_policies",
                columns: new[] { "tenant_id", "facility_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_activity_events_tenant_id_production_plan_id_occurred_~",
                table: "plan_activity_events",
                columns: new[] { "tenant_id", "production_plan_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_production_plan_operations_version_sequence",
                table: "production_plan_operations",
                columns: new[] { "tenant_id", "plan_version_id", "sequence_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_production_plan_versions_plan_version",
                table: "production_plan_versions",
                columns: new[] { "tenant_id", "facility_id", "production_plan_id", "version_number" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f60', 'production.planning.simulate', 'production.planning.simulate',
                     'Simulate adaptive production plan alternatives.',
                     'Plan simülasyonu', 'production-planning', 'simulate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 09:20:00+00'),
                    ('10000000-0000-4000-8000-000000000f61', 'production.planning.publish', 'production.planning.publish',
                     'Publish adaptive production plan versions.',
                     'Plan yayınla', 'production-planning', 'publish',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 09:20:00+00'),
                    ('10000000-0000-4000-8000-000000000f62', 'production.planning.autonomy.manage', 'production.planning.autonomy.manage',
                     'Manage adaptive planning autonomy envelope.',
                     'Planlama otonomisi yönet', 'production-planning', 'autonomy-manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 09:20:00+00'),
                    ('10000000-0000-4000-8000-000000000f63', 'production.planning.policy.manage', 'production.planning.policy.manage',
                     'Manage adaptive planning policy and frozen window.',
                     'Planlama politikası yönet', 'production-planning', 'policy-manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 09:20:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, sim_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production.planning.review'
                JOIN permission_definitions sim_perm ON sim_perm.key = 'production.planning.simulate'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = sim_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, pub_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production.planning.schedule'
                JOIN permission_definitions pub_perm ON pub_perm.key = 'production.planning.publish'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = pub_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, auto_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.autonomy.manage'
                JOIN permission_definitions auto_perm ON auto_perm.key = 'production.planning.autonomy.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = auto_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, pol_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production.planning.approve'
                JOIN permission_definitions pol_perm ON pol_perm.key = 'production.planning.policy.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = pol_perm.id);
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_versions TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_operations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE planning_constraint_snapshots TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE planning_scenarios TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE planning_scenario_metrics TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE plan_deviations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE plan_changes TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE adaptive_planning_policies TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE plan_activity_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "production_plan_versions", "ppv");
            ApplyTenantRls(migrationBuilder, "production_plan_operations", "ppo");
            ApplyTenantRls(migrationBuilder, "planning_constraint_snapshots", "pcs");
            ApplyTenantRls(migrationBuilder, "planning_scenarios", "ps");
            ApplyTenantRls(migrationBuilder, "planning_scenario_metrics", "psm");
            ApplyTenantRls(migrationBuilder, "plan_deviations", "pd");
            ApplyTenantRls(migrationBuilder, "plan_changes", "pc");
            ApplyTenantRls(migrationBuilder, "adaptive_planning_policies", "app");
            ApplyTenantRls(migrationBuilder, "plan_activity_events", "pae");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f60',
                    '10000000-0000-4000-8000-000000000f61',
                    '10000000-0000-4000-8000-000000000f62',
                    '10000000-0000-4000-8000-000000000f63');
                """);

            migrationBuilder.DropTable(
                name: "adaptive_planning_policies");

            migrationBuilder.DropTable(
                name: "plan_activity_events");

            migrationBuilder.DropTable(
                name: "plan_changes");

            migrationBuilder.DropTable(
                name: "plan_deviations");

            migrationBuilder.DropTable(
                name: "planning_constraint_snapshots");

            migrationBuilder.DropTable(
                name: "planning_scenario_metrics");

            migrationBuilder.DropTable(
                name: "planning_scenarios");

            migrationBuilder.DropTable(
                name: "production_plan_operations");

            migrationBuilder.DropTable(
                name: "production_plan_versions");

            migrationBuilder.DropColumn(
                name: "active_plan_version_id",
                table: "production_plans");

            migrationBuilder.DropColumn(
                name: "adaptive_lifecycle_status",
                table: "production_plans");
        }
    }
}
