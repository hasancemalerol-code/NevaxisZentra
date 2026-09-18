using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenDemoP1Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "golden_demo_agent_findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    worker_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    detail_json = table.Column<string>(type: "text", nullable: false),
                    evidence_classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_agent_findings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_cycle_baselines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reference_source_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_cycle_seconds = table.Column<decimal>(type: "numeric", nullable: false),
                    phase_durations_json = table.Column<string>(type: "text", nullable: false),
                    process_parameters_json = table.Column<string>(type: "text", nullable: false),
                    reference_kwh_per_good_part = table.Column<decimal>(type: "numeric", nullable: true),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_cycle_baselines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_cycle_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    total_cycle_seconds = table.Column<decimal>(type: "numeric", nullable: false),
                    phase_durations_json = table.Column<string>(type: "text", nullable: false),
                    process_parameters_json = table.Column<string>(type: "text", nullable: false),
                    micro_stop_count = table.Column<int>(type: "integer", nullable: false),
                    micro_stop_duration_seconds = table.Column<decimal>(type: "numeric", nullable: false),
                    good_parts = table.Column<int>(type: "integer", nullable: false),
                    reject_parts = table.Column<int>(type: "integer", nullable: false),
                    availability_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    kwh_per_good_part = table.Column<decimal>(type: "numeric", nullable: true),
                    total_energy_kwh = table.Column<decimal>(type: "numeric", nullable: true),
                    machine_state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_cycle_observations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_digital_work_case_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    digital_work_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    linked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_digital_work_case_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_production_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    mold_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shift_target_good_parts = table.Column<int>(type: "integer", nullable: false),
                    shift_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    shift_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_production_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_recovery_plan_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    action_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    responsible_role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_required = table.Column<string>(type: "text", nullable: false),
                    estimated_downtime_minutes = table.Column<int>(type: "integer", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    validation_criteria = table.Column<string>(type: "text", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_recovery_plan_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_recovery_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recommendation_summary = table.Column<string>(type: "text", nullable: false),
                    recommended_scenario_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_recovery_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_verification_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criteria_json = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    recovered_good_parts_per_shift = table.Column<decimal>(type: "numeric", nullable: false),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_verification_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_agent_findings_production_event_id_agent_code",
                table: "golden_demo_agent_findings",
                columns: new[] { "production_event_id", "agent_code" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_cycle_baselines_production_event_id",
                table: "golden_demo_cycle_baselines",
                column: "production_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_cycle_observations_production_event_id_observat~",
                table: "golden_demo_cycle_observations",
                columns: new[] { "production_event_id", "observation_kind" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_digital_work_case_links_production_event_id_wor~",
                table: "golden_demo_digital_work_case_links",
                columns: new[] { "production_event_id", "worker_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_production_events_tenant_id_facility_id_machine~",
                table: "golden_demo_production_events",
                columns: new[] { "tenant_id", "facility_id", "machine_asset_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_recovery_plan_actions_recovery_plan_id_sort_ord~",
                table: "golden_demo_recovery_plan_actions",
                columns: new[] { "recovery_plan_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_recovery_plans_production_event_id",
                table: "golden_demo_recovery_plans",
                column: "production_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_verification_runs_production_event_id_verified_~",
                table: "golden_demo_verification_runs",
                columns: new[] { "production_event_id", "verified_at_utc" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f76', 'golden-demo-p1.read', 'golden-demo-p1.read',
                     'Read golden demo production recovery workspace.',
                     'Altın demo üretim kurtarma oku', 'golden-demo-p1', 'read',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f77', 'golden-demo-p1.manage', 'golden-demo-p1.manage',
                     'Refresh golden demo analysis and manage recovery workspace.',
                     'Altın demo üretim kurtarma yönet', 'golden-demo-p1', 'manage',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f78', 'golden-demo-p1.approve', 'golden-demo-p1.approve',
                     'Approve golden demo recovery plans.',
                     'Altın demo kurtarma planı onayla', 'golden-demo-p1', 'approve',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f79', 'golden-demo-p1.execute', 'golden-demo-p1.execute',
                     'Execute golden demo recovery interventions.',
                     'Altın demo kurtarma uygula', 'golden-demo-p1', 'execute',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f7a', 'golden-demo-p1.verify', 'golden-demo-p1.verify',
                     'Verify golden demo production recovery outcomes.',
                     'Altın demo kurtarma doğrula', 'golden-demo-p1', 'verify',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.read', 'production.execution.read', 'process-oee.read')
                JOIN permission_definitions read_perm ON read_perm.key = 'golden-demo-p1.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.manage', 'production-manager.decide')
                JOIN permission_definitions manage_perm ON manage_perm.key = 'golden-demo-p1.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, approve_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.approve'
                JOIN permission_definitions approve_perm ON approve_perm.key = 'golden-demo-p1.approve'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = approve_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, execute_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('maintenance.execute', 'production-manager.approve')
                JOIN permission_definitions execute_perm ON execute_perm.key = 'golden-demo-p1.execute'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = execute_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, verify_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('production-manager.approve', 'quality.release')
                JOIN permission_definitions verify_perm ON verify_perm.key = 'golden-demo-p1.verify'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = verify_perm.id);
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_production_events TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_cycle_baselines TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_cycle_observations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_recovery_plans TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_recovery_plan_actions TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_agent_findings TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_verification_runs TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE golden_demo_digital_work_case_links TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "golden_demo_production_events", "gdpe");
            ApplyTenantRls(migrationBuilder, "golden_demo_cycle_baselines", "gdcb");
            ApplyTenantRls(migrationBuilder, "golden_demo_cycle_observations", "gdco");
            ApplyTenantRls(migrationBuilder, "golden_demo_recovery_plans", "gdrp");
            ApplyTenantRls(migrationBuilder, "golden_demo_recovery_plan_actions", "gdrpa");
            ApplyTenantRls(migrationBuilder, "golden_demo_agent_findings", "gdaf");
            ApplyTenantRls(migrationBuilder, "golden_demo_verification_runs", "gdvr");
            ApplyTenantRls(migrationBuilder, "golden_demo_digital_work_case_links", "gdwcl");
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
                DELETE FROM permission_definitions WHERE id IN (
                    '10000000-0000-4000-8000-000000000f76',
                    '10000000-0000-4000-8000-000000000f77',
                    '10000000-0000-4000-8000-000000000f78',
                    '10000000-0000-4000-8000-000000000f79',
                    '10000000-0000-4000-8000-000000000f7a');
                """);
            migrationBuilder.DropTable(
                name: "golden_demo_agent_findings");

            migrationBuilder.DropTable(
                name: "golden_demo_cycle_baselines");

            migrationBuilder.DropTable(
                name: "golden_demo_cycle_observations");

            migrationBuilder.DropTable(
                name: "golden_demo_digital_work_case_links");

            migrationBuilder.DropTable(
                name: "golden_demo_production_events");

            migrationBuilder.DropTable(
                name: "golden_demo_recovery_plan_actions");

            migrationBuilder.DropTable(
                name: "golden_demo_recovery_plans");

            migrationBuilder.DropTable(
                name: "golden_demo_verification_runs");
        }
    }
}
