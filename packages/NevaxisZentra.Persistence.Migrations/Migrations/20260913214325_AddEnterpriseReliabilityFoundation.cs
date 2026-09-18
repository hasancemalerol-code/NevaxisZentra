using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseReliabilityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "background_job_leases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    owner_instance_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_leases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_verification_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    backup_artifact_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    restore_target_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_json = table.Column<string>(type: "text", nullable: false),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_verification_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "edge_agent_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    agent_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    connectivity_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    buffer_backlog_count = table.Column<int>(type: "integer", nullable: false),
                    buffer_capacity = table.Column<int>(type: "integer", nullable: false),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_sync_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    machine_scope_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edge_agent_registrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enterprise_reliability_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    policy_envelope_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enterprise_reliability_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_circuit_breaker_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    failure_threshold = table.Column<int>(type: "integer", nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cooldown_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_probe_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_circuit_breaker_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_quarantine_replay_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quarantine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replay_idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    replayed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_quarantine_replay_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recovery_reconciliation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    summary_json = table.Column<string>(type: "text", nullable: false),
                    conflict_json = table.Column<string>(type: "text", nullable: true),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_reconciliation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_component_health_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    component_kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    component_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    health_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    detail_json = table.Column<string>(type: "text", nullable: true),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_healthy_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_failure_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_component_health_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "technical_operational_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alert_kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    detail_json = table.Column<string>(type: "text", nullable: false),
                    dedup_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_operational_alerts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_leases_tenant_id_job_key",
                table: "background_job_leases",
                columns: new[] { "tenant_id", "job_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_edge_agent_registrations_tenant_id_agent_key",
                table: "edge_agent_registrations",
                columns: new[] { "tenant_id", "agent_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enterprise_reliability_policies_tenant_id",
                table: "enterprise_reliability_policies",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_circuit_breaker_states_connector_id",
                table: "integration_circuit_breaker_states",
                column: "connector_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_quarantine_replay_audit_tenant_id_replay_idempo~",
                table: "integration_quarantine_replay_audit",
                columns: new[] { "tenant_id", "replay_idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recovery_reconciliation_runs_tenant_id_started_at_utc",
                table: "recovery_reconciliation_runs",
                columns: new[] { "tenant_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_system_component_health_snapshots_tenant_id_component_kind_~",
                table: "system_component_health_snapshots",
                columns: new[] { "tenant_id", "component_kind", "component_key", "observed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_technical_operational_alerts_tenant_id_dedup_key_status",
                table: "technical_operational_alerts",
                columns: new[] { "tenant_id", "dedup_key", "status" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f70', 'operability.read', 'operability.read',
                     'View system status, integration health, and edge agent operability.',
                     'Sistem durumu görüntüle', 'operability', 'read',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f71', 'operability.manage', 'operability.manage',
                     'Manage enterprise reliability policy and operability settings.',
                     'Operasyonel dayanıklılık yönet', 'operability', 'manage',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f72', 'operability.alert.manage', 'operability.alert.manage',
                     'Acknowledge and resolve technical operational alerts.',
                     'Operasyonel uyarı yönet', 'operability', 'alert.manage',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f73', 'operability.reconcile.run', 'operability.reconcile.run',
                     'Run recovery reconciliation after outages.',
                     'Kurtarma mutabakatı çalıştır', 'operability', 'reconcile.run',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f74', 'operability.quarantine.replay', 'operability.quarantine.replay',
                     'Replay quarantined integration items safely.',
                     'Karantina kaydı tekrar oynat', 'operability', 'quarantine.replay',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f75', 'operability.backup.verify', 'operability.backup.verify',
                     'Verify database backup and restore readiness.',
                     'Yedekleme doğrulama', 'operability', 'backup.verify',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('admin-access.read', 'integration.read', 'system.diagnostics.read')
                JOIN permission_definitions read_perm ON read_perm.key = 'operability.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('admin-access.manage', 'integration.manage')
                JOIN permission_definitions manage_perm ON manage_perm.key = 'operability.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO edge_agent_registrations (
                    id, tenant_id, facility_id, agent_key, display_name, agent_version,
                    connectivity_status, buffer_backlog_count, buffer_capacity,
                    last_heartbeat_at_utc, last_sync_at_utc, machine_scope_json,
                    created_at_utc, updated_at_utc, version)
                VALUES (
                    'bb000001-0009-4000-8000-000000000901',
                    'bb000001-0001-4000-8000-000000000001',
                    'bb000001-0005-4000-8000-000000000005',
                    'pilot-edge-agent-001',
                    'Pilot Edge Agent',
                    '1.0.0',
                    'healthy',
                    0,
                    5000,
                    NOW(),
                    NOW(),
                    '["CNC-001","PRESS-001"]',
                    NOW(),
                    NOW(),
                    1)
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE enterprise_reliability_policies TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE system_component_health_snapshots TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_circuit_breaker_states TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE recovery_reconciliation_runs TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE background_job_leases TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE technical_operational_alerts TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE edge_agent_registrations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE backup_verification_runs TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_quarantine_replay_audit TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "enterprise_reliability_policies", "erp");
            ApplyTenantRls(migrationBuilder, "system_component_health_snapshots", "schs");
            ApplyTenantRls(migrationBuilder, "integration_circuit_breaker_states", "icbs");
            ApplyTenantRls(migrationBuilder, "recovery_reconciliation_runs", "rrr");
            ApplyTenantRls(migrationBuilder, "background_job_leases", "bjl");
            ApplyTenantRls(migrationBuilder, "technical_operational_alerts", "toa");
            ApplyTenantRls(migrationBuilder, "edge_agent_registrations", "ear");
            ApplyTenantRls(migrationBuilder, "backup_verification_runs", "bvr");
            ApplyTenantRls(migrationBuilder, "integration_quarantine_replay_audit", "iqra");
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
                DELETE FROM edge_agent_registrations WHERE id = 'bb000001-0009-4000-8000-000000000901';
                DELETE FROM permission_definitions WHERE id IN (
                    '10000000-0000-4000-8000-000000000f70',
                    '10000000-0000-4000-8000-000000000f71',
                    '10000000-0000-4000-8000-000000000f72',
                    '10000000-0000-4000-8000-000000000f73',
                    '10000000-0000-4000-8000-000000000f74',
                    '10000000-0000-4000-8000-000000000f75');
                """);
            migrationBuilder.DropTable(
                name: "background_job_leases");

            migrationBuilder.DropTable(
                name: "backup_verification_runs");

            migrationBuilder.DropTable(
                name: "edge_agent_registrations");

            migrationBuilder.DropTable(
                name: "enterprise_reliability_policies");

            migrationBuilder.DropTable(
                name: "integration_circuit_breaker_states");

            migrationBuilder.DropTable(
                name: "integration_quarantine_replay_audit");

            migrationBuilder.DropTable(
                name: "recovery_reconciliation_runs");

            migrationBuilder.DropTable(
                name: "system_component_health_snapshots");

            migrationBuilder.DropTable(
                name: "technical_operational_alerts");
        }
    }
}
