using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddContinuousOptimizationFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "continuous_optimization_runs", "cor", new[]
            {
                "line_id uuid",
                "optimization_type character varying(32) NOT NULL",
                "trigger_type character varying(64) NOT NULL",
                "entity_type character varying(64) NOT NULL",
                "entity_id character varying(128) NOT NULL",
                "current_phase character varying(32) NOT NULL",
                "outcome character varying(64) NOT NULL DEFAULT ''",
                "failure_state character varying(64) NOT NULL DEFAULT ''",
                "correlation_key character varying(240) NOT NULL",
                "digital_work_case_id uuid",
                "throughput_optimizer_run_id uuid",
                "scenario_recovery_run_id uuid",
                "qualification_json text NOT NULL DEFAULT '{}'",
                "opportunity_json text NOT NULL DEFAULT '{}'",
                "explainability_json text NOT NULL DEFAULT '{}'",
                "metrics_json text NOT NULL DEFAULT '{}'",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, correlation_key");

            CreateTenantFacilityTable(migrationBuilder, "continuous_optimization_opportunities", "coo", new[]
            {
                "run_id uuid NOT NULL",
                "opportunity_type character varying(32) NOT NULL",
                "scope_entity_type character varying(64) NOT NULL",
                "scope_entity_id character varying(128) NOT NULL",
                "title character varying(240) NOT NULL",
                "summary character varying(1024) NOT NULL",
                "estimated_benefit numeric NOT NULL",
                "confidence numeric NOT NULL",
                "risk_class character varying(32) NOT NULL",
                "evidence_json text NOT NULL DEFAULT '[]'",
                "expires_at_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, run_id");

            CreateTenantFacilityTable(migrationBuilder, "continuous_optimization_policies", "cop", new[]
            {
                "optimization_type character varying(32) NOT NULL",
                "scope_type character varying(32) NOT NULL",
                "scope_id character varying(128) NOT NULL",
                "is_enabled boolean NOT NULL",
                "is_paused boolean NOT NULL",
                "thresholds_json text NOT NULL DEFAULT '{}'",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, optimization_type, scope_type, scope_id");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000fc1', 'continuous-optimization.read', 'continuous-optimization.read',
                     'Read continuous optimization runs, opportunities, and verified gains.',
                     'Sürekli optimizasyon okuma', 'continuous-optimization', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-20 20:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fc2', 'continuous-optimization.manage', 'continuous-optimization.manage',
                     'Manage continuous optimization policies and user controls.',
                     'Sürekli optimizasyon yönetimi', 'continuous-optimization', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-20 20:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fc3', 'continuous-optimization.run', 'continuous-optimization.run',
                     'Evaluate continuous optimization triggers and bounded runs.',
                     'Sürekli optimizasyon çalıştırma', 'continuous-optimization', 'run',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-20 20:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000fc1',
                    '10000000-0000-4000-8000-000000000fc2',
                    '10000000-0000-4000-8000-000000000fc3');
                """);

            migrationBuilder.DropTable(name: "continuous_optimization_policies");
            migrationBuilder.DropTable(name: "continuous_optimization_opportunities");
            migrationBuilder.DropTable(name: "continuous_optimization_runs");
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
