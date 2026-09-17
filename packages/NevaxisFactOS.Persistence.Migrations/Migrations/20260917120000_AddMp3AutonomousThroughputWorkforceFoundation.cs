using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    public partial class AddMp3AutonomousThroughputWorkforceFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "model_gateway_audit_records", "mgar", new[]
            {
                "worker_id uuid",
                "digital_work_case_id uuid",
                "context_snapshot_id uuid",
                "request_classification character varying(64) NOT NULL",
                "intelligence_path character varying(32) NOT NULL",
                "provider_id character varying(64) NOT NULL",
                "model_name character varying(128) NOT NULL",
                "model_version character varying(32) NOT NULL",
                "routing_reason character varying(512) NOT NULL",
                "used_fallback boolean NOT NULL",
                "response_classification character varying(64) NOT NULL",
                "validation_status character varying(32) NOT NULL",
                "structured_proposal_json text NOT NULL",
                "validation_result_json text NOT NULL",
                "latency_ms bigint NOT NULL",
                "estimated_cost numeric NOT NULL",
                "input_tokens integer NOT NULL",
                "output_tokens integer NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, digital_work_case_id, created_at_utc");

            CreateTenantFacilityTable(migrationBuilder, "model_gateway_cost_usage_records", "mgcu", new[]
            {
                "worker_id uuid",
                "digital_work_case_id uuid",
                "provider_id character varying(64) NOT NULL",
                "model_name character varying(128) NOT NULL",
                "request_classification character varying(64) NOT NULL",
                "request_count integer NOT NULL",
                "input_tokens integer NOT NULL",
                "output_tokens integer NOT NULL",
                "estimated_cost numeric NOT NULL",
                "budget_window character varying(16) NOT NULL",
                "window_start_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, worker_id, window_start_utc");

            CreateTenantFacilityTable(migrationBuilder, "model_gateway_budget_policies", "mgbp", new[]
            {
                "worker_id uuid",
                "daily_budget numeric NOT NULL",
                "monthly_budget numeric NOT NULL",
                "case_budget numeric NOT NULL",
                "restricted_request_classes_json text NOT NULL DEFAULT '[]'",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, worker_id");

            CreateTenantFacilityTable(migrationBuilder, "throughput_optimizer_runs", "tor", new[]
            {
                "line_id uuid NOT NULL",
                "digital_work_case_id uuid NOT NULL",
                "context_snapshot_id uuid",
                "correlation_key character varying(240) NOT NULL",
                "current_phase character varying(32) NOT NULL",
                "observation_json text NOT NULL",
                "diagnosis_json text NOT NULL",
                "simulation_json text NOT NULL",
                "selection_json text NOT NULL",
                "action_plan_json text NOT NULL",
                "model_intelligence_path character varying(32) NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, correlation_key");

            CreateTenantFacilityTable(migrationBuilder, "verified_improvement_records", "vir", new[]
            {
                "digital_work_case_id uuid NOT NULL",
                "throughput_optimizer_run_id uuid NOT NULL",
                "verification_outcome character varying(64) NOT NULL",
                "baseline_json text NOT NULL",
                "target_improvement_per_hour numeric NOT NULL",
                "intervention_summary character varying(512) NOT NULL",
                "actual_json text NOT NULL",
                "quality_guard_passed boolean NOT NULL",
                "safety_guard_passed boolean NOT NULL",
                "observation_start_utc timestamp with time zone",
                "observation_end_utc timestamp with time zone",
                "verified_at_utc timestamp with time zone",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, digital_work_case_id");

            CreateTenantFacilityTable(migrationBuilder, "verified_training_dataset_candidates", "vtdc", new[]
            {
                "digital_work_case_id uuid NOT NULL",
                "verified_improvement_id uuid NOT NULL",
                "eligibility_status character varying(32) NOT NULL",
                "ineligibility_reason character varying(512) NOT NULL",
                "record_json text NOT NULL",
                "tenant_policy_json text NOT NULL",
                "verification_level character varying(32) NOT NULL",
                "data_quality_score character varying(16) NOT NULL",
                "anonymization_eligible boolean NOT NULL",
                "export_eligible boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, digital_work_case_id");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f93', 'model-gateway.read', 'model-gateway.read',
                     'Read model gateway audit and routing metadata.',
                     'Model gateway okuma', 'model-gateway', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f94', 'model-gateway.route', 'model-gateway.route',
                     'Route structured model requests through Nevaxis Model Gateway.',
                     'Model gateway yönlendirme', 'model-gateway', 'route',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f95', 'throughput-optimizer.read', 'throughput-optimizer.read',
                     'Read throughput optimizer worker workspace and verification state.',
                     'Verim optimizasyon okuma', 'throughput-optimizer', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f96', 'throughput-optimizer.manage', 'throughput-optimizer.manage',
                     'Manage throughput optimizer configuration and lifecycle.',
                     'Verim optimizasyon yönetimi', 'throughput-optimizer', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f97', 'throughput-optimizer.run', 'throughput-optimizer.run',
                     'Run throughput optimizer observe/diagnose/simulate/execute lifecycle.',
                     'Verim optimizasyon çalıştırma', 'throughput-optimizer', 'run',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "verified_training_dataset_candidates");
            migrationBuilder.DropTable(name: "verified_improvement_records");
            migrationBuilder.DropTable(name: "throughput_optimizer_runs");
            migrationBuilder.DropTable(name: "model_gateway_budget_policies");
            migrationBuilder.DropTable(name: "model_gateway_cost_usage_records");
            migrationBuilder.DropTable(name: "model_gateway_audit_records");
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

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select ON {tableName}
                            FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert ON {tableName}
                            FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update ON {tableName}
                            FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
