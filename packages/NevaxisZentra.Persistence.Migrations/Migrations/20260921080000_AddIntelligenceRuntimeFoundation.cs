using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddIntelligenceRuntimeFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "intelligence_runtime_routes", "irr", new[]
            {
                "worker_id uuid",
                "digital_work_case_id uuid",
                "optimization_run_id uuid",
                "scenario_recovery_run_id uuid",
                "context_snapshot_id uuid",
                "model_gateway_audit_id uuid",
                "task_type character varying(64) NOT NULL",
                "request_classification character varying(64) NOT NULL",
                "route_type character varying(32) NOT NULL",
                "intelligence_path character varying(32) NOT NULL",
                "provider_id character varying(64) NOT NULL",
                "model_name character varying(128) NOT NULL",
                "routing_reason character varying(512) NOT NULL",
                "why_model_called_or_skipped character varying(1024) NOT NULL",
                "model_invoked boolean NOT NULL",
                "used_fallback boolean NOT NULL",
                "input_tokens integer NOT NULL",
                "output_tokens integer NOT NULL",
                "estimated_cost numeric NOT NULL",
                "price_version character varying(32) NOT NULL DEFAULT ''",
                "latency_ms bigint NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, id");

            migrationBuilder.Sql(
                """
                CREATE TABLE provider_price_catalog (
                    id uuid NOT NULL,
                    provider_id character varying(64) NOT NULL,
                    model_name character varying(128) NOT NULL,
                    input_price_per_1k numeric NOT NULL,
                    output_price_per_1k numeric NOT NULL,
                    cached_input_price_per_1k numeric,
                    currency character varying(8) NOT NULL,
                    price_version character varying(32) NOT NULL,
                    source character varying(128) NOT NULL,
                    is_active boolean NOT NULL,
                    effective_from_utc timestamp with time zone NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_provider_price_catalog" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX "IX_provider_price_catalog_provider_id_model_name_price_version"
                    ON provider_price_catalog (provider_id, model_name, price_version);
                INSERT INTO provider_price_catalog (
                    id, provider_id, model_name, input_price_per_1k, output_price_per_1k, currency,
                    price_version, source, is_active, effective_from_utc, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000fd1', 'openai', 'gpt-4.1-mini', 0.0004, 0.0016, 'USD',
                     '2026-09-21', 'seed-default', TRUE, TIMESTAMPTZ '2026-09-21 00:00:00+00', TIMESTAMPTZ '2026-09-21 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fd2', 'frontier-model', 'frontier-reasoning-v1', 0.003, 0.012, 'USD',
                     '2026-09-21', 'seed-default', TRUE, TIMESTAMPTZ '2026-09-21 00:00:00+00', TIMESTAMPTZ '2026-09-21 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                REVOKE ALL ON TABLE provider_price_catalog FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE provider_price_catalog TO crevafactos_runtime;
                """);

            CreateTenantFacilityTable(migrationBuilder, "tenant_intelligence_learning_policies", "tilp", new[]
            {
                "learning_policy character varying(64) NOT NULL",
                "allow_export boolean NOT NULL",
                "allow_training boolean NOT NULL",
                "allow_shadow boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id");

            CreateTenantFacilityTable(migrationBuilder, "nevaxis_verified_manufacturing_datasets", "nvmd", new[]
            {
                "version_code character varying(64) NOT NULL",
                "schema_version character varying(16) NOT NULL",
                "readiness_state character varying(32) NOT NULL",
                "tenant_policy character varying(64) NOT NULL",
                "sanitization_version character varying(16) NOT NULL",
                "policy_version character varying(16) NOT NULL",
                "included_case_count integer NOT NULL",
                "excluded_case_count integer NOT NULL",
                "manifest_json text NOT NULL DEFAULT '{}'",
                "quality_metrics_json text NOT NULL DEFAULT '{}'",
                "task_distribution_json text NOT NULL DEFAULT '{}'",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, version_code");

            CreateTenantFacilityTable(migrationBuilder, "nevaxis_verified_manufacturing_dataset_entries", "nvmde", new[]
            {
                "dataset_version_id uuid NOT NULL",
                "source_case_id uuid NOT NULL",
                "verified_improvement_id uuid",
                "is_negative_learning boolean NOT NULL",
                "record_json text NOT NULL DEFAULT '{}'",
                "provenance_json text NOT NULL DEFAULT '{}'",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, dataset_version_id, source_case_id");

            CreateTenantFacilityTable(migrationBuilder, "nevaxis_model_registry", "nmr", new[]
            {
                "model_code character varying(128) NOT NULL",
                "version character varying(32) NOT NULL",
                "base_model character varying(128) NOT NULL",
                "status character varying(32) NOT NULL",
                "evaluation_status character varying(32) NOT NULL DEFAULT ''",
                "safety_status character varying(32) NOT NULL DEFAULT ''",
                "deployment_type character varying(32) NOT NULL",
                "context_limit integer NOT NULL",
                "task_capabilities_json text NOT NULL DEFAULT '[]'",
                "training_dataset_version_id uuid",
                "evaluation_dataset_version_id uuid",
                "metadata_json text NOT NULL DEFAULT '{}'",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
                "promoted_at_utc timestamp with time zone",
                "retired_at_utc timestamp with time zone",
            }, "tenant_id, facility_id, model_code, version");

            CreateTenantFacilityTable(migrationBuilder, "nevaxis_model_training_jobs", "nmtj", new[]
            {
                "model_id uuid NOT NULL",
                "dataset_version_id uuid NOT NULL",
                "status character varying(32) NOT NULL",
                "method character varying(64) NOT NULL",
                "artifact_metadata_json text NOT NULL DEFAULT '{}'",
                "failure_reason character varying(512) NOT NULL DEFAULT ''",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, model_id, dataset_version_id");

            CreateTenantFacilityTable(migrationBuilder, "nevaxis_model_evaluation_runs", "nmer", new[]
            {
                "model_id uuid NOT NULL",
                "evaluation_dataset_version_id uuid",
                "evaluation_mode character varying(64) NOT NULL",
                "outcome character varying(64) NOT NULL",
                "structured_output_validity_rate numeric NOT NULL",
                "safety_pass_rate numeric NOT NULL",
                "hallucinated_entity_rate numeric NOT NULL",
                "unsupported_claim_rate numeric NOT NULL",
                "estimated_cost numeric NOT NULL",
                "latency_ms bigint NOT NULL",
                "metrics_json text NOT NULL DEFAULT '{}'",
                "safety_benchmark_json text NOT NULL DEFAULT '[]'",
                "is_shadow_mode boolean NOT NULL",
                "executed_operationally boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, model_id, created_at_utc");

            CreateTenantFacilityTable(migrationBuilder, "verified_outcome_intelligence_costs", "voic", new[]
            {
                "digital_work_case_id uuid",
                "optimization_run_id uuid",
                "scenario_recovery_run_id uuid",
                "outcome character varying(64) NOT NULL",
                "total_intelligence_cost numeric NOT NULL",
                "model_call_count integer NOT NULL",
                "deterministic_route_count integer NOT NULL",
                "cost_per_verified_outcome numeric",
                "verified_at_utc timestamp with time zone",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, id");

            CreateTenantFacilityTable(migrationBuilder, "intelligence_runtime_budget_policies", "irbp", new[]
            {
                "scope_type character varying(32) NOT NULL",
                "scope_id character varying(128) NOT NULL",
                "max_model_calls integer NOT NULL",
                "max_context_tokens integer NOT NULL",
                "max_output_tokens integer NOT NULL",
                "max_estimated_cost numeric NOT NULL",
                "max_retries integer NOT NULL",
                "max_simulation_count integer NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, scope_type, scope_id");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000fe1', 'intelligence-runtime.read', 'intelligence-runtime.read',
                     'Read intelligence runtime routing, cost, and model lifecycle metrics.',
                     'Zeka çalışma zamanı okuma', 'intelligence-runtime', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 08:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fe2', 'intelligence-runtime.manage', 'intelligence-runtime.manage',
                     'Manage intelligence budgets, routing policies, and dataset operations.',
                     'Zeka çalışma zamanı yönetimi', 'intelligence-runtime', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 08:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fe3', 'intelligence-runtime.evaluate', 'intelligence-runtime.evaluate',
                     'Evaluate, shadow, and promote Nevaxis candidate models.',
                     'Zeka model değerlendirme', 'intelligence-runtime', 'evaluate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 08:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000fe1',
                    '10000000-0000-4000-8000-000000000fe2',
                    '10000000-0000-4000-8000-000000000fe3');
                """);

            migrationBuilder.DropTable(name: "intelligence_runtime_budget_policies");
            migrationBuilder.DropTable(name: "verified_outcome_intelligence_costs");
            migrationBuilder.DropTable(name: "nevaxis_model_evaluation_runs");
            migrationBuilder.DropTable(name: "nevaxis_model_training_jobs");
            migrationBuilder.DropTable(name: "nevaxis_model_registry");
            migrationBuilder.DropTable(name: "nevaxis_verified_manufacturing_dataset_entries");
            migrationBuilder.DropTable(name: "nevaxis_verified_manufacturing_datasets");
            migrationBuilder.DropTable(name: "tenant_intelligence_learning_policies");
            migrationBuilder.DropTable(name: "provider_price_catalog");
            migrationBuilder.DropTable(name: "intelligence_runtime_routes");
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert ON {tableName}
                            FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update ON {tableName}
                            FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
