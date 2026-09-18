using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddThroughputIntelligenceFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "throughput_intelligence_line_targets", "tit", new[]
            {
                "line_id uuid NOT NULL",
                "target_good_units_per_hour numeric NOT NULL",
                "required_good_quantity integer",
                "available_production_hours numeric",
                "target_cycle_seconds numeric",
                "order_reference character varying(64)",
                "is_active boolean NOT NULL",
                "effective_from_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, line_id, is_active, effective_from_utc");

            CreateTenantFacilityTable(migrationBuilder, "throughput_intelligence_operator_skills", "tios", new[]
            {
                "operator_code character varying(64) NOT NULL",
                "display_name character varying(200) NOT NULL",
                "skill_level integer NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, operator_code");

            CreateTenantFacilityTable(migrationBuilder, "throughput_intelligence_operator_qualifications", "tioq", new[]
            {
                "operator_code character varying(64) NOT NULL",
                "station_id uuid NOT NULL",
                "operation_code character varying(64) NOT NULL",
                "qualification_status character varying(32) NOT NULL",
                "expires_at_utc timestamp with time zone",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, operator_code, station_id, operation_code");

            CreateTenantFacilityTable(migrationBuilder, "throughput_intelligence_scenarios", "tis", new[]
            {
                "line_id uuid NOT NULL",
                "scenario_code character varying(64) NOT NULL",
                "scenario_type character varying(64) NOT NULL",
                "title character varying(256) NOT NULL",
                "analysis_window character varying(32) NOT NULL",
                "window_start_utc timestamp with time zone",
                "window_end_utc timestamp with time zone",
                "interventions_json text NOT NULL",
                "assumptions_json text NOT NULL",
                "guardrails_json text NOT NULL",
                "requested_outputs_json text NOT NULL",
                "created_by_subject_id character varying(128) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, line_id, scenario_code");

            CreateTenantFacilityTable(migrationBuilder, "throughput_intelligence_scenario_results", "tisr", new[]
            {
                "scenario_definition_id uuid NOT NULL",
                "eligibility character varying(64) NOT NULL",
                "data_quality character varying(32) NOT NULL",
                "result_json text NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "events_considered integer NOT NULL",
                "stations_evaluated integer NOT NULL",
                "analysis_duration_ms bigint NOT NULL",
                "computed_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, scenario_definition_id, computed_at_utc");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f87', 'throughput-intelligence.read', 'throughput-intelligence.read',
                     'Read throughput measurement, constraints, loss tree, and topology intelligence.',
                     'Verimlilik zekası okuma', 'throughput-intelligence', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 10:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f88', 'throughput-intelligence.manage', 'throughput-intelligence.manage',
                     'Manage throughput targets, operator skills, and qualification configuration.',
                     'Verimlilik zekası yönetimi', 'throughput-intelligence', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 10:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f89', 'throughput-simulation.run', 'throughput-simulation.run',
                     'Run throughput scenario simulations and compare projected outcomes.',
                     'Verimlilik simülasyonu', 'throughput-simulation', 'run',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 10:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "throughput_intelligence_scenario_results");
            migrationBuilder.DropTable(name: "throughput_intelligence_scenarios");
            migrationBuilder.DropTable(name: "throughput_intelligence_operator_qualifications");
            migrationBuilder.DropTable(name: "throughput_intelligence_operator_skills");
            migrationBuilder.DropTable(name: "throughput_intelligence_line_targets");
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
