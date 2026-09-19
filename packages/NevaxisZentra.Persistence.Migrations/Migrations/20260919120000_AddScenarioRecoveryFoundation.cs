using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddScenarioRecoveryFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "scenario_recovery_runs", "srr", new[]
            {
                "line_id uuid NOT NULL",
                "digital_work_case_id uuid",
                "context_snapshot_id uuid",
                "correlation_key character varying(240) NOT NULL",
                "category character varying(64) NOT NULL",
                "entry_point character varying(32) NOT NULL",
                "current_phase character varying(32) NOT NULL",
                "execution_state character varying(32) NOT NULL",
                "problem_json text NOT NULL",
                "objectives_json text NOT NULL",
                "snapshot_summary_json text NOT NULL DEFAULT '{}'",
                "comparison_json text NOT NULL DEFAULT '{}'",
                "selected_option_label character varying(32) NOT NULL DEFAULT ''",
                "recovery_plan_json text NOT NULL DEFAULT '{}'",
                "verification_json text NOT NULL DEFAULT '{}'",
                "source_entity_type character varying(64) NOT NULL DEFAULT ''",
                "source_entity_id character varying(128) NOT NULL DEFAULT ''",
                "engine_version character varying(64) NOT NULL",
                "schema_version character varying(16) NOT NULL",
                "concurrency_token bigint NOT NULL DEFAULT 1",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, correlation_key");

            CreateTenantFacilityTable(migrationBuilder, "scenario_recovery_options", "sro", new[]
            {
                "run_id uuid NOT NULL",
                "option_label character varying(32) NOT NULL",
                "title character varying(256) NOT NULL",
                "category character varying(64) NOT NULL",
                "feasibility_state character varying(32) NOT NULL",
                "actions_json text NOT NULL",
                "simulation_json text NOT NULL",
                "metrics_json text NOT NULL",
                "assumptions_json text NOT NULL",
                "requires_approval boolean NOT NULL",
                "is_dominated boolean NOT NULL",
                "recommendation_score numeric NOT NULL",
                "feasibility_reasons_json text NOT NULL DEFAULT '[]'",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, run_id, option_label");

            CreateTenantFacilityTable(migrationBuilder, "scenario_recovery_prediction_accuracy", "srpa", new[]
            {
                "run_id uuid NOT NULL",
                "digital_work_case_id uuid",
                "metric_code character varying(64) NOT NULL",
                "predicted_value numeric NOT NULL",
                "actual_value numeric",
                "prediction_error numeric",
                "verification_outcome character varying(64) NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, run_id, metric_code");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f98', 'scenario-recovery.read', 'scenario-recovery.read',
                     'Read scenario recovery workspace, comparison, and history.',
                     'Senaryo kurtarma okuma', 'scenario-recovery', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f99', 'scenario-recovery.manage', 'scenario-recovery.manage',
                     'Manage scenario recovery configuration and lifecycle metadata.',
                     'Senaryo kurtarma yönetimi', 'scenario-recovery', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fa0', 'scenario-recovery.simulate', 'scenario-recovery.simulate',
                     'Run scenario simulation and option comparison.',
                     'Senaryo simülasyonu', 'scenario-recovery', 'simulate',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fa1', 'scenario-recovery.execute', 'scenario-recovery.execute',
                     'Apply selected scenario recovery option through controlled execution.',
                     'Senaryo kurtarma uygulama', 'scenario-recovery', 'execute',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "scenario_recovery_prediction_accuracy");
            migrationBuilder.DropTable(name: "scenario_recovery_options");
            migrationBuilder.DropTable(name: "scenario_recovery_runs");
        }

        private static void CreateTenantFacilityTable(
            MigrationBuilder migrationBuilder,
            string tableName,
            string prefix,
            string[] extraColumns,
            string uniqueIndexColumns)
        {
            var columnsSql = string.Join(",\n                    ", extraColumns);
            migrationBuilder.Sql(
                $"""
                CREATE TABLE {tableName} (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    {columnsSql},
                    CONSTRAINT "PK_{tableName}" PRIMARY KEY (id),
                    CONSTRAINT "FK_{tableName}_facilities" FOREIGN KEY (tenant_id, facility_id)
                        REFERENCES facilities (tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_{tableName}_tenants" FOREIGN KEY (tenant_id)
                        REFERENCES tenants (id) ON DELETE RESTRICT
                );
                CREATE UNIQUE INDEX "IX_{tableName}_{uniqueIndexColumns.Replace(", ", "_")}"
                    ON {tableName} ({uniqueIndexColumns});
                """);

            ApplyTenantRls(migrationBuilder, tableName, prefix);

            migrationBuilder.Sql(
                $"""
                REVOKE ALL ON TABLE {tableName} FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE {tableName} TO crevafactos_runtime;
                """);
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;
                CREATE POLICY {prefix}_tenant_isolation ON {tableName}
                    USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
                """);
        }
    }
}
