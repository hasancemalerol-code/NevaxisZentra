using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryOrchestrationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "administratively_closed",
                table: "digital_work_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "blocker_since_utc",
                table: "digital_work_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_key",
                table: "digital_work_cases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_blocker_code",
                table: "digital_work_cases",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "dependency_graph_json",
                table: "digital_work_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "impact_graph_json",
                table: "digital_work_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "orchestration_root_case_id",
                table: "digital_work_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "outcome_verification_json",
                table: "digital_work_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_case_id",
                table: "digital_work_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "participating_domains_json",
                table: "digital_work_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "primary_domain",
                table: "digital_work_cases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "recovery_plan_json",
                table: "digital_work_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "reopen_count",
                table: "digital_work_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "factory_case_dependencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_node_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    to_node_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    from_node_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    to_node_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    edge_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    linked_case_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    linked_worker_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    linked_task_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    metadata_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_case_dependencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_event_correlations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    root_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    event_category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    first_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_event_correlations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_cases_tenant_correlation",
                table: "digital_work_cases",
                columns: new[] { "tenant_id", "correlation_key" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_case_dependencies_tenant_root",
                table: "factory_case_dependencies",
                columns: new[] { "tenant_id", "root_case_id" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_event_correlations_tenant_source",
                table: "factory_event_correlations",
                columns: new[] { "tenant_id", "facility_id", "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ux_factory_event_correlations_tenant_key",
                table: "factory_event_correlations",
                columns: new[] { "tenant_id", "correlation_key" },
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE digital_work_cases SET current_blocker_code = 'none' WHERE current_blocker_code = '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f30', 'factory-case.read', 'factory-case.read',
                     'View factory orchestration cases and dependency graphs.',
                     'Fabrika operasyon vakalarını görüntüle', 'factory-case', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 20:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f31', 'factory-case.manage', 'factory-case.manage',
                     'Route factory events and manage orchestration cases.',
                     'Fabrika vakalarını yönet', 'factory-case', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 20:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f32', 'factory-case.escalate', 'factory-case.escalate',
                     'Escalate factory orchestration cases.',
                     'Fabrika vakasını eskale et', 'factory-case', 'escalate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 20:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f33', 'factory-case.verify', 'factory-case.verify',
                     'Verify factory outcome and close orchestration cases.',
                     'Fabrika sonucunu doğrula', 'factory-case', 'verify',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 20:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f34', 'factory-case.reopen', 'factory-case.reopen',
                     'Reopen verified factory orchestration cases.',
                     'Fabrika vakasını yeniden aç', 'factory-case', 'reopen',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 20:55:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_case_dependencies TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_event_correlations TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "factory_case_dependencies", "fcd");
            ApplyTenantRls(migrationBuilder, "factory_event_correlations", "fec");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f30',
                    '10000000-0000-4000-8000-000000000f31',
                    '10000000-0000-4000-8000-000000000f32',
                    '10000000-0000-4000-8000-000000000f33',
                    '10000000-0000-4000-8000-000000000f34');
                """);

            migrationBuilder.DropTable(
                name: "factory_case_dependencies");

            migrationBuilder.DropTable(
                name: "factory_event_correlations");

            migrationBuilder.DropIndex(
                name: "ix_digital_work_cases_tenant_correlation",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "administratively_closed",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "blocker_since_utc",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "correlation_key",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "current_blocker_code",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "dependency_graph_json",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "impact_graph_json",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "orchestration_root_case_id",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "outcome_verification_json",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "parent_case_id",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "participating_domains_json",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "primary_domain",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "recovery_plan_json",
                table: "digital_work_cases");

            migrationBuilder.DropColumn(
                name: "reopen_count",
                table: "digital_work_cases");
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
    }
}
