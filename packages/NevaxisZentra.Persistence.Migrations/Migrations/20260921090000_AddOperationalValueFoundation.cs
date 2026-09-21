using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddOperationalValueFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "verified_operational_value_records", "vovr", new[]
            {
                "digital_work_case_id uuid",
                "optimization_run_id uuid",
                "scenario_recovery_run_id uuid",
                "verified_improvement_id uuid",
                "value_category character varying(64) NOT NULL",
                "status character varying(32) NOT NULL",
                "attribution_class character varying(32) NOT NULL",
                "baseline_value numeric NOT NULL",
                "actual_value numeric NOT NULL",
                "delta_value numeric NOT NULL",
                "unit character varying(32) NOT NULL",
                "baseline_source character varying(128) NOT NULL",
                "baseline_window character varying(128) NOT NULL",
                "baseline_method character varying(64) NOT NULL",
                "observation_window_hours numeric NOT NULL",
                "confidence_score numeric NOT NULL",
                "calculation_method character varying(128) NOT NULL",
                "formula_type character varying(64) NOT NULL",
                "formula_version character varying(16) NOT NULL",
                "input_version character varying(32) NOT NULL",
                "economic_conversion_status character varying(32) NOT NULL",
                "economic_value numeric",
                "currency character varying(8)",
                "intelligence_cost numeric",
                "net_value numeric",
                "roi_percent numeric",
                "payback_days numeric",
                "workflow_type character varying(64) NOT NULL",
                "line_code character varying(64)",
                "scope_entity_type character varying(64)",
                "scope_entity_id character varying(128)",
                "overlap_group_key character varying(64)",
                "human_touches_before integer NOT NULL",
                "human_touches_after integer NOT NULL",
                "evidence_json text NOT NULL DEFAULT '[]'",
                "provenance_json text NOT NULL DEFAULT '{}'",
                "is_invalidated boolean NOT NULL",
                "invalidation_reason character varying(256) NOT NULL DEFAULT ''",
                "invalidated_at_utc timestamp with time zone",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, id");

            CreateTenantFacilityTable(migrationBuilder, "tenant_economic_input_configurations", "teic", new[]
            {
                "version_code character varying(32) NOT NULL",
                "currency character varying(8) NOT NULL",
                "unit_contribution_value numeric",
                "scrap_unit_cost numeric",
                "downtime_cost_per_hour numeric",
                "labor_cost_per_hour numeric",
                "changeover_cost_per_minute numeric",
                "late_delivery_penalty numeric",
                "is_active boolean NOT NULL",
                "effective_from_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, version_code");

            CreateTenantFacilityTable(migrationBuilder, "operational_value_period_summaries", "ovps", new[]
            {
                "period_type character varying(16) NOT NULL",
                "period_start_utc timestamp with time zone NOT NULL",
                "period_end_utc timestamp with time zone NOT NULL",
                "verified_outcome_count integer NOT NULL",
                "good_output_gain_units numeric NOT NULL",
                "downtime_avoided_minutes numeric NOT NULL",
                "late_orders_protected integer NOT NULL",
                "human_touches_reduced integer NOT NULL",
                "intelligence_cost numeric NOT NULL",
                "verified_economic_value numeric",
                "net_value numeric",
                "summary_json text NOT NULL DEFAULT '{}'",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, period_type, period_start_utc");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000ff1', 'operational-value.read', 'operational-value.read',
                     'Read verified operational value, ROI metrics, and evidence.',
                     'Operasyonel değer okuma', 'operational-value', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 09:00:00+00'),
                    ('10000000-0000-4000-8000-000000000ff2', 'operational-value.manage-economic-inputs', 'operational-value.manage-economic-inputs',
                     'Manage tenant economic input configuration for value conversion.',
                     'Ekonomik girdi yönetimi', 'operational-value', 'manage-economic-inputs',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 09:00:00+00'),
                    ('10000000-0000-4000-8000-000000000ff3', 'operational-value.export', 'operational-value.export',
                     'Export customer-safe operational value and ROI reports.',
                     'Operasyonel değer dışa aktarma', 'operational-value', 'export',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-21 09:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000ff1',
                    '10000000-0000-4000-8000-000000000ff2',
                    '10000000-0000-4000-8000-000000000ff3');
                """);

            migrationBuilder.DropTable(name: "operational_value_period_summaries");
            migrationBuilder.DropTable(name: "tenant_economic_input_configurations");
            migrationBuilder.DropTable(name: "verified_operational_value_records");
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
