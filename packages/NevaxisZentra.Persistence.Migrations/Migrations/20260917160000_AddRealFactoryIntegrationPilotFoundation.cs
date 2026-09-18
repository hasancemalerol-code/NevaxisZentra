using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddRealFactoryIntegrationPilotFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "real_factory_pilot_runs", "rfpr", new[]
            {
                "line_id uuid NOT NULL",
                "line_code character varying(64) NOT NULL",
                "current_phase character varying(32) NOT NULL",
                "acceptance_kind character varying(64) NOT NULL",
                "digital_work_case_id uuid",
                "throughput_optimizer_run_id uuid",
                "connector_state_json text NOT NULL",
                "shadow_state_json text NOT NULL",
                "calibration_json text NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, line_id");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_field_mappings", "rffm", new[]
            {
                "connector_id uuid NOT NULL",
                "mapping_version_id uuid",
                "mapping_version_number integer NOT NULL",
                "status character varying(32) NOT NULL",
                "source_field character varying(256) NOT NULL",
                "source_type character varying(64) NOT NULL",
                "target_entity character varying(64) NOT NULL",
                "target_entity_id character varying(128)",
                "canonical_event_type character varying(64) NOT NULL",
                "transformation character varying(128)",
                "unit character varying(32)",
                "scale numeric",
                "quality_rule character varying(128)",
                "timestamp_rule character varying(32) NOT NULL",
                "created_by_subject_id character varying(128) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "activated_at_utc timestamp with time zone",
            }, "tenant_id, connector_id, source_field, mapping_version_number");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_discovery_records", "rfdr", new[]
            {
                "connector_id uuid NOT NULL",
                "discovery_json text NOT NULL",
                "discovered_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, connector_id, discovered_at_utc");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_shadow_sessions", "rfss", new[]
            {
                "line_id uuid NOT NULL",
                "pilot_run_id uuid",
                "mode character varying(32) NOT NULL",
                "is_active boolean NOT NULL",
                "blocks_physical_actions boolean NOT NULL",
                "recommendations_json text NOT NULL",
                "started_at_utc timestamp with time zone NOT NULL",
                "ended_at_utc timestamp with time zone",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, line_id, is_active");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_data_quality_records", "rfdq", new[]
            {
                "connector_id uuid",
                "line_id uuid NOT NULL",
                "overall_state character varying(32) NOT NULL",
                "assessment_json text NOT NULL",
                "blocks_autonomous_action boolean NOT NULL",
                "assessed_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, line_id, assessed_at_utc");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_shadow_accuracy_records", "rfsa", new[]
            {
                "pilot_run_id uuid NOT NULL",
                "predicted_constraint_station character varying(64) NOT NULL",
                "observed_constraint_station character varying(64)",
                "predicted_good_throughput_per_hour numeric",
                "actual_good_throughput_per_hour numeric",
                "predicted_delta_percent numeric",
                "actual_delta_percent numeric",
                "constraint_matched boolean NOT NULL",
                "throughput_within_tolerance boolean NOT NULL",
                "recorded_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, pilot_run_id");

            CreateTenantFacilityTable(migrationBuilder, "mp2_calibration_versions", "mcv", new[]
            {
                "line_id uuid NOT NULL",
                "version_number integer NOT NULL",
                "reason character varying(512) NOT NULL",
                "evidence_json text NOT NULL",
                "parameters_json text NOT NULL",
                "effective_from_utc timestamp with time zone NOT NULL",
                "created_by_subject_id character varying(128) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, line_id, version_number");

            CreateTenantFacilityTable(migrationBuilder, "real_factory_pilot_evidence_packs", "rfpe", new[]
            {
                "pilot_run_id uuid NOT NULL",
                "line_id uuid NOT NULL",
                "facility_code character varying(64) NOT NULL",
                "line_code character varying(64) NOT NULL",
                "pilot_period_start_utc timestamp with time zone NOT NULL",
                "pilot_period_end_utc timestamp with time zone NOT NULL",
                "pack_json text NOT NULL",
                "verification_outcome character varying(64) NOT NULL",
                "knowledge_promoted boolean NOT NULL",
                "generated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, pilot_run_id");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f98', 'real-factory-pilot.read', 'real-factory-pilot.read',
                     'Read real factory pilot workspace, connectors, and evidence packs.',
                     'Gerçek fabrika pilot okuma', 'real-factory-pilot', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f99', 'real-factory-pilot.manage', 'real-factory-pilot.manage',
                     'Manage real factory connector mapping, shadow mode, and pilot configuration.',
                     'Gerçek fabrika pilot yönetimi', 'real-factory-pilot', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fa0', 'real-factory-pilot.run', 'real-factory-pilot.run',
                     'Run real factory pilot observe/shadow/golden acceptance flows.',
                     'Gerçek fabrika pilot çalıştırma', 'real-factory-pilot', 'run',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 16:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "real_factory_pilot_evidence_packs");
            migrationBuilder.DropTable(name: "mp2_calibration_versions");
            migrationBuilder.DropTable(name: "real_factory_shadow_accuracy_records");
            migrationBuilder.DropTable(name: "real_factory_data_quality_records");
            migrationBuilder.DropTable(name: "real_factory_shadow_sessions");
            migrationBuilder.DropTable(name: "real_factory_discovery_records");
            migrationBuilder.DropTable(name: "real_factory_field_mappings");
            migrationBuilder.DropTable(name: "real_factory_pilot_runs");
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
